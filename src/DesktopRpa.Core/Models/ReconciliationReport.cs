namespace DesktopRpa.Core.Models;

public sealed class ReconciliationSummary
{
    public int TotalStatementTransactions { get; set; }
    public int TotalRegistryPayments { get; set; }
    public int MatchedCount { get; set; }
    public int AmountMismatchCount { get; set; }
    public int CounterpartyMismatchCount { get; set; }
    public int MissingInStatementCount { get; set; }
    public int MissingInRegistryCount { get; set; }
    public decimal TotalStatementAmount { get; set; }
    public decimal TotalRegistryAmount { get; set; }
    public decimal TotalDiscrepancyAmount { get; set; }
    public TimeSpan ElapsedDuration { get; set; }
}

public sealed class ReconciliationReport
{
    public string RunId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ReconciliationRunStatus Status { get; set; } = ReconciliationRunStatus.NotStarted;
    public ReconciliationSummary Summary { get; set; } = new();
    public List<ReconciliationItem> Items { get; set; } = [];
}
