using System.Collections.Generic;

namespace FinAPP.Models;

public enum OutfitType
{
    ClassicGreen,
    RoyalBlue,
    RubyRed
}

public enum AccessoryType
{
    None,
    Sunglasses,
    AcademicCap,
    Crown
}

public enum GrowthStage
{
    Baby = 1,     // Малыш Финни (периоды 1-2)
    Teen = 2,     // Подросток Финни (периоды 3-4)
    Master = 3    // Финни-Мастер (период 5+)
}

public class PeriodSummary
{
    public int PeriodNumber { get; set; }
    public int PlannedObligatory { get; set; }
    public int PlannedDiscretionary { get; set; }
    public int PlannedSavings { get; set; }
    public int ActualObligatory { get; set; }
    public int ActualDiscretionary { get; set; }
    public int ActualSavings { get; set; }
    public bool IsBudgetSuccess { get; set; }
    public string SummaryNotes { get; set; } = string.Empty;
}

public class PetProfile
{
    public string KidName { get; set; } = "Юный финансист";
    public string PetName { get; set; } = "Финни";
    
    public OutfitType Outfit { get; set; } = OutfitType.ClassicGreen;
    public AccessoryType Accessory { get; set; } = AccessoryType.None;
    public GrowthStage Stage { get; set; } = GrowthStage.Baby;

    // Жизненные показатели питомца (0 - 100%)
    public int Hunger { get; set; } = 85;
    public int Mood { get; set; } = 90;

    // Финансы
    public int Balance { get; set; } = 450;
    public int Savings { get; set; } = 150;
    public string SelectedGoalId { get; set; } = "goal_gadget";

    // Игровой цикл и периоды (по ТЗ: не менее 5 периодов в демо-режиме)
    public int CurrentPeriod { get; set; } = 1;
    public bool IsDemoMode { get; set; } = true;
    public bool IsOnboardingCompleted { get; set; } = true;

    // Настройки доступности (ТЗ п. 3.6)
    public bool AnimationsEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;

    // Статистика для кабинета взрослого
    public int TestsPassedCount { get; set; } = 0;
    public int BonusCoinsFromParent { get; set; } = 0;

    // Планирование текущего периода
    public int PlannedObligatory { get; set; } = 150;
    public int PlannedDiscretionary { get; set; } = 100;
    public int PlannedSavings { get; set; } = 100;
    public bool IsPlanConfirmed { get; set; } = false;

    // Фактическое исполнение текущего периода
    public int ActualObligatory { get; set; } = 0;
    public int ActualDiscretionary { get; set; } = 0;
    public int ActualSavings { get; set; } = 0;

    // История завершенных периодов
    public List<PeriodSummary> History { get; set; } = new();

    // Список выполненных ID заданий
    public List<string> CompletedTaskIds { get; set; } = new();

    // Алиасы для совместимости с кодом интерфейса
    public int SpentObligatory { get => ActualObligatory; set => ActualObligatory = value; }
    public int SpentDiscretionary { get => ActualDiscretionary; set => ActualDiscretionary = value; }
    public int CompletedTasksCount { get => TestsPassedCount; set => TestsPassedCount = value; }
}
