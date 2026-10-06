namespace DesktopRpa.Core.Models;

public sealed class ReconciliationItem
{
    public string ItemId { get; set; } = Guid.NewGuid().ToString("N");
    public string MatchKey { get; set; } = string.Empty;
    public MatchStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public decimal StatementAmount { get; set; }
    public decimal RegistryAmount { get; set; }
    public decimal Discrepancy => StatementAmount - RegistryAmount;
    public BankTransaction? StatementTransaction { get; set; }
    public RegistryPayment? InternalPayment { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
