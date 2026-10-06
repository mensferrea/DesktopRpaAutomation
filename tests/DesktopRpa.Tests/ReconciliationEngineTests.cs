using DesktopRpa.Core.Models;
using DesktopRpa.Core.Services;
using Xunit;

namespace DesktopRpa.Tests;

public class ReconciliationEngineTests
{
    [Fact]
    public void Reconcile_MatchingRecords_ReturnsMatchedStatus()
    {
        var engine = new ReconciliationEngine();
        var statement = new List<BankTransaction>
        {
            new()
            {
                TransactionId = "TX-01",
                ReferenceNumber = "REF-100",
                PayeeInn = "7701234567",
                Amount = 15000.00m
            }
        };

        var registry = new List<RegistryPayment>
        {
            new()
            {
                PaymentId = "PAY-01",
                OrderReference = "REF-100",
                CreditorInn = "7701234567",
                PlannedAmount = 15000.00m
            }
        };

        var options = new AutomationOptions();
        var report = engine.Reconcile(statement, registry, options);

        Assert.Equal(1, report.Summary.MatchedCount);
        Assert.Equal(0, report.Summary.AmountMismatchCount);
        Assert.Equal(MatchStatus.Matched, report.Items[0].Status);
        Assert.Equal(0m, report.Items[0].Discrepancy);
    }

    [Fact]
    public void Reconcile_AmountMismatch_IdentifiesDifference()
    {
        var engine = new ReconciliationEngine();
        var statement = new List<BankTransaction>
        {
            new()
            {
                TransactionId = "TX-02",
                ReferenceNumber = "REF-200",
                PayeeInn = "7701234567",
                Amount = 15000.00m
            }
        };

        var registry = new List<RegistryPayment>
        {
            new()
            {
                PaymentId = "PAY-02",
                OrderReference = "REF-200",
                CreditorInn = "7701234567",
                PlannedAmount = 14500.00m
            }
        };

        var options = new AutomationOptions();
        var report = engine.Reconcile(statement, registry, options);

        Assert.Equal(0, report.Summary.MatchedCount);
        Assert.Equal(1, report.Summary.AmountMismatchCount);
        Assert.Equal(MatchStatus.AmountMismatch, report.Items[0].Status);
        Assert.Equal(500.00m, report.Items[0].Discrepancy);
    }

    [Fact]
    public void Reconcile_CounterpartyMismatch_FlagsIncorrectInn()
    {
        var engine = new ReconciliationEngine();
        var statement = new List<BankTransaction>
        {
            new()
            {
                TransactionId = "TX-03",
                ReferenceNumber = "REF-300",
                PayeeInn = "7701234567",
                Amount = 20000.00m
            }
        };

        var registry = new List<RegistryPayment>
        {
            new()
            {
                PaymentId = "PAY-03",
                OrderReference = "REF-300",
                CreditorInn = "7799999999",
                PlannedAmount = 20000.00m
            }
        };

        var options = new AutomationOptions();
        var report = engine.Reconcile(statement, registry, options);

        Assert.Equal(1, report.Summary.CounterpartyMismatchCount);
        Assert.Equal(MatchStatus.CounterpartyMismatch, report.Items[0].Status);
    }

    [Fact]
    public void Reconcile_MissingEntries_IdentifiedCorrectly()
    {
        var engine = new ReconciliationEngine();
        var statement = new List<BankTransaction>
        {
            new()
            {
                TransactionId = "TX-10",
                ReferenceNumber = "REF-ONLY-STMT",
                Amount = 1000m
            }
        };

        var registry = new List<RegistryPayment>
        {
            new()
            {
                PaymentId = "PAY-20",
                OrderReference = "REF-ONLY-REG",
                PlannedAmount = 2000m
            }
        };

        var options = new AutomationOptions();
        var report = engine.Reconcile(statement, registry, options);

        Assert.Equal(1, report.Summary.MissingInRegistryCount);
        Assert.Equal(1, report.Summary.MissingInStatementCount);
        Assert.Equal(2, report.Items.Count);
    }
}
