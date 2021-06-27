/*
 * Web2Mail Version 1.1
 * 
 * Copyright 2014 kemunpus@hotmail.com.
 * 
 * Project Page:
 * https://kemunpus.visualstudio.com/DefaultCollection/KemunpusProject
 * 
 * See also:
 * http://kemunpus.azurewebsites.net/
 */
using System;
using System.Diagnostics;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Kemunpus.Web2Mail.Common {

    public sealed class ValueConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, string language) {

            if (value != null) {
                Encoding encoding = value as Encoding;

                if (encoding != null) {
                    return encoding.WebName;
                }

                Debug.WriteLine("ValueConverter : can not convert [{0}] as encoding object.", value.ToString());
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {

            if (value != null) {

                try {
                    return Encoding.GetEncoding(value.ToString());

                } catch (ArgumentException e) {
                    Debug.WriteLine("ValueConverter : exception [{0}] has occurred when convert-back encoding object from value [{1}].", e.ToString(), value.ToString());
                }
            }

            return DependencyProperty.UnsetValue;
        }
    }
}
