using System;
using System.Threading.Tasks;
using FinAPP.Models;

namespace FinAPP.Services;

public class GameEngine
{
    private readonly StorageService _storageService;
    public PetProfile Profile { get; private set; }

    public event Action? OnStateChanged;

    public GameEngine(StorageService storageService)
    {
        _storageService = storageService;
        Profile = _storageService.CreateInitialProfile();
    }

    public async Task InitializeAsync()
    {
        Profile = await _storageService.LoadProfileAsync();
        OnStateChanged?.Invoke();
    }

    public async Task SaveAsync()
    {
        await _storageService.SaveProfileAsync(Profile);
    }

    // Определение текущей эмоции Финни на основе показателей и контекста (ТЗ п. 2.5.10)
    public string CurrentEmotion
    {
        get
        {
            if (Profile.Hunger <= 40 || Profile.Mood <= 40)
                return "sad";
            if (Profile.Hunger >= 75 && Profile.Mood >= 75 && Profile.IsPlanConfirmed)
                return "proud";
            return "happy";
        }
    }

    // Текстовое объяснение состояния питомца (ТЗ п. 2.5.10)
    public string EmotionStatusExplanation
    {
        get
        {
            if (Profile.Hunger <= 40)
                return $"{Profile.PetName} проголодался (сытость {Profile.Hunger}%)! Нужен питательный обед из обязательных расходов.";
            if (Profile.Mood <= 40)
                return $"{Profile.PetName} заскучал (настроение {Profile.Mood}%)! Поиграйте или купите игрушку из желаний.";
            if (Profile.Hunger >= 75 && Profile.Mood >= 75)
                return $"{Profile.PetName} сыт, счастлив и гордится вашим грамотным бюджетом!";
            return $"{Profile.PetName} в отличном настроении и готов к новым финансовым открытиям!";
        }
    }

    // Совершение покупки (ТЗ п. 2.5.6)
    public (bool Success, string Message) PurchaseItem(ShopItem item)
    {
        if (Profile.Balance < item.Price)
        {
            int deficit = item.Price - Profile.Balance;
            return (false, $"Недостаточно монет!\nВам не хватает {deficit} монет.\n\nЧто можно сделать:\n1. Пройти обучающие финансовые кейсы и получить вознаграждение.\n2. Дождаться карманных денег в следующем периоде.\n3. Скорректировать необязательные траты.");
        }

        Profile.Balance -= item.Price;
        Profile.Hunger = Math.Clamp(Profile.Hunger + item.HungerBoost, 0, 100);
        Profile.Mood = Math.Clamp(Profile.Mood + item.MoodBoost, 0, 100);

        if (item.Category == ExpenseCategory.Obligatory)
        {
            Profile.ActualObligatory += item.Price;
        }
        else
        {
            Profile.ActualDiscretionary += item.Price;
        }

        if (item.LinkedDesk.HasValue)
        {
            Profile.UnlockDesk(item.LinkedDesk.Value);
            Profile.Desk = item.LinkedDesk.Value;
        }
        if (item.LinkedPlatform.HasValue)
        {
            Profile.UnlockPlatform(item.LinkedPlatform.Value);
            Profile.Platform = item.LinkedPlatform.Value;
        }

        OnStateChanged?.Invoke();
        _ = SaveAsync();

        return (true, $"Успешно куплено: «{item.Name}»! {item.EffectDescription}.\nСписано: {item.Price} монет.");
    }

    // Планирование бюджета на период (ТЗ п. 2.5.5)
    public (bool Success, string Message) ConfirmBudgetPlan(int obligatory, int discretionary, int savings)
    {
        int totalPlan = obligatory + discretionary + savings;
        if (totalPlan > Profile.Balance)
        {
            return (false, $"Сумма плана ({totalPlan} монет) превышает ваш доступный баланс ({Profile.Balance} монет)!\nУменьшите траты или сбережения, чтобы уложиться в бюджет.");
        }

        Profile.PlannedObligatory = obligatory;
        Profile.PlannedDiscretionary = discretionary;
        Profile.PlannedSavings = savings;
        Profile.IsPlanConfirmed = true;

        OnStateChanged?.Invoke();
        _ = SaveAsync();

        int remainder = Profile.Balance - totalPlan;
        return (true, $"Бюджет периода {Profile.CurrentPeriod} успешно утвержден!\n" +
                      $"• Обязательные расходы: {obligatory} монет\n" +
                      $"• Желания: {discretionary} монет\n" +
                      $"• Накопления: {savings} монет\n" +
                      $"• Свободный остаток: {remainder} монет");
    }

