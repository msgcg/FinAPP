namespace FinAPP.Models;

public enum ExpenseCategory
{
    Obligatory,     // Обязательные расходы (еда, уход, здоровье)
    Discretionary   // Необязательные расходы (желания, игрушки, развлечения)
}

public class ShopItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ExpenseCategory Category { get; set; }
    public int Price { get; set; }
    public int HungerBoost { get; set; }
    public int MoodBoost { get; set; }
    public string IconEmoji { get; set; } = "📦";
    public string Description { get; set; } = string.Empty;

    public string CategoryName => Category == ExpenseCategory.Obligatory 
        ? "Обязательные расходы" 
        : "Желания и развлечения";

    public string EffectDescription
    {
        get
        {
            var effects = new System.Collections.Generic.List<string>();
            if (HungerBoost > 0) effects.Add($"+{HungerBoost}% к сытости");
            if (MoodBoost > 0) effects.Add($"+{MoodBoost}% к настроению");
            return effects.Count > 0 ? string.Join(", ", effects) : "Приятная покупка";
        }
    }
}
