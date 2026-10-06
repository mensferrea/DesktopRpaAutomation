using System.Text;
using DesktopRpa.Core.Models;

namespace DesktopRpa.Core.Services;

public sealed class TestDataGenerator : ITestDataGenerator
{
    public (string statementFile, string registryFile) GenerateSampleDataset(string outputDirectory, int totalRows = 25)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var statementPath = Path.Combine(outputDirectory, "bank_statement_sample.csv");
        var registryPath = Path.Combine(outputDirectory, "internal_registry_sample.csv");

        var stmtSb = new StringBuilder();
        var regSb = new StringBuilder();

        stmtSb.AppendLine("TransactionId;ValueDate;Amount;Currency;Type;PayerAccount;PayerInn;PayerName;PayeeAccount;PayeeInn;PayeeName;Purpose;ReferenceNumber");
        regSb.AppendLine("PaymentId;CreatedDate;PlannedAmount;Currency;DebtorAccount;DebtorInn;CreditorAccount;CreditorInn;CreditorName;PaymentPurpose;ContractNumber;OrderReference");

        var random = new Random(42);
        var baseDate = DateTime.Today.AddDays(-3);

        for (int i = 1; i <= totalRows; i++)
        {
            var txId = $"TX-2026-{1000 + i}";
            var payId = $"PAY-2026-{1000 + i}";
            var refNo = $"DOC-N-{5000 + i}";
            var date = baseDate.AddHours(i).ToString("yyyy-MM-dd HH:mm:ss");
            var amount = Math.Round((decimal)(random.Next(5000, 250000) + random.NextDouble()), 2);
            var payerInn = "7701102030";
            var payerName = "ООО Альфа-Трейдинг";
            var payerAcc = "40702810900000001234";

            var payeeInn = $"77{random.Next(10000000, 99999999)}";
            var payeeName = $"Контрагент №{i} (ООО)";
            var payeeAcc = $"40702810{random.Next(10000000, 99999999)}";
            var purpose = $"Оплата по счету №{i * 10} за услуги поставки оборудования";
            var contract = $"DOG-2026/{i}";

            if (i == 4)
            {
                var regAmount = amount + 1500.50m;
                stmtSb.AppendLine($"{txId};{date};{amount:F2};RUB;Debit;{payerAcc};{payerInn};{payerName};{payeeAcc};{payeeInn};{payeeName};{purpose};{refNo}");
                regSb.AppendLine($"{payId};{date};{regAmount:F2};RUB;{payerAcc};{payerInn};{payeeAcc};{payeeInn};{payeeName};{purpose};{contract};{refNo}");
                continue;
            }

            if (i == 8)
            {
                var alteredInn = "7799999999";
                stmtSb.AppendLine($"{txId};{date};{amount:F2};RUB;Debit;{payerAcc};{payerInn};{payerName};{payeeAcc};{payeeInn};{payeeName};{purpose};{refNo}");
                regSb.AppendLine($"{payId};{date};{amount:F2};RUB;{payerAcc};{payerInn};{payeeAcc};{alteredInn};{payeeName};{purpose};{contract};{refNo}");
                continue;
            }

            if (i == 12)
            {
                stmtSb.AppendLine($"{txId};{date};{amount:F2};RUB;Debit;{payerAcc};{payerInn};{payerName};{payeeAcc};{payeeInn};{payeeName};{purpose};{refNo}");
                continue;
            }

            if (i == 16)
            {
                regSb.AppendLine($"{payId};{date};{amount:F2};RUB;{payerAcc};{payerInn};{payeeAcc};{payeeInn};{payeeName};{purpose};{contract};{refNo}");
                continue;
            }

            stmtSb.AppendLine($"{txId};{date};{amount:F2};RUB;Debit;{payerAcc};{payerInn};{payerName};{payeeAcc};{payeeInn};{payeeName};{purpose};{refNo}");
            regSb.AppendLine($"{payId};{date};{amount:F2};RUB;{payerAcc};{payerInn};{payeeAcc};{payeeInn};{payeeName};{purpose};{contract};{refNo}");
        }

        File.WriteAllText(statementPath, stmtSb.ToString(), Encoding.UTF8);
        File.WriteAllText(registryPath, regSb.ToString(), Encoding.UTF8);

        return (statementPath, registryPath);
    }
}
