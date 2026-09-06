using System;
using System.Globalization;
using Avalonia.Data.Converters;
using FluentAvalonia.UI.Controls;

namespace FluentSend;

public class BoolToPaneModeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool useTop = value is bool b && b;
        return useTop
            ? FANavigationViewPaneDisplayMode.Top
            : FANavigationViewPaneDisplayMode.Left;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
