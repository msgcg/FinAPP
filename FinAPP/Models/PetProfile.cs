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

public enum PetPlatformType
{
    Emerald = 0,   // Изумрудный кристалл (неоновый изумрудно-мятный свет)
    Stars = 1,     // Звёздная дорожка (золотой кристалл со звёздами)
    Flowers = 2,   // Цветочная полянка (весенняя полянка с цветочными акцентами)
    Cosmic = 3,    // Космический неон (кибер-подиум)
    Cloud = 4      // Облако накоплений (небесно-голубой подиум)
}

public enum PetDeskType
{
    None = 0,      // Без стола (свободная сцена)
    Modern = 1,    // Стол IT-финансиста (ноутбук, лампа, гаджеты)
    Artisan = 2,   // Творческий стол (краски, палитра, холст)
    Market = 3,    // Лавка предпринимателя (витрина, монеты, вывеска)
    Maker = 4,     // Верстак инженера (инструменты, шестерни, чертежи)
    Reading = 5,   // Кабинет профессора (книги, глобус, свитки)
    Botanical = 6  // Эко-стол биолога (комнатные растения, лейка)
}

public enum GrowthStage
{
    Baby = 1,     // Малыш Финни (периоды 1-2)
    Teen = 2,     // Подросток Финни (периоды 3-4)
    Master = 3    // Финни-Мастер (период 5+)
}

public enum AgeGroup
{
    Junior7_8 = 0,  // 7–8 лет (1–2 класс)
    Senior9_11 = 1  // 9–11 лет (3–5 класс)
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
    public int EndPeriodSavings { get; set; } = 0;
    public int InterestEarned { get; set; } = 0;
    public bool IsBudgetSuccess { get; set; }
    public string SummaryNotes { get; set; } = string.Empty;
}

public class PetProfile
{
    public string KidName { get; set; } = "Юный финансист";
    public string PetName { get; set; } = "Финни";
    
    public AgeGroup AgeGroup { get; set; } = AgeGroup.Junior7_8;
    public OutfitType Outfit { get; set; } = OutfitType.ClassicGreen;
    public AccessoryType Accessory { get; set; } = AccessoryType.None;
    public PetPlatformType Platform { get; set; } = PetPlatformType.Flowers;
    public PetDeskType Desk { get; set; } = PetDeskType.None;
    public GrowthStage Stage { get; set; } = GrowthStage.Baby;

    // Жизненные показатели питомца (0 - 100%)
    public int Hunger { get; set; } = 85;
    public int Mood { get; set; } = 90;

    // Финансы
    public int Balance { get; set; } = 450;
    public int Savings { get; set; } = 150;
    public string SelectedGoalId { get; set; } = "goal_desk_modern";
    public List<FinancialGoal> CustomGoals { get; set; } = new();

    // Количество успешно достигнутых целей накопления (для автовзросления: 3 цели -> Юниор, 9 целей -> Мастер)
    public int GoalsAchievedCount { get; set; } = 0;
    public List<string> CompletedGoalIds { get; set; } = new();

    // Купленные нерегулярные товары (игрушки, предметы интерьера) текущей стадии роста (п. 2 ТЗ)
    public List<string> PurchasedNonRegularItemIds { get; set; } = new();

    public bool IsNonRegularItemPurchased(string itemId)
    {
        return PurchasedNonRegularItemIds != null && PurchasedNonRegularItemIds.Contains(itemId);
    }

    public void ClearNonRegularPurchases()
    {
        PurchasedNonRegularItemIds?.Clear();
    }

    // Разблокированные подиумы и рабочие столы (стартовые Flowers и None бесплатны)
    public List<string> UnlockedPlatforms { get; set; } = new() { "Flowers" };
    public List<string> UnlockedDesks { get; set; } = new() { "None" };

    // Все подиумы полностью бесплатны и доступны детям
    public bool IsPlatformUnlocked(PetPlatformType platform) => true;

    public bool IsDeskUnlocked(PetDeskType desk)
    {
        if (desk == PetDeskType.None) return true;
        return UnlockedDesks != null && UnlockedDesks.Contains(desk.ToString());
    }

    public void UnlockPlatform(PetPlatformType platform)
    {
        UnlockedPlatforms ??= new List<string> { "Flowers" };
        if (!UnlockedPlatforms.Contains(platform.ToString()))
            UnlockedPlatforms.Add(platform.ToString());
    }

    public void UnlockDesk(PetDeskType desk)
    {
        UnlockedDesks ??= new List<string> { "None" };
        if (!UnlockedDesks.Contains(desk.ToString()))
            UnlockedDesks.Add(desk.ToString());
    }

    // Игровой цикл и периоды (по ТЗ: не менее 5 периодов в демо-режиме)
    public int CurrentPeriod { get; set; } = 1;
    public bool IsDemoMode { get; set; } = false;
    public bool HasMigratedDemoDefault { get; set; } = false;
    public bool IsOnboardingCompleted { get; set; } = false;

    // Настройки доступности (ТЗ п. 3.6)
    public bool AnimationsEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;

    // Статистика для кабинета взрослого
    public int TestsPassedCount { get; set; } = 0;
    public int BonusCoinsFromParent { get; set; } = 0;
    public string ParentPin { get; set; } = string.Empty;

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
