using System.Collections.Generic;

namespace FinAPP.Models;

public enum TaskTopic
{
    BudgetPlanning,         // Планирование личного бюджета (компетенция 1, 3)
    SavingsAndReserve,      // Формирование сбережений и подушки (компетенция 4)
    PaymentsAndSecurity     // Платежи, покупки и кибербезопасность (компетенция 2, 5)
}

public class TaskOption
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public int RewardCoins { get; set; } = 100;
}

public class FinancialTask
{
    public string Id { get; set; } = string.Empty;
    public AgeGroup TargetAge { get; set; } = AgeGroup.Junior7_8;
    public TaskTopic Topic { get; set; }
    public string Title { get; set; } = string.Empty;
    
    // Ссылка на стандарт Единой рамки компетенций Минфина/Банка России
    public string CompetencyReference { get; set; } = string.Empty;

    public string ScenarioDescription { get; set; } = string.Empty;
    public List<TaskOption> Options { get; set; } = new();

    public string TopicDisplayName => Topic switch
    {
        TaskTopic.BudgetPlanning => "Планирование бюджета",
        TaskTopic.SavingsAndReserve => "Сбережения и подушка",
        TaskTopic.PaymentsAndSecurity => "Платежи и безопасность",
        _ => "Финансы"
    };
}

public class TaskCompletionRecord
{
    public string TaskId { get; set; } = string.Empty;
    public string TaskTitle { get; set; } = string.Empty;
    public TaskTopic Topic { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public string CompetencyReference { get; set; } = string.Empty;
    public int PeriodNumber { get; set; } = 1;
    public DateTime CompletedAt { get; set; } = DateTime.Now;
    public int RewardCoins { get; set; } = 0;
}

