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
    public void CompleteTask_IncorrectOption_ShouldExplainWithoutPunishment()
    {
        _engine.Profile.Balance = 100;
        _engine.Profile.TestsPassedCount = 0;

        var task = new FinancialTask { Id = "test_task", Title = "Тестовый кейс" };
        var option = new TaskOption { Text = "Неверно", IsCorrect = false, Explanation = "Пояснение ошибки" };

        var result = _engine.CompleteTask(task, option);

        Assert.False(result.Success);
        Assert.Equal(100, _engine.Profile.Balance); // деньги не отбираются
        Assert.Equal(0, _engine.Profile.TestsPassedCount);
        Assert.Contains("Пояснение ошибки", result.Message);
    }

    [Fact]
    public void AdvancePeriod_FiveConsecutivePeriods_ShouldTriggerPetEvolutionToMaster()
    {
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
    public void ContentRepository_ShouldMeetAllMinimumVolumesOfSpecification()
    {
        // п. 2.6 ТЗ: Покупки не менее 8 позиций двух типов
        var items = ContentRepository.GetShopItems();
        Assert.True(items.Count >= 8);
        Assert.True(items.Any(i => i.Category == ExpenseCategory.Obligatory));
        Assert.True(items.Any(i => i.Category == ExpenseCategory.Discretionary));

        // п. 2.6 ТЗ: Задания не менее 6 заданий по 3 темам
        var tasks = ContentRepository.GetFinancialTasks();
        Assert.True(tasks.Count >= 6);
        Assert.True(tasks.Any(t => t.Topic == TaskTopic.BudgetPlanning));
        Assert.True(tasks.Any(t => t.Topic == TaskTopic.SavingsAndReserve));
        Assert.True(tasks.Any(t => t.Topic == TaskTopic.PaymentsAndSecurity));

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
        Assert.Equal(6, juniorTasks.Count);
        Assert.All(juniorTasks, t => Assert.Equal(AgeGroup.Junior7_8, t.TargetAge));
        Assert.Contains(juniorTasks, t => t.Topic == TaskTopic.BudgetPlanning);
        Assert.Contains(juniorTasks, t => t.Topic == TaskTopic.SavingsAndReserve);
        Assert.Contains(juniorTasks, t => t.Topic == TaskTopic.PaymentsAndSecurity);

        // Проверяем 9–11 лет (Senior)
        _engine.SetAgeGroup(AgeGroup.Senior9_11);
        Assert.Equal(AgeGroup.Senior9_11, _engine.Profile.AgeGroup);
        var seniorTasks = _engine.GetTasksForCurrentAge();
        Assert.Equal(6, seniorTasks.Count);
        Assert.All(seniorTasks, t => Assert.Equal(AgeGroup.Senior9_11, t.TargetAge));
        Assert.Contains(seniorTasks, t => t.Topic == TaskTopic.BudgetPlanning);
        Assert.Contains(seniorTasks, t => t.Topic == TaskTopic.SavingsAndReserve);
        Assert.Contains(seniorTasks, t => t.Topic == TaskTopic.PaymentsAndSecurity);

        // Всего заданий 12
        var allTasks = ContentRepository.GetFinancialTasks();
        Assert.Equal(12, allTasks.Count);
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
}
