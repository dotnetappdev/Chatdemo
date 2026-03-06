using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ChatDemo.Wpf.Converters;

/// <summary>bool → Visibility (true = Visible)</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}

/// <summary>bool → Visibility (true = Collapsed, inverse)</summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Collapsed;
}

/// <summary>IsOwn bool → HorizontalAlignment (own = Right, other = Left)</summary>
public class IsOwnToAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? HorizontalAlignment.Right : HorizontalAlignment.Left;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>IsOwn bool → bubble background brush</summary>
public class IsOwnToBubbleBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush OwnBrush = new(Color.FromRgb(0x00, 0x7A, 0xFF));
    private static readonly SolidColorBrush OtherBrush = new(Color.FromRgb(0xE9, 0xE9, 0xEB));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? OwnBrush : OtherBrush;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>IsOwn bool → text foreground (own = white, other = black)</summary>
public class IsOwnToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Brushes.White : Brushes.Black;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>int > 0 → Visible, else Collapsed (unread badge)</summary>
public class UnreadToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is int n && n > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Online bool → green / grey brush</summary>
public class OnlineToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush OnlineBrush = new(Color.FromRgb(0x34, 0xC7, 0x59));
    private static readonly SolidColorBrush OfflineBrush = new(Color.FromRgb(0xAA, 0xAA, 0xAA));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? OnlineBrush : OfflineBrush;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Returns the first character of a string, or "?" if empty.</summary>
public class FirstCharConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is string s && s.Length > 0 ? s[0].ToString() : "?";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>DateTime → "HH:mm" short time string</summary>
public class DateTimeToShortTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DateTime dt ? dt.ToString("HH:mm") : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
