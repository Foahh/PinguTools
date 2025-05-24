using System.Globalization;
using System.Windows.Data;
using System.Windows.Forms;

namespace PinguTools.Converters;

public class BoolHandCursorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value as bool? == true ? Cursors.Hand : Cursors.Arrow;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}