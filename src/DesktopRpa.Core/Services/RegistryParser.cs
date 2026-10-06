using System.Globalization;
using DesktopRpa.Core.Models;

namespace DesktopRpa.Core.Services;

public sealed class RegistryParser : IRegistryParser
{
    public async Task<IReadOnlyList<RegistryPayment>> ParseRegistryAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Registry file not found: {filePath}", filePath);
        }

        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        var payments = new List<RegistryPayment>();

        for (int i = 0; i < lines.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line) || (i == 0 && (line.StartsWith("Id", StringComparison.OrdinalIgnoreCase) || line.StartsWith("PaymentId", StringComparison.OrdinalIgnoreCase))))
            {
                continue;
            }

            var parts = ParseCsvLine(line);
            if (parts.Count < 4)
            {
                continue;
            }

            var payment = new RegistryPayment
            {
                PaymentId = parts[0].Trim(),
                CreatedDate = DateTime.TryParse(parts[1].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : DateTime.UtcNow,
                PlannedAmount = decimal.TryParse(parts[2].Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var amt) ? amt : 0m,
                Currency = parts.Count > 3 && !string.IsNullOrWhiteSpace(parts[3]) ? parts[3].Trim() : "RUB",
                DebtorAccount = parts.Count > 4 ? parts[4].Trim() : string.Empty,
                DebtorInn = parts.Count > 5 ? parts[5].Trim() : string.Empty,
                CreditorAccount = parts.Count > 6 ? parts[6].Trim() : string.Empty,
                CreditorInn = parts.Count > 7 ? parts[7].Trim() : string.Empty,
                CreditorName = parts.Count > 8 ? parts[8].Trim() : string.Empty,
                PaymentPurpose = parts.Count > 9 ? parts[9].Trim() : string.Empty,
                ContractNumber = parts.Count > 10 ? parts[10].Trim() : string.Empty,
                OrderReference = parts.Count > 11 ? parts[11].Trim() : string.Empty
            };

            payments.Add(payment);
        }

        return payments;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var currentToken = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    currentToken.Append('\"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ';' || (c == ',' && !inQuotes))
            {
                result.Add(currentToken.ToString());
                currentToken.Clear();
            }
            else
            {
                currentToken.Append(c);
            }
        }

        result.Add(currentToken.ToString());
        return result;
    }
}
