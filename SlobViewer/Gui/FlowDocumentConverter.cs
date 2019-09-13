// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SlobViewer.Gui
{
    /// <summary>
    /// Converts a <see cref="HtmlToFlowDocument.Dom.FlowDocument"/> into a Windows <see cref="System.Windows.Documents.FlowDocument"/>.
    /// </summary>
    /// <seealso cref="System.Windows.Data.IValueConverter" />
    [ValueConversion(typeof(HtmlToFlowDocument.Dom.FlowDocument), typeof(System.Windows.Documents.FlowDocument))]
    public class FlowDocumentConverter : IValueConverter, IMultiValueConverter
    {
        HtmlToFlowDocument.Rendering.WpfRenderer _renderer;

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Convert(new object[] { value }, targetType, parameter, culture);
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length > 0 && values[0] is HtmlToFlowDocument.Dom.FlowDocument doc)
            {
                if (null == _renderer)
                    _renderer = new HtmlToFlowDocument.Rendering.WpfRenderer();

                if (values?.Length > 1 && values[1] is bool isInDarkMode)
                    _renderer.InvertColors = isInDarkMode;


                return _renderer.Render(doc);
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
