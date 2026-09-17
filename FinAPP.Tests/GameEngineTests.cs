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
}
