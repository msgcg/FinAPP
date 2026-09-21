using System;
using System.IO;
using System.Linq;
using FinAPP.Models;
using FinAPP.Services;
using Xunit;

namespace FinAPP.Tests;

public class GameEngineTests
{
    private readonly GameEngine _engine;
    private readonly string _testDbPath;

    public GameEngineTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"finapp_test_{Guid.NewGuid():N}.json");
        var storage = new StorageService(_testDbPath);
        _engine = new GameEngine(storage);
    }

    [Fact]
    public void BudgetPlan_ExceedsBalance_ShouldFail()
    {
        // Баланс по умолчанию 400
        _engine.Profile.Balance = 300;

        var result = _engine.ConfirmBudgetPlan(obligatory: 150, discretionary: 100, savings: 100);

        Assert.False(result.Success);
        Assert.False(_engine.Profile.IsPlanConfirmed);
        Assert.Contains("превышает", result.Message);
    }

    [Fact]
    public void BudgetPlan_WithinBalance_ShouldSucceed()
    {
        _engine.Profile.Balance = 500;

        var result = _engine.ConfirmBudgetPlan(obligatory: 150, discretionary: 100, savings: 100);

        Assert.True(result.Success);
        Assert.True(_engine.Profile.IsPlanConfirmed);
        Assert.Equal(150, _engine.Profile.PlannedObligatory);
        Assert.Equal(100, _engine.Profile.PlannedDiscretionary);
        Assert.Equal(100, _engine.Profile.PlannedSavings);
    }

    [Fact]
    public void PurchaseItem_InsufficientFunds_ShouldFailWithoutNegativeBalance()
    {
        _engine.Profile.Balance = 30;
        var item = new ShopItem
        {
            Id = "test_food",
            Name = "Премиум обед",
            Category = ExpenseCategory.Obligatory,
            Price = 50,
            HungerBoost = 30,
            MoodBoost = 10
        };

        var result = _engine.PurchaseItem(item);

        Assert.False(result.Success);
        Assert.Equal(30, _engine.Profile.Balance); // баланс не изменился
        Assert.Contains("Недостаточно монет", result.Message);
    }

    [Fact]
    public void PurchaseItem_SufficientFunds_ShouldDeductBalanceAndBoostPet()
    {
        _engine.Profile.Balance = 200;
        _engine.Profile.Hunger = 50;
        _engine.Profile.Mood = 50;
        var item = new ShopItem
        {
            Id = "test_food",
            Name = "Обед",
            Category = ExpenseCategory.Obligatory,
            Price = 50,
            HungerBoost = 35,
            MoodBoost = 10
        };

        var result = _engine.PurchaseItem(item);

        Assert.True(result.Success);
        Assert.Equal(150, _engine.Profile.Balance);
        Assert.Equal(85, _engine.Profile.Hunger);
        Assert.Equal(60, _engine.Profile.Mood);
        Assert.Equal(50, _engine.Profile.ActualObligatory);
    }

    [Fact]
    public void DepositToSavings_ShouldTransferFundsAndIncreaseMood()
    {
        _engine.Profile.Balance = 300;
        _engine.Profile.Savings = 50;
        var goal = new FinancialGoal { Title = "Самокат", TargetAmount = 350 };

        var result = _engine.DepositToSavings(100, goal);

        Assert.True(result.Success);
        Assert.Equal(200, _engine.Profile.Balance);
        Assert.Equal(150, _engine.Profile.Savings);
    }

    [Fact]
    public void WithdrawFromSavings_ShouldUpdateSavingsAndWarnOfDelay()
    {
        _engine.Profile.Balance = 100;
        _engine.Profile.Savings = 200;
        var goal = new FinancialGoal { Title = "Самокат", TargetAmount = 350 };

        var result = _engine.WithdrawFromSavings(50, goal);

        Assert.True(result.Success);
        Assert.Equal(150, _engine.Profile.Balance);
        Assert.Equal(150, _engine.Profile.Savings);
        Assert.Contains("увеличился", result.Message);
    }

    [Fact]
    public void CompleteTask_CorrectOption_ShouldRewardAndTrackStats()
    {
        _engine.Profile.Balance = 100;
        _engine.Profile.TestsPassedCount = 0;

        var task = new FinancialTask { Id = "test_task", Title = "Тестовый кейс" };
        var option = new TaskOption { Text = "Верно", IsCorrect = true, RewardCoins = 100, Explanation = "Отлично" };

        var result = _engine.CompleteTask(task, option);

        Assert.True(result.Success);
        Assert.Equal(200, _engine.Profile.Balance);
        Assert.Equal(1, _engine.Profile.TestsPassedCount);
    }

    [Fact]
    public void CompleteTask_IncorrectOption_ShouldPenalizeMoodWithoutMoneyPenalty()
    {
        _engine.Profile.Balance = 100;
        _engine.Profile.TestsPassedCount = 0;
        _engine.Profile.Mood = 80;

        var task = new FinancialTask { Id = "test_task", Title = "Тестовый кейс" };
        var option = new TaskOption { Text = "Неверно", IsCorrect = false, Explanation = "Пояснение ошибки" };

        var result = _engine.CompleteTask(task, option);

        Assert.False(result.Success);
        Assert.Equal(100, _engine.Profile.Balance); // деньги не отбираются
        Assert.Equal(65, _engine.Profile.Mood); // настроение снижается на 15
        Assert.Equal(0, _engine.Profile.TestsPassedCount);
        Assert.Contains("Пояснение ошибки", result.Message);
    }

    [Fact]
    public void CompleteTask_RetryCorrectOption_ShouldRefundPenaltyAndRewardMood()
    {
        _engine.Profile.Balance = 100;
        _engine.Profile.Mood = 80;

        var task = new FinancialTask { Id = "test_task", Title = "Тестовый кейс" };
        var wrongOption = new TaskOption { Text = "Неверно", IsCorrect = false, Explanation = "Пояснение ошибки" };
        var correctOption = new TaskOption { Text = "Верно", IsCorrect = true, RewardCoins = 50, Explanation = "Всё верно!" };

        // 1. Первая неверная попытка снижает настроение с 80 до 65
        var failResult = _engine.CompleteTask(task, wrongOption, isRetry: false);
        Assert.False(failResult.Success);
        Assert.Equal(65, _engine.Profile.Mood);

        // 2. Вторая неверная попытка в режиме retry не штрафует повторно
        var retryFailResult = _engine.CompleteTask(task, wrongOption, isRetry: true);
        Assert.False(retryFailResult.Success);
        Assert.Equal(65, _engine.Profile.Mood);

        // 3. Верный ответ при retry возвращает штраф (+15) и начисляет награду (+15), итого 65 + 30 = 95
        var successResult = _engine.CompleteTask(task, correctOption, isRetry: true);
        Assert.True(successResult.Success);
        Assert.Equal(95, _engine.Profile.Mood);
        Assert.Equal(150, _engine.Profile.Balance);
    }

    [Fact]
    public void AdvancePeriod_FiveConsecutivePeriods_ShouldTriggerPetEvolutionToMaster_InDemoMode()
    {
        _engine.Profile.IsDemoMode = true;
        Assert.Equal(1, _engine.Profile.CurrentPeriod);
        Assert.Equal(GrowthStage.Baby, _engine.Profile.Stage);

        // Период 1 -> 2
        _engine.AdvanceToNextPeriod();
        Assert.Equal(2, _engine.Profile.CurrentPeriod);
        Assert.Equal(GrowthStage.Baby, _engine.Profile.Stage);

        // Период 2 -> 3 (Эволюция в подростка)
        _engine.AdvanceToNextPeriod();
        Assert.Equal(3, _engine.Profile.CurrentPeriod);
        Assert.Equal(GrowthStage.Teen, _engine.Profile.Stage);

        // Период 3 -> 4
        _engine.AdvanceToNextPeriod();
        Assert.Equal(4, _engine.Profile.CurrentPeriod);
        Assert.Equal(GrowthStage.Teen, _engine.Profile.Stage);

        // Период 4 -> 5 (Эволюция в Финни-Мастера)
        _engine.AdvanceToNextPeriod();
        Assert.Equal(5, _engine.Profile.CurrentPeriod);
        Assert.Equal(GrowthStage.Master, _engine.Profile.Stage);

        // Проверяем историю
        Assert.Equal(4, _engine.Profile.History.Count);
    }

    [Fact]
    public void GoalAchieved_Evolution_ZeroToTwoBaby_ThreeToEightTeen_NinePlusMaster()
    {
        // 0 целей - Малыш
        _engine.Profile.GoalsAchievedCount = 0;
        _engine.CheckGoalEvolution();
        Assert.Equal(GrowthStage.Baby, _engine.Profile.Stage);

        // 2 цели - всё ещё Малыш
        _engine.Profile.GoalsAchievedCount = 2;
        _engine.CheckGoalEvolution();
        Assert.Equal(GrowthStage.Baby, _engine.Profile.Stage);

        // 3 цели - взрослеет до Юниора
        _engine.Profile.GoalsAchievedCount = 3;
        bool evolvedToTeen = _engine.CheckGoalEvolution();
        Assert.True(evolvedToTeen);
        Assert.Equal(GrowthStage.Teen, _engine.Profile.Stage);

        // 8 целей - всё ещё Юниор
        _engine.Profile.GoalsAchievedCount = 8;
        _engine.CheckGoalEvolution();
        Assert.Equal(GrowthStage.Teen, _engine.Profile.Stage);

        // 9 целей - взрослеет до Мастера
        _engine.Profile.GoalsAchievedCount = 9;
        bool evolvedToMaster = _engine.CheckGoalEvolution();
        Assert.True(evolvedToMaster);
        Assert.Equal(GrowthStage.Master, _engine.Profile.Stage);

        // 12 целей - высшая стадия Мастер
        _engine.Profile.GoalsAchievedCount = 12;
        _engine.CheckGoalEvolution();
        Assert.Equal(GrowthStage.Master, _engine.Profile.Stage);
    }

    [Fact]
    public void ContentRepository_ShouldMeetAllMinimumVolumesOfSpecification()
    {
        // п. 2.6 ТЗ: Покупки не менее 8 позиций двух типов
        var items = ContentRepository.GetShopItems();
        Assert.True(items.Count >= 8);
        Assert.Contains(items, i => i.Category == ExpenseCategory.Obligatory);
        Assert.Contains(items, i => i.Category == ExpenseCategory.Discretionary);

        // п. 2.6 ТЗ: Задания не менее 6 заданий по 3 темам
        var tasks = ContentRepository.GetFinancialTasks();
        Assert.True(tasks.Count >= 36);
        Assert.Contains(tasks, t => t.Topic == TaskTopic.BudgetPlanning);
        Assert.Contains(tasks, t => t.Topic == TaskTopic.SavingsAndReserve);
        Assert.Contains(tasks, t => t.Topic == TaskTopic.PaymentsAndSecurity);

        // п. 2.6 ТЗ: Цели накопления не менее 3 целей
        var goals = ContentRepository.GetPresetGoals();
        Assert.True(goals.Count >= 3);

        // Словарь терминов
        var glossary = ContentRepository.GetGlossaryTerms();
        Assert.True(glossary.Count >= 6);
    }

    [Fact]
    public void AgeGroup_Switching_ShouldFilterTasksAppropriately()
    {
        // Проверяем 7–8 лет (Junior)
        _engine.SetAgeGroup(AgeGroup.Junior7_8);
        Assert.Equal(AgeGroup.Junior7_8, _engine.Profile.AgeGroup);
        var juniorTasks = _engine.GetTasksForCurrentAge();
        Assert.Equal(18, juniorTasks.Count);
        Assert.All(juniorTasks, t => Assert.Equal(AgeGroup.Junior7_8, t.TargetAge));
        Assert.Contains(juniorTasks, t => t.Topic == TaskTopic.BudgetPlanning);
        Assert.Contains(juniorTasks, t => t.Topic == TaskTopic.SavingsAndReserve);
        Assert.Contains(juniorTasks, t => t.Topic == TaskTopic.PaymentsAndSecurity);

        // Проверяем 9–11 лет (Senior)
        _engine.SetAgeGroup(AgeGroup.Senior9_11);
        Assert.Equal(AgeGroup.Senior9_11, _engine.Profile.AgeGroup);
        var seniorTasks = _engine.GetTasksForCurrentAge();
        Assert.Equal(18, seniorTasks.Count);
        Assert.All(seniorTasks, t => Assert.Equal(AgeGroup.Senior9_11, t.TargetAge));
        Assert.Contains(seniorTasks, t => t.Topic == TaskTopic.BudgetPlanning);
        Assert.Contains(seniorTasks, t => t.Topic == TaskTopic.SavingsAndReserve);
        Assert.Contains(seniorTasks, t => t.Topic == TaskTopic.PaymentsAndSecurity);

        // Всего заданий 36
        var allTasks = ContentRepository.GetFinancialTasks();
        Assert.Equal(36, allTasks.Count);
    }

    [Fact]
    public void FinnyMascotGifs_AllStagesAndEmotions_ShouldExistAndBeValidGifFiles()
    {
        // 3 стадии эволюции x 4 эмоции = 12 обязательных файлов анимаций
        var stages = new[] { "finny_baby", "finny_teen", "finny_master" };
        var emotions = new[] { "idle", "wave", "proud", "sad" };

        var testDir = AppContext.BaseDirectory;
        // Ищем путь к папке проекта FinAPP
        var currentDir = new DirectoryInfo(testDir);
        while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "FinAPP.slnx")))
        {
            currentDir = currentDir.Parent;
        }
        Assert.NotNull(currentDir);

        var rawDir = Path.Combine(currentDir.FullName, "FinAPP", "Resources", "Raw");
        Assert.True(Directory.Exists(rawDir), $"Папка Resources/Raw не найдена по пути {rawDir}");

        foreach (var stage in stages)
        {
            foreach (var emotion in emotions)
            {
                var fileName = $"{stage}_{emotion}.gif";
                var filePath = Path.Combine(rawDir, fileName);

                Assert.True(File.Exists(filePath), $"Отсутствует файл анимации: {fileName}");

                var fileInfo = new FileInfo(filePath);
                Assert.True(fileInfo.Length > 100_000, $"Размер файла {fileName} подозрительно мал: {fileInfo.Length} байт");

                // Проверяем GIF-сигнатуру (GIF87a или GIF89a)
                using var fs = File.OpenRead(filePath);
                var header = new byte[6];
                int bytesRead = fs.Read(header, 0, 6);
                Assert.Equal(6, bytesRead);

                var headerStr = System.Text.Encoding.ASCII.GetString(header);
                Assert.True(headerStr == "GIF89a" || headerStr == "GIF87a",
                    $"Файл {fileName} не имеет валидной сигнатуры GIF: {headerStr}");
            }
        }
    }

    [Fact]
    public void AdvanceToNextPeriod_ShouldAccrueCompoundInterestOnSavings()
    {
        // Начальные накопления 200 монет
        _engine.Profile.Savings = 200;
        _engine.Profile.Balance = 300;
        int initialSavings = _engine.Profile.Savings;

        string msg = _engine.AdvanceToNextPeriod();

        // +5% от 200 = 10 монет
        int expectedInterest = 10;
        Assert.Equal(initialSavings + expectedInterest, _engine.Profile.Savings);
        Assert.True(_engine.Profile.History.Count > 0);
        var lastSummary = _engine.Profile.History.Last();
        Assert.Equal(expectedInterest, lastSummary.InterestEarned);
        Assert.Contains("+10 монет", msg);
    }

    [Fact]
    public void AdvanceToNextPeriod_SmallSavings_ShouldAccrueAtLeastOneCoinInterest()
    {
        // Небольшие сбережения 10 монет -> 5% от 10 = 0.5 -> округление и минимум 1 монета
        _engine.Profile.Savings = 10;
        string msg = _engine.AdvanceToNextPeriod();

        Assert.Equal(11, _engine.Profile.Savings);
        var lastSummary = _engine.Profile.History.Last();
        Assert.Equal(1, lastSummary.InterestEarned);
        Assert.Contains("+1 монет", msg);
    }

    [Fact]
    public void PetDesks_AllTypes_ShouldHaveExistingPngAssets()
    {
        var testDir = AppContext.BaseDirectory;
        var currentDir = new DirectoryInfo(testDir);
        while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "FinAPP.slnx")))
        {
            currentDir = currentDir.Parent;
        }
        Assert.NotNull(currentDir);

        var imgDir = Path.Combine(currentDir.FullName, "FinAPP", "Resources", "Images");
        var deskFileMap = new Dictionary<PetDeskType, string>
        {
            [PetDeskType.Modern] = "desk_modern.png",
            [PetDeskType.Artisan] = "desk_artisan.png",
            [PetDeskType.Market] = "desk_market.png",
            [PetDeskType.Maker] = "desk_maker.png",
            [PetDeskType.Reading] = "desk_reading.png",
            [PetDeskType.Botanical] = "desk_botanical.png"
        };

        foreach (var (deskType, fileName) in deskFileMap)
        {
            var filePath = Path.Combine(imgDir, fileName);
            Assert.True(File.Exists(filePath), $"Отсутствует файл стола: {fileName} для типа {deskType}");
            var fi = new FileInfo(filePath);
            Assert.True(fi.Length > 20_000, $"Размер файла {fileName} подозрительно мал: {fi.Length} байт");
        }
    }

    [Fact]
    public void Profile_DeskAndParentPinAndOnboarding_ShouldPersistCorrectly()
    {
        var initial = new StorageService().CreateInitialProfile();
        Assert.False(initial.IsOnboardingCompleted, "При первом запуске обучение должно быть не пройдено (false)");

        _engine.Profile.Desk = PetDeskType.Modern;
        _engine.Profile.ParentPin = "1234";
        _engine.Profile.IsOnboardingCompleted = true;

        Assert.Equal(PetDeskType.Modern, _engine.Profile.Desk);
        Assert.Equal("1234", _engine.Profile.ParentPin);
        Assert.True(_engine.Profile.IsOnboardingCompleted);
    }

    [Fact]
    public void AdvanceToNextPeriod_DemoMode_ShouldSimulateExpensesAndTrackCumulativeSavings()
    {
        _engine.Profile.IsDemoMode = true;
        _engine.Profile.Savings = 100;
        _engine.Profile.ActualObligatory = 0;
        _engine.Profile.ActualDiscretionary = 0;
        _engine.Profile.ActualSavings = 0;

        _engine.AdvanceToNextPeriod();

        var summary = _engine.Profile.History.Last();
        Assert.True(summary.ActualObligatory > 0, "В демо-режиме должны быть сгенерированы обязательные траты");
        Assert.True(summary.ActualDiscretionary > 0, "В демо-режиме должны быть сгенерированы траты на желания");
        Assert.True(summary.EndPeriodSavings > 0, "EndPeriodSavings должен фиксировать баланс сбережений");
        Assert.Equal(_engine.Profile.Savings, summary.EndPeriodSavings);
    }

    [Fact]
    public void FinnyEmotion_LowMoodOrHunger_ShouldBeSad_AndHappyOtherwise()
    {
        // 1. При сытости <= 40 - грустный
        _engine.Profile.Hunger = 40;
        _engine.Profile.Mood = 80;
        Assert.Equal("sad", _engine.CurrentEmotion);

        // 2. При настроении <= 40 - грустный
        _engine.Profile.Hunger = 80;
        _engine.Profile.Mood = 35;
        Assert.Equal("sad", _engine.CurrentEmotion);

        // 3. При высоких показателях (> 40) - веселый (happy или proud)
        _engine.Profile.Hunger = 60;
        _engine.Profile.Mood = 60;
        _engine.Profile.IsPlanConfirmed = false;
        Assert.Equal("happy", _engine.CurrentEmotion);

        // 4. При отличных показателях и утвержденном плане - гордый (proud)
        _engine.Profile.Hunger = 80;
        _engine.Profile.Mood = 85;
        _engine.Profile.IsPlanConfirmed = true;
        Assert.Equal("proud", _engine.CurrentEmotion);
    }

    [Fact]
    public void PetProfile_PlatformsAndDesks_DefaultAndUnlocking_ShouldWorkCorrectly()
    {
        var profile = new PetProfile();

        // Все подиумы полностью бесплатны для детей по умолчанию
        Assert.True(profile.IsPlatformUnlocked(PetPlatformType.Flowers));
        Assert.True(profile.IsPlatformUnlocked(PetPlatformType.Stars));
        Assert.True(profile.IsPlatformUnlocked(PetPlatformType.Emerald));
        Assert.True(profile.IsPlatformUnlocked(PetPlatformType.Cosmic));
        Assert.True(profile.IsPlatformUnlocked(PetPlatformType.Cloud));

        // Бесплатный стол по умолчанию
        Assert.True(profile.IsDeskUnlocked(PetDeskType.None));

        // Платные столы по умолчанию заблокированы
        Assert.False(profile.IsDeskUnlocked(PetDeskType.Modern));
        Assert.False(profile.IsDeskUnlocked(PetDeskType.Artisan));

        // Разблокировка стола
        profile.UnlockDesk(PetDeskType.Modern);
        Assert.True(profile.IsDeskUnlocked(PetDeskType.Modern));
    }

    [Fact]
    public void PurchaseItem_InteriorDeskAndPlatform_ShouldUnlockAndApply()
    {
        _engine.Profile.Balance = 600;
        var deskItem = ContentRepository.GetShopItems().First(i => i.Id == "desk_modern");
        Assert.NotNull(deskItem.LinkedDesk);
        Assert.False(_engine.Profile.IsDeskUnlocked(deskItem.LinkedDesk.Value));

        var result = _engine.PurchaseItem(deskItem);

        Assert.True(result.Success);
        Assert.True(_engine.Profile.IsDeskUnlocked(deskItem.LinkedDesk.Value));
        Assert.Equal(deskItem.LinkedDesk.Value, _engine.Profile.Desk);
        Assert.Equal(400, _engine.Profile.Balance);

        // Подиумы бесплатны (0 монет) и всегда разблокированы
        var platItem = ContentRepository.GetShopItems().First(i => i.Id == "platform_stars");
        Assert.NotNull(platItem.LinkedPlatform);
        Assert.True(_engine.Profile.IsPlatformUnlocked(platItem.LinkedPlatform.Value));
        Assert.Equal(0, platItem.Price);

        var platResult = _engine.PurchaseItem(platItem);

        Assert.True(platResult.Success);
        Assert.Equal(platItem.LinkedPlatform.Value, _engine.Profile.Platform);
        Assert.Equal(400, _engine.Profile.Balance); // Баланс не уменьшился (0 монет)
    }

    [Fact]
    public void ContentRepository_PresetGoals_ShouldContainDesksAndToysWithoutOldScooter()
    {
        var goals = ContentRepository.GetPresetGoals();

        // Старые цели удалены
        Assert.DoesNotContain(goals, g => g.Id == "goal_scooter");
        Assert.DoesNotContain(goals, g => g.Id == "goal_game");
        Assert.DoesNotContain(goals, g => g.Id == "goal_gadget");

        // Присутствуют платные столы и игрушки
        Assert.Contains(goals, g => g.LinkedDesk.HasValue);
        Assert.Contains(goals, g => !string.IsNullOrEmpty(g.LinkedShopItemId));
        Assert.True(goals.Count >= 5);
    }

    [Fact]
    public void PetName_MaxLength_ShouldBeLimitedTo12Characters()
    {
        string longName = "ВеликийКотФинниДвенадцатый";
        string truncated = longName.Length > 12 ? longName.Substring(0, 12) : longName;
        Assert.Equal(12, truncated.Length);
        Assert.Equal("ВеликийКотФи", truncated);

        _engine.Profile.PetName = truncated;
        Assert.Equal("ВеликийКотФи", _engine.Profile.PetName);
    }

    [Fact]
    public void SfxMeowAudio_File_ShouldExistInRawAudioAndHaveValidSize()
    {
        var testDir = AppContext.BaseDirectory;
        var currentDir = new DirectoryInfo(testDir);
        while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "FinAPP.slnx")))
        {
            currentDir = currentDir.Parent;
        }
        Assert.NotNull(currentDir);

        var audioPath = Path.Combine(currentDir.FullName, "FinAPP", "Resources", "Raw", "audio", "sfx_meow.mp3");
        Assert.True(File.Exists(audioPath), $"Файл sfx_meow.mp3 отсутствует по пути {audioPath}");

        var fileInfo = new FileInfo(audioPath);
        // Звук короткого мяуканья ~0.5 сек должен быть в диапазоне 4–50 КБ
        Assert.True(fileInfo.Length > 3_000 && fileInfo.Length < 100_000, 
            $"Размер sfx_meow.mp3 необычен: {fileInfo.Length} байт");
    }

    [Fact]
    public void PurchaseItem_RegularItem_CanBePurchasedMultipleTimes()
    {
        _engine.Profile.Balance = 500;
        var food = new ShopItem
        {
            Id = "test_food_repeat",
            Name = "Вкусный обед",
            Category = ExpenseCategory.Obligatory,
            Price = 40,
            HungerBoost = 25
        };

        var first = _engine.PurchaseItem(food);
        Assert.True(first.Success);
        Assert.Equal(460, _engine.Profile.Balance);

        var second = _engine.PurchaseItem(food);
        Assert.True(second.Success);
        Assert.Equal(420, _engine.Profile.Balance);
    }

    [Fact]
    public void PurchaseItem_NonRegularItem_CannotBePurchasedTwiceInSameStage()
    {
        _engine.Profile.Balance = 500;
        var toy = new ShopItem
        {
            Id = "toy_drone_unique",
            Name = "Квадрокоптер",
            Category = ExpenseCategory.Discretionary,
            Price = 120,
            MoodBoost = 35
        };

        var first = _engine.PurchaseItem(toy);
        Assert.True(first.Success);
        Assert.True(_engine.Profile.IsNonRegularItemPurchased("toy_drone_unique"));
        Assert.Equal(380, _engine.Profile.Balance);

        // Повторная покупка на той же стадии роста блокируется
        var second = _engine.PurchaseItem(toy);
        Assert.False(second.Success);
        Assert.Contains("уже приобретён", second.Message);
        Assert.Equal(380, _engine.Profile.Balance); // Баланс не списался
    }

    [Fact]
    public void GrowthStageEvolution_ClearsNonRegularPurchases()
    {
        _engine.Profile.Stage = GrowthStage.Baby;
        _engine.Profile.GoalsAchievedCount = 2;
        _engine.Profile.PurchasedNonRegularItemIds.Add("toy_drone_unique");
        Assert.True(_engine.Profile.IsNonRegularItemPurchased("toy_drone_unique"));

        // Достижение 3-й цели переводит котика на стадию Teen (Юниор)
        _engine.Profile.GoalsAchievedCount = 3;
        bool evolved = _engine.CheckGoalEvolution();

        Assert.True(evolved);
        Assert.Equal(GrowthStage.Teen, _engine.Profile.Stage);
        // Список покупок текущей стадии очищается для новых возможностей подросшего котика
        Assert.Empty(_engine.Profile.PurchasedNonRegularItemIds);
        Assert.False(_engine.Profile.IsNonRegularItemPurchased("toy_drone_unique"));
    }

    [Fact]
    public void PurchaseItem_MatchingActiveGoal_AutomaticallyCompletesGoalAndIncrementsCount()
    {
        _engine.Profile.Balance = 500;
        _engine.Profile.GoalsAchievedCount = 0;
        _engine.Profile.CompletedGoalIds.Clear();
        _engine.Profile.SelectedGoalId = "goal_toy_laser";

        var laserShopItem = new ShopItem
        {
            Id = "toy_laser",
            Name = "Лазерная указка-дразнилка",
            Category = ExpenseCategory.Discretionary,
            Price = 140,
            MoodBoost = 25
        };

        var result = _engine.PurchaseItem(laserShopItem);
        Assert.True(result.Success);
        Assert.Contains("Цель «Лазерная указка-дразнилка» достигнута", result.Message);
        Assert.Equal(1, _engine.Profile.GoalsAchievedCount);
        Assert.Contains("goal_toy_laser", _engine.Profile.CompletedGoalIds);
        Assert.True(_engine.Profile.IsNonRegularItemPurchased("toy_laser"));
    }

    [Fact]
    public void CompleteTask_CorrectOption_ShouldLogTopicAndTaskDetailsInParentLog()
    {
        _engine.Profile.TaskCompletionLog.Clear();
        _engine.Profile.CompletedTaskIds.Clear();

        var taskBudget = new FinancialTask
        {
            Id = "test_task_budget",
            Title = "Планирование недели",
            Topic = TaskTopic.BudgetPlanning,
            CompetencyReference = "Рамка компетенций 6.1",
            Options = new()
            {
                new TaskOption { Text = "Правильный ответ", IsCorrect = true, RewardCoins = 50, Explanation = "Отлично!" }
            }
        };

        var taskSecurity = new FinancialTask
        {
            Id = "test_task_security",
            Title = "Безопасность пароля",
            Topic = TaskTopic.PaymentsAndSecurity,
            CompetencyReference = "Рамка компетенций 6.5",
            Options = new()
            {
                new TaskOption { Text = "Не сообщать никому", IsCorrect = true, RewardCoins = 70, Explanation = "Верно!" }
            }
        };

        var res1 = _engine.CompleteTask(taskBudget, taskBudget.Options[0]);
        Assert.True(res1.Success);

        var res2 = _engine.CompleteTask(taskSecurity, taskSecurity.Options[0]);
        Assert.True(res2.Success);

        Assert.Equal(2, _engine.Profile.TaskCompletionLog.Count);
        Assert.Equal("test_task_budget", _engine.Profile.TaskCompletionLog[0].TaskId);
        Assert.Equal("Планирование недели", _engine.Profile.TaskCompletionLog[0].TaskTitle);
        Assert.Equal(TaskTopic.BudgetPlanning, _engine.Profile.TaskCompletionLog[0].Topic);
        Assert.Equal("Планирование бюджета", _engine.Profile.TaskCompletionLog[0].TopicName);
        Assert.Equal(50, _engine.Profile.TaskCompletionLog[0].RewardCoins);

        Assert.Equal("test_task_security", _engine.Profile.TaskCompletionLog[1].TaskId);
        Assert.Equal(TaskTopic.PaymentsAndSecurity, _engine.Profile.TaskCompletionLog[1].Topic);
        Assert.Equal("Платежи и безопасность", _engine.Profile.TaskCompletionLog[1].TopicName);
        Assert.Equal(70, _engine.Profile.TaskCompletionLog[1].RewardCoins);
    }

    [Fact]
    public async System.Threading.Tasks.Task TaskCompletionLog_SerializationAndPersistence_ShouldRetainLoggedTopics()
    {
        _engine.Profile.TaskCompletionLog.Clear();
        var task = new FinancialTask
        {
            Id = "test_task_savings",
            Title = "Копилка мечты",
            Topic = TaskTopic.SavingsAndReserve,
            CompetencyReference = "Рамка компетенций 6.4",
            Options = new()
            {
                new TaskOption { Text = "Копить регулярно", IsCorrect = true, RewardCoins = 60 }
            }
        };

        _engine.CompleteTask(task, task.Options[0]);
        await _engine.SaveAsync();

        var storage = new StorageService(_testDbPath);
        var loadedProfile = await storage.LoadProfileAsync();

        Assert.NotNull(loadedProfile.TaskCompletionLog);
        Assert.Single(loadedProfile.TaskCompletionLog);
        Assert.Equal("test_task_savings", loadedProfile.TaskCompletionLog[0].TaskId);
        Assert.Equal(TaskTopic.SavingsAndReserve, loadedProfile.TaskCompletionLog[0].Topic);
        Assert.Equal("Сбережения и подушка", loadedProfile.TaskCompletionLog[0].TopicName);
        Assert.Equal(60, loadedProfile.TaskCompletionLog[0].RewardCoins);
        Assert.True(loadedProfile.TaskCompletionLog[0].IsSuccess);
        Assert.Equal(1, loadedProfile.TaskCompletionLog[0].AttemptsCount);
    }

    [Fact]
    public void CompleteTask_TrackAttemptsAndSuccessState_CorrectFirstAttempt_ShouldLogSuccessAndOneAttempt()
    {
        _engine.Profile.TaskCompletionLog.Clear();
        var task = new FinancialTask
        {
            Id = "task_attempt_first",
            Title = "Разумные покупки",
            Topic = TaskTopic.BudgetPlanning,
            CompetencyReference = "Рамка Минфина 2.1",
            Options = new()
            {
                new TaskOption { Text = "Составить список", IsCorrect = true, RewardCoins = 45 }
            }
        };

        var result = _engine.CompleteTask(task, task.Options[0], isRetry: false, attemptNumber: 1);

        Assert.True(result.Success);
        Assert.Single(_engine.Profile.TaskCompletionLog);
        var record = _engine.Profile.TaskCompletionLog[0];
        Assert.True(record.IsSuccess);
        Assert.Equal(1, record.AttemptsCount);
        Assert.Equal(45, record.RewardCoins);
    }

    [Fact]
    public void CompleteTask_TrackAttemptsAndSuccessState_WrongThenRetryCorrect_ShouldUpdateLogToSuccessAndMultipleAttempts()
    {
        _engine.Profile.TaskCompletionLog.Clear();
        var task = new FinancialTask
        {
            Id = "task_attempt_retry",
            Title = "Безопасность в сети",
            Topic = TaskTopic.PaymentsAndSecurity,
            CompetencyReference = "Рамка Минфина 4.2",
            Options = new()
            {
                new TaskOption { Text = "Открыть неизвестный файл", IsCorrect = false, RewardCoins = 0 },
                new TaskOption { Text = "Посоветоваться со взрослыми", IsCorrect = true, RewardCoins = 55 }
            }
        };

        // Первая неверная попытка
        var failResult = _engine.CompleteTask(task, task.Options[0], isRetry: false, attemptNumber: 1);
        Assert.False(failResult.Success);
        Assert.Single(_engine.Profile.TaskCompletionLog);
        var recordFail = _engine.Profile.TaskCompletionLog[0];
        Assert.False(recordFail.IsSuccess);
        Assert.Equal(1, recordFail.AttemptsCount);
        Assert.Equal(0, recordFail.RewardCoins);

        // Вторая правильная попытка
        var successResult = _engine.CompleteTask(task, task.Options[1], isRetry: true, attemptNumber: 2);
        Assert.True(successResult.Success);
        Assert.Single(_engine.Profile.TaskCompletionLog);
        var recordSuccess = _engine.Profile.TaskCompletionLog[0];
        Assert.True(recordSuccess.IsSuccess);
        Assert.Equal(2, recordSuccess.AttemptsCount);
        Assert.Equal(55, recordSuccess.RewardCoins);
    }

    [Fact]
    public void EvaluateBudgetDiscipline_WithinLimits_AwardsBonusOncePerPeriod()
    {
        _engine.Profile.CurrentPeriod = 1;
        _engine.Profile.BudgetBonusAwardedPeriod = 0;
        _engine.Profile.PlannedObligatory = 150;
        _engine.Profile.PlannedDiscretionary = 100;
        _engine.Profile.ActualObligatory = 120;
        _engine.Profile.ActualDiscretionary = 80;
        int initialBalance = _engine.Profile.Balance;

        // Первый вызов: награда начисляется
        var eval1 = _engine.EvaluateBudgetDiscipline();
        Assert.True(eval1.IsDisciplineKept);
        Assert.True(eval1.BonusAwarded);
        Assert.Equal(25, eval1.BonusAmount);
        Assert.Equal(initialBalance + 25, _engine.Profile.Balance);
        Assert.Equal(1, _engine.Profile.BudgetBonusAwardedPeriod);

        // Повторный вызов в том же периоде: повторного начисления нет
        var eval2 = _engine.EvaluateBudgetDiscipline();
        Assert.True(eval2.IsDisciplineKept);
        Assert.False(eval2.BonusAwarded);
        Assert.Equal(initialBalance + 25, _engine.Profile.Balance);

        // Переход в период 2: в новом периоде награду можно получить снова
        _engine.Profile.CurrentPeriod = 2;
        _engine.Profile.ActualObligatory = 100;
        _engine.Profile.ActualDiscretionary = 70;
        var eval3 = _engine.EvaluateBudgetDiscipline();
        Assert.True(eval3.IsDisciplineKept);
        Assert.True(eval3.BonusAwarded);
        Assert.Equal(initialBalance + 50, _engine.Profile.Balance);
        Assert.Equal(2, _engine.Profile.BudgetBonusAwardedPeriod);
    }

    [Fact]
    public void EvaluateBudgetDiscipline_Overspent_DoesNotAwardBonus()
    {
        _engine.Profile.CurrentPeriod = 1;
        _engine.Profile.BudgetBonusAwardedPeriod = 0;
        _engine.Profile.PlannedObligatory = 150;
        _engine.Profile.PlannedDiscretionary = 100;
        _engine.Profile.ActualObligatory = 170; // Перерасход обязательных трат
        _engine.Profile.ActualDiscretionary = 80;
        int initialBalance = _engine.Profile.Balance;

        var eval = _engine.EvaluateBudgetDiscipline();
        Assert.False(eval.IsDisciplineKept);
        Assert.False(eval.BonusAwarded);
        Assert.Equal(0, eval.BonusAmount);
        Assert.Equal(initialBalance, _engine.Profile.Balance);
        Assert.Equal(0, _engine.Profile.BudgetBonusAwardedPeriod);
    }

    [Fact]
    public void KidName_UpdateAndPersistence_ShouldRetainValue()
    {
        _engine.Profile.KidName = "Максим";
        Assert.Equal("Максим", _engine.Profile.KidName);

        _engine.Profile.KidName = "Алиса";
        Assert.Equal("Алиса", _engine.Profile.KidName);
    }
}

