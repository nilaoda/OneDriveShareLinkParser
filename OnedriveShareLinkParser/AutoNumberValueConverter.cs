﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace OnedriveShareLinkParser
{
    //https://www.cnblogs.com/hoze/p/5103037.html
    class AutoNumberValueConverter : IMultiValueConverter
    {
        #region IMultiValueConverter 成员

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var item = values[0];
            var items = values[1] as ItemCollection;

            var index = items.IndexOf(item);
            return (index + 1).ToString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

        #endregion
    }
}
