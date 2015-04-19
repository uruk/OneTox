﻿using System;
using Windows.UI.Xaml.Data;

namespace WinTox.Converters
{
    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var dateTime = (DateTime) value;
            return dateTime.Hour + ":" + dateTime.Minute;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}