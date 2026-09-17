namespace FinAPP.Models;

public class GlossaryTerm
{
    public string Term { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public string KidFriendlyExample { get; set; } = string.Empty;
    public string IconEmoji { get; set; } = "📖";
    public AgeGroup? TargetAge { get; set; } = null; // null = для всех возрастов
}