    // Пополнение копилки / цели (ТЗ п. 2.5.7)
    public (bool Success, string Message) DepositToSavings(int amount, FinancialGoal goal)
    {
        if (amount <= 0)
            return (false, "Введите корректную сумму для пополнения.");

        if (Profile.Balance < amount)
            return (false, $"Недостаточно средств на балансе. У вас {Profile.Balance} монет, а требуется {amount}.");

        Profile.Balance -= amount;
        Profile.Savings += amount;
        Profile.ActualSavings += amount;
        Profile.Mood = Math.Min(100, Profile.Mood + 10); // радость от сбережений

        OnStateChanged?.Invoke();
        _ = SaveAsync();

        int percent = goal.GetProgressPercent(Profile.Savings);
        int remainingPeriods = goal.EstimateRemainingPeriods(Profile.Savings, 50);

        string goalAchievedMsg = Profile.Savings >= goal.TargetAmount 
            ? $"\n\nУРА! ЦЕЛЬ «{goal.Title}» ДОСТИГНУТА! Вы накопили всю сумму!" 
            : $"\nПрогресс цели: {percent}%. Осталось накопить: {goal.GetRemainingAmount(Profile.Savings)} монет (~{remainingPeriods} периодов).";

        return (true, $"В копилку добавлено +{amount} монет!{goalAchievedMsg}");
    }

    // Снятие из копилки с предупреждением о последствиях (ТЗ п. 2.5.7)
    public (bool Success, string Message) WithdrawFromSavings(int amount, FinancialGoal goal)
    {
        if (amount <= 0)
            return (false, "Введите корректную сумму для снятия.");

        if (Profile.Savings < amount)
            return (false, $"В копилке недостаточно средств. Накоплено {Profile.Savings} монет, а запрошено {amount}.");

        Profile.Savings -= amount;
        Profile.Balance += amount;
        Profile.ActualSavings = Math.Max(0, Profile.ActualSavings - amount);

        OnStateChanged?.Invoke();
        _ = SaveAsync();

        int remainingPeriods = goal.EstimateRemainingPeriods(Profile.Savings, 50);
        return (true, $"Из копилки снято {amount} монет в кошелек.\n" +
                      $"Внимание: теперь срок достижения цели «{goal.Title}» увеличился (~{remainingPeriods} периодов при обычном темпе).");
    }

    public const int TaskMoodPenalty = 15;

    // Образовательные задания с немедленной обратной связью (ТЗ п. 2.5.8)
    public (bool Success, string Message) CompleteTask(FinancialTask task, TaskOption option, bool isRetry = false)
    {
        if (option.IsCorrect)
        {
            Profile.Balance += option.RewardCoins;
            Profile.CompletedTasksCount++;
            Profile.CompletedTaskIds ??= new();
            if (!Profile.CompletedTaskIds.Contains(task.Id))
            {
                Profile.CompletedTaskIds.Add(task.Id);
            }

            int moodGain = isRetry ? (15 + TaskMoodPenalty) : 15;
            Profile.Mood = Math.Min(100, Profile.Mood + moodGain);

            OnStateChanged?.Invoke();
            _ = SaveAsync();

            string refundNotice = isRetry ? $"\nНастроение {Profile.PetName} полностью восстановлено!" : "";
            return (true, $"Отлично! Ответ верный!\nВам начислено +{option.RewardCoins} монет.{refundNotice}\n\nРазбор: {option.Explanation}");
        }
        else
        {
            // Ошибка снижает настроение питомца, если штраф ещё не начислялся в этой попытке
            if (!isRetry)
            {
                Profile.Mood = Math.Max(10, Profile.Mood - TaskMoodPenalty);
            }
            OnStateChanged?.Invoke();
            _ = SaveAsync();
            return (false, $"Не совсем так.\n\nРазбор эксперта: {option.Explanation}\n\nПопробуйте ещё раз или выберите другое задание!");
        }
    }

    // Начисление карманных денег или бонуса от родителя (ТЗ п. 2.5.4, 2.5.12)
    public void AddIncome(int amount, string source)
    {
        Profile.Balance += amount;
        if (source.Contains("родител", StringComparison.OrdinalIgnoreCase))
        {
            Profile.BonusCoinsFromParent += amount;
        }

        OnStateChanged?.Invoke();
        _ = SaveAsync();
    }

    // Переключение возрастной группы (ТЗ п. 2.2: возрастная уместность)
    public void SetAgeGroup(AgeGroup ageGroup)
    {
        Profile.AgeGroup = ageGroup;
        OnStateChanged?.Invoke();
        _ = SaveAsync();
    }

    public List<FinancialTask> GetTasksForCurrentAge()
    {
        return ContentRepository.GetFinancialTasks(Profile.AgeGroup);
    }

    public List<GlossaryTerm> GetGlossaryForCurrentAge()
    {
        return ContentRepository.GetGlossaryTerms(Profile.AgeGroup);
    }

