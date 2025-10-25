using System;
using System.Globalization;
using System.Windows.Data;

namespace ParadoxTranslator.Utils
{
    // Converts Value and Maximum into a fraction (Value/Maximum) for ProgressBar scaling
    public class ProgressFractionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values == null || values.Length < 2) return 0.0;
                double value = System.Convert.ToDouble(values[0]);
                double maximum = System.Convert.ToDouble(values[1]);
                if (maximum <= 0) return 0.0;
                double frac = value / maximum;
                if (double.IsNaN(frac) || double.IsInfinity(frac)) return 0.0;
                return Math.Max(0.0, Math.Min(1.0, frac));
            }
            catch
            {
                return 0.0;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
