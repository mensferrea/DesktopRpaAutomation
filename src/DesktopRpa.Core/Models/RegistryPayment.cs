namespace DesktopRpa.Core.Models;

public sealed class RegistryPayment
{
    public string PaymentId { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public decimal PlannedAmount { get; set; }
    public string Currency { get; set; } = "RUB";
    public string DebtorAccount { get; set; } = string.Empty;
    public string DebtorInn { get; set; } = string.Empty;
    public string CreditorAccount { get; set; } = string.Empty;
    public string CreditorInn { get; set; } = string.Empty;
    public string CreditorName { get; set; } = string.Empty;
    public string PaymentPurpose { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public string OrderReference { get; set; } = string.Empty;
}
