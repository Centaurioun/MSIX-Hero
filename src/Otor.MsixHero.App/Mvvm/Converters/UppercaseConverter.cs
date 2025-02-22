﻿// MSIX Hero
// Copyright (C) 2024 Marcin Otorowski
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// Full notice:
// https://github.com/marcinotorowski/msix-hero/blob/develop/LICENSE.md

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Otor.MsixHero.App.Mvvm.Converters
{
    public class UppercaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            if (value is string valString)
            {
                return valString.ToUpper();
            }

            if (value == null || value == DependencyProperty.UnsetValue)
            {
                return value;
            }

            return value.ToString()?.ToUpper();
        }

        public object ConvertBack(object value, Type targetType, object param, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}