    // Переход к следующему периоду (ТЗ п. 2.5.10, 2.6: не менее 5 периодов в демо-режиме)
    public string AdvanceToNextPeriod()
    {
        int prevPeriod = Profile.CurrentPeriod;
        
        // В демо-режиме, если игрок просто перелистывает периоды, генерируем реалистичные траты и накопления
        if (Profile.IsDemoMode && Profile.ActualObligatory == 0 && Profile.ActualDiscretionary == 0)
        {
            Profile.ActualObligatory = Math.Min(Profile.PlannedObligatory, 60 + (prevPeriod * 15) % 45);
            Profile.ActualDiscretionary = Math.Min(Profile.PlannedDiscretionary, 35 + (prevPeriod * 20) % 45);
            if (Profile.ActualSavings == 0)
            {
                int demoSav = 30 + (prevPeriod * 10) % 30;
                Profile.ActualSavings = demoSav;
                Profile.Savings += demoSav;
            }
        }

        // Анализ соблюдения бюджета
        bool isBudgetKept = Profile.ActualObligatory <= Profile.PlannedObligatory * 1.2 &&
                            Profile.ActualDiscretionary <= Profile.PlannedDiscretionary * 1.2;

        // Начисление сложного процента на сбережения в копилке (ТЗ п. 2.5.7: банковский процент +5%)
        int interest = 0;
        if (Profile.Savings > 0)
        {
            interest = Math.Max(1, (int)Math.Round(Profile.Savings * 0.05));
            Profile.Savings += interest;
        }

        var summary = new PeriodSummary
        {
            PeriodNumber = prevPeriod,
            PlannedObligatory = Profile.PlannedObligatory,
            PlannedDiscretionary = Profile.PlannedDiscretionary,
            PlannedSavings = Profile.PlannedSavings,
            ActualObligatory = Profile.ActualObligatory,
            ActualDiscretionary = Profile.ActualDiscretionary,
            ActualSavings = Profile.ActualSavings,
            EndPeriodSavings = Profile.Savings,
            InterestEarned = interest,
            IsBudgetSuccess = isBudgetKept,
            SummaryNotes = isBudgetKept ? "Бюджет соблюден отлично!" : "Траты превысили запланированный план."
        };
        Profile.History.Add(summary);

        // Переход периода
        Profile.CurrentPeriod++;

        // Развитие и рост питомца (в демо-режиме переключается по периодам, в обычном режиме — по достигнутым целям)
        if (Profile.IsDemoMode)
        {
            if (Profile.CurrentPeriod >= 5)
            {
                Profile.Stage = GrowthStage.Master;
            }
            else if (Profile.CurrentPeriod >= 3)
            {
                Profile.Stage = GrowthStage.Teen;
            }
        }
        else
        {
            CheckGoalEvolution();
        }

        // Начисление карманных денег за новый период (+150 монет)
        const int pocketMoney = 150;
        Profile.Balance += pocketMoney;

        // Сброс трат периода
        Profile.ActualObligatory = 0;
        Profile.ActualDiscretionary = 0;
        Profile.ActualSavings = 0;
        Profile.IsPlanConfirmed = false;

        // Небольшое уменьшение сытости/настроения в начале нового цикла
        Profile.Hunger = Math.Max(40, Profile.Hunger - 25);
        Profile.Mood = Math.Max(40, Profile.Mood - 20);

        OnStateChanged?.Invoke();
        _ = SaveAsync();

        string growthMsg = Profile.Stage switch
        {
            GrowthStage.Master => $"{Profile.PetName} достиг высшей стадии развития: «{Profile.PetName}-Мастер»!",
            GrowthStage.Teen => $"{Profile.PetName} повзрослел и стал подростком-юниором!",
            _ => $"{Profile.PetName} активно растет и развивается."
        };

        string interestLine = interest > 0
            ? $"• Банковский процент на копилку (+5%): +{interest} монет.\n"
            : string.Empty;

        return $"Наступил Период #{Profile.CurrentPeriod}!\n" +
               $"• Начислено карманных денег: +{pocketMoney} монет.\n" +
               interestLine +
               $"• {growthMsg}\n" +
               $"• Не забудьте составить план личного бюджета на новый период!";
    }

    // Сброс тестового профиля для экспертов хакатона (ТЗ п. 2.5.13)
    public void ResetDemoProfile()
    {
        Profile = _storageService.ResetToDemoProfile();
        OnStateChanged?.Invoke();
    }

    public void ResetData() => ResetDemoProfile();

    public string GetPlanVsFactAnalysis()
    {
        bool obligOk = Profile.ActualObligatory <= (Profile.PlannedObligatory > 0 ? Profile.PlannedObligatory : 250);
        bool discOk = Profile.ActualDiscretionary <= (Profile.PlannedDiscretionary > 0 ? Profile.PlannedDiscretionary : 150);
        return $"План/Факт: Обязательные {Profile.ActualObligatory}/{Profile.PlannedObligatory} монет ({(obligOk ? "В норме" : "Превышение")}), " +
               $"Желания {Profile.ActualDiscretionary}/{Profile.PlannedDiscretionary} монет ({(discOk ? "В норме" : "Превышение")}).";
    }

    // Автовзросление котика по достигнутым целям (0-2 цели - Малыш, 3-8 целей - Юниор, 9+ целей - Мастер)
    public bool CheckGoalEvolution()
    {
        var oldStage = Profile.Stage;
        if (Profile.GoalsAchievedCount >= 9)
        {
            Profile.Stage = GrowthStage.Master;
        }
        else if (Profile.GoalsAchievedCount >= 3)
        {
            Profile.Stage = GrowthStage.Teen;
        }
        else
        {
            Profile.Stage = GrowthStage.Baby;
        }
        return Profile.Stage != oldStage;
    }
}
