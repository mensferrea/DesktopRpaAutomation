namespace DesktopRpa.Core.Models;

public sealed class BankTransaction
{
    public string TransactionId { get; set; } = string.Empty;
    public DateTime ValueDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public TransactionType Type { get; set; }
    public string PayerAccount { get; set; } = string.Empty;
    public string PayerInn { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string PayeeAccount { get; set; } = string.Empty;
    public string PayeeInn { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
}
