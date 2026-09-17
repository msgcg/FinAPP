namespace FinAPP.Models;

public class FinancialGoal
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int TargetAmount { get; set; }
    public string IconEmoji { get; set; } = string.Empty;
    public string Icon => IconEmoji;
    public string IconImage { get; set; } = "ic_goal_scooter.png";
    public string Description { get; set; } = string.Empty;
    public bool IsCustom { get; set; } = false;
    public PetDeskType? LinkedDesk { get; set; }
    public PetPlatformType? LinkedPlatform { get; set; }
    public string? LinkedShopItemId { get; set; }

    public int GetProgressPercent(int currentSavings)
    {
        if (TargetAmount <= 0) return 0;
        return System.Math.Clamp((int)((currentSavings / (double)TargetAmount) * 100), 0, 100);
    }

    public int GetRemainingAmount(int currentSavings)
    {
        return System.Math.Max(0, TargetAmount - currentSavings);
    }

    // Расчет срока достижения цели в периодах на основе регулярного пополнения (п. 2.5.7 ТЗ)
    public int EstimateRemainingPeriods(int currentSavings, int avgDepositPerPeriod)
    {
        int remaining = GetRemainingAmount(currentSavings);
        if (remaining <= 0) return 0;
        if (avgDepositPerPeriod <= 0) avgDepositPerPeriod = 50; // базовое предположение
        return (int)System.Math.Ceiling(remaining / (double)avgDepositPerPeriod);
    }
}
