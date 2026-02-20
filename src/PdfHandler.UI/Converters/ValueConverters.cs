using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace PdfHandler.UI.Converters;

/// <summary>
/// BoolをVisibilityに変換するコンバーター
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}

/// <summary>
/// Boolを反転してVisibilityに変換するコンバーター
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility != Visibility.Visible;
        }
        return true;
    }
}

/// <summary>
/// Boolを反転するコンバーター
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}

/// <summary>
/// byte[]を画像ソースに変換するコンバーター
/// </summary>
public class ByteArrayToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte[] bytes && bytes.Length > 0)
        {
            var image = new System.Windows.Media.Imaging.BitmapImage();
            using (var mem = new System.IO.MemoryStream(bytes))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Boolを色に変換するコンバーター（試用期間終了間近の警告用）
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
        }
        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// ページ数をVisibilityに変換するコンバーター（複数ページの場合のみ表示）
/// </summary>
public class PageCountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int pageCount && pageCount > 1)
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// BoolをGridLengthに変換するコンバーター（プレビューペインの幅制御用）
/// </summary>
public class PreviewColumnWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // trueの場合は*（残りのスペースを使用）、falseの場合は0（非表示）
            return boolValue ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        }
        return new GridLength(1, GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is GridLength gridLength)
        {
            return gridLength.Value > 0;
        }
        return true;
    }
}
