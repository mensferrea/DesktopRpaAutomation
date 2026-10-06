using System.Text;
using DesktopRpa.Core.Models;

namespace DesktopRpa.Core.Services;

public sealed class ReportExporter : IReportExporter
{
    public async Task<string> ExportCsvAsync(ReconciliationReport report, string filePath, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var sb = new StringBuilder();
        sb.AppendLine("ItemId;Status;Description;StatementAmount;RegistryAmount;Discrepancy;TransactionId;PaymentId;Payer;Payee;Purpose");

        foreach (var item in report.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var txId = item.StatementTransaction?.TransactionId ?? string.Empty;
            var regId = item.InternalPayment?.PaymentId ?? string.Empty;
            var payer = EscapeCsv(item.StatementTransaction?.PayerName ?? item.InternalPayment?.DebtorAccount ?? string.Empty);
            var payee = EscapeCsv(item.StatementTransaction?.PayeeName ?? item.InternalPayment?.CreditorName ?? string.Empty);
            var purpose = EscapeCsv(item.StatementTransaction?.Purpose ?? item.InternalPayment?.PaymentPurpose ?? string.Empty);
            var desc = EscapeCsv(item.StatusDescription);

            sb.AppendLine($"{item.ItemId};{item.Status};\"{desc}\";{item.StatementAmount:F2};{item.RegistryAmount:F2};{item.Discrepancy:F2};{txId};{regId};\"{payer}\";\"{payee}\";\"{purpose}\"");
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8, cancellationToken);
        return filePath;
    }

    public async Task<string> ExportMarkdownSummaryAsync(ReconciliationReport report, string filePath, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Протокол автоматизированной сверки платежей RPA");
        sb.AppendLine();
        sb.AppendLine($"- **Идентификатор прогона (Run ID):** `{report.RunId}`");
        sb.AppendLine($"- **Дата и время сверки:** {report.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"- **Статус завершения:** {report.Status}");
        sb.AppendLine($"- **Время выполнения:** {report.Summary.ElapsedDuration.TotalMilliseconds:F1} мс");
        sb.AppendLine();
        sb.AppendLine("## Сводная ведомость");
        sb.AppendLine();
        sb.AppendLine("| Показатель | Значение |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| Обработано операций по выписке | {report.Summary.TotalStatementTransactions} |");
        sb.AppendLine($"| Обработано записей реестра | {report.Summary.TotalRegistryPayments} |");
        sb.AppendLine($"| Успешно сопоставлено | {report.Summary.MatchedCount} |");
        sb.AppendLine($"| Расхождение по сумме | {report.Summary.AmountMismatchCount} |");
        sb.AppendLine($"| Расхождение по реквизитам контрагента | {report.Summary.CounterpartyMismatchCount} |");
        sb.AppendLine($"| Не найдено в банковской выписке | {report.Summary.MissingInStatementCount} |");
        sb.AppendLine($"| Не найдено во внутреннем реестре | {report.Summary.MissingInRegistryCount} |");
        sb.AppendLine($"| Общий оборот по выписке | {report.Summary.TotalStatementAmount:N2} RUB |");
        sb.AppendLine($"| Общий оборот по реестру | {report.Summary.TotalRegistryAmount:N2} RUB |");
        sb.AppendLine($"| Суммарное расхождение | {report.Summary.TotalDiscrepancyAmount:N2} RUB |");
        sb.AppendLine();
        sb.AppendLine("## Выявленные расхождения и инциденты");
        sb.AppendLine();
        sb.AppendLine("| Статус | Выписка (Сумма) | Реестр (Сумма) | Дельта | Детализация |");
        sb.AppendLine("|---|---|---|---|---|");

        var issues = report.Items.Where(i => i.Status != MatchStatus.Matched).ToList();
        if (issues.Count == 0)
        {
            sb.AppendLine("| - | - | - | - | Расхождений не обнаружено |");
        }
        else
        {
            foreach (var item in issues)
            {
                sb.AppendLine($"| `{item.Status}` | {item.StatementAmount:N2} | {item.RegistryAmount:N2} | {item.Discrepancy:N2} | {item.StatusDescription} |");
            }
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8, cancellationToken);
        return filePath;
    }

    private static string EscapeCsv(string val) => val.Replace("\"", "\"\"");
}
