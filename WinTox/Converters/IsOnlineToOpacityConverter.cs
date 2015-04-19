﻿using System;
using Windows.UI.Xaml.Data;

namespace WinTox.Converters
{
    public class IsOnlineToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var isOnline = (bool) value;
            if (isOnline)
                return 1;
            return 0.5;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}