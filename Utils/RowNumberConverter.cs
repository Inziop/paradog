using System;
using System.Globalization;
using System.Windows.Data;

namespace ParadoxTranslator.Utils;

/// <summary>
/// Converter to convert AlternationIndex to 1-based row number
/// </summary>
public class AlternationIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return (index + 1).ToString(); // Convert to 1-based index
        }
        return "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
