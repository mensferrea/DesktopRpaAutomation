using System.Diagnostics;
using DesktopRpa.Core.Models;

namespace DesktopRpa.Core.Services;

public sealed class ReconciliationEngine : IReconciliationEngine
{
    public ReconciliationReport Reconcile(
        IReadOnlyList<BankTransaction> statementTransactions,
        IReadOnlyList<RegistryPayment> registryPayments,
        AutomationOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var report = new ReconciliationReport
        {
            Status = ReconciliationRunStatus.Running
        };

        var matchedStatementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var matchedRegistryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var registryByKey = new Dictionary<string, RegistryPayment>(StringComparer.OrdinalIgnoreCase);
        foreach (var reg in registryPayments)
        {
            var key = BuildPaymentKey(reg.PaymentId, reg.OrderReference, reg.CreditorInn);
            if (!registryByKey.ContainsKey(key))
            {
                registryByKey[key] = reg;
            }
        }

        foreach (var stmt in statementTransactions)
        {
            var key = BuildPaymentKey(stmt.TransactionId, stmt.ReferenceNumber, stmt.PayeeInn);

            RegistryPayment? matchedReg = null;

            if (registryByKey.TryGetValue(key, out var directMatch))
            {
                matchedReg = directMatch;
            }
            else
            {
                matchedReg = registryPayments.FirstOrDefault(r =>
                    !matchedRegistryIds.Contains(r.PaymentId) &&
                    (string.Equals(r.OrderReference, stmt.ReferenceNumber, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(r.PaymentId, stmt.TransactionId, StringComparison.OrdinalIgnoreCase)));
            }

            if (matchedReg != null)
            {
                matchedStatementIds.Add(stmt.TransactionId);
                matchedRegistryIds.Add(matchedReg.PaymentId);

                var diff = Math.Abs(stmt.Amount - matchedReg.PlannedAmount);
                MatchStatus status;
                string description;

                if (diff <= options.AllowedAmountTolerance)
                {
                    bool counterpartyMatches = string.IsNullOrWhiteSpace(stmt.PayeeInn) ||
                                                string.IsNullOrWhiteSpace(matchedReg.CreditorInn) ||
                                                string.Equals(stmt.PayeeInn, matchedReg.CreditorInn, StringComparison.OrdinalIgnoreCase);

                    if (counterpartyMatches)
                    {
                        status = MatchStatus.Matched;
                        description = "Полное совпадение реквизитов и суммы платежа";
                    }
                    else
                    {
                        status = MatchStatus.CounterpartyMismatch;
                        description = $"Расхождение ИНН получателя: выписка={stmt.PayeeInn}, реестр={matchedReg.CreditorInn}";
                    }
                }
                else
                {
                    status = MatchStatus.AmountMismatch;
                    description = $"Расхождение суммы: выписка={stmt.Amount:N2}, реестр={matchedReg.PlannedAmount:N2} (дельта {stmt.Amount - matchedReg.PlannedAmount:N2})";
                }

                report.Items.Add(new ReconciliationItem
                {
                    MatchKey = key,
                    Status = status,
                    StatusDescription = description,
                    StatementAmount = stmt.Amount,
                    RegistryAmount = matchedReg.PlannedAmount,
                    StatementTransaction = stmt,
                    InternalPayment = matchedReg
                });
            }
            else
            {
                report.Items.Add(new ReconciliationItem
                {
                    MatchKey = key,
                    Status = MatchStatus.MissingInInternalRegistry,
                    StatusDescription = "Операция присутствует в банковской выписке, но отсутствует во внутреннем реестре",
                    StatementAmount = stmt.Amount,
                    RegistryAmount = 0m,
                    StatementTransaction = stmt,
                    InternalPayment = null
                });
            }
        }

        foreach (var reg in registryPayments)
        {
            if (!matchedRegistryIds.Contains(reg.PaymentId))
            {
                var key = BuildPaymentKey(reg.PaymentId, reg.OrderReference, reg.CreditorInn);
                report.Items.Add(new ReconciliationItem
                {
                    MatchKey = key,
                    Status = MatchStatus.MissingInBankStatement,
                    StatusDescription = "Платеж зарегистрирован в реестре, но не обнаружен в выписке банка",
                    StatementAmount = 0m,
                    RegistryAmount = reg.PlannedAmount,
                    StatementTransaction = null,
                    InternalPayment = reg
                });
            }
        }

        stopwatch.Stop();

        report.Summary = new ReconciliationSummary
        {
            TotalStatementTransactions = statementTransactions.Count,
            TotalRegistryPayments = registryPayments.Count,
            MatchedCount = report.Items.Count(i => i.Status == MatchStatus.Matched),
            AmountMismatchCount = report.Items.Count(i => i.Status == MatchStatus.AmountMismatch),
            CounterpartyMismatchCount = report.Items.Count(i => i.Status == MatchStatus.CounterpartyMismatch),
            MissingInStatementCount = report.Items.Count(i => i.Status == MatchStatus.MissingInBankStatement),
            MissingInRegistryCount = report.Items.Count(i => i.Status == MatchStatus.MissingInInternalRegistry),
            TotalStatementAmount = statementTransactions.Sum(s => s.Amount),
            TotalRegistryAmount = registryPayments.Sum(r => r.PlannedAmount),
            TotalDiscrepancyAmount = report.Items.Sum(i => Math.Abs(i.Discrepancy)),
            ElapsedDuration = stopwatch.Elapsed
        };

        report.Status = ReconciliationRunStatus.Completed;
        return report;
    }

    private static string BuildPaymentKey(string id, string refNo, string inn)
    {
        if (!string.IsNullOrWhiteSpace(refNo))
        {
            return $"REF_{refNo.Trim()}";
        }

        if (!string.IsNullOrWhiteSpace(id))
        {
            return $"ID_{id.Trim()}";
        }

        return $"INN_{inn.Trim()}";
    }
}
