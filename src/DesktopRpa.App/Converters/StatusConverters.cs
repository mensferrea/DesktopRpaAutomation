using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DesktopRpa.Core.Models;

namespace DesktopRpa.App.Converters;

public sealed class MatchStatusToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush MatchedBrush = new((Color)ColorConverter.ConvertFromString("#10B981"));
    private static readonly SolidColorBrush AmountMismatchBrush = new((Color)ColorConverter.ConvertFromString("#EF4444"));
    private static readonly SolidColorBrush CounterpartyMismatchBrush = new((Color)ColorConverter.ConvertFromString("#F59E0B"));
    private static readonly SolidColorBrush MissingBrush = new((Color)ColorConverter.ConvertFromString("#6B7280"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MatchStatus status)
        {
            return status switch
            {
                MatchStatus.Matched => MatchedBrush,
                MatchStatus.AmountMismatch => AmountMismatchBrush,
                MatchStatus.CounterpartyMismatch => CounterpartyMismatchBrush,
                MatchStatus.MissingInBankStatement or MatchStatus.MissingInInternalRegistry => MissingBrush,
                _ => Brushes.Gray
            };
        }

        return Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class MatchStatusToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MatchStatus status)
        {
            return status switch
            {
                MatchStatus.Matched => "Совпало",
                MatchStatus.AmountMismatch => "Расхождение суммы",
                MatchStatus.CounterpartyMismatch => "Расхождение контрагента",
                MatchStatus.MissingInBankStatement => "Нет в выписке",
                MatchStatus.MissingInInternalRegistry => "Нет в реестре",
                _ => status.ToString()
            };
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
