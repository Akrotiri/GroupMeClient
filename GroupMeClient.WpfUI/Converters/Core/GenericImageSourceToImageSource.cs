﻿using System;
using System.Windows.Data;
using System.Windows.Media;
using GroupMeClient.Core.Controls.Media;

namespace GroupMeClient.WpfUI.Converters
{
    /// <summary>
    /// <see cref="GenericImageSourceToImageSource"/> provides a conversion between the abstract GMDC <see cref="GenericImageSource"/> and
    /// WPF's <see cref="ImageSource"/> type.
    /// </summary>
    [ValueConversion(typeof(GenericImageSource), typeof(ImageSource))]
    public class GenericImageSourceToImageSource : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is GenericImageSource genericImageSource)
            {
                if (genericImageSource.RenderHeight > 0 || genericImageSource.RenderWidth > 0)
                {
                    return Utilities.ImageUtils.BytesToImageSource(
                        genericImageSource.RawImageData,
                        genericImageSource.RenderWidth,
                        genericImageSource.RenderHeight);
                }
                else
                {
                    return Utilities.ImageUtils.BytesToImageSource(genericImageSource.RawImageData);
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
