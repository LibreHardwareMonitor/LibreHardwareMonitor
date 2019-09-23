// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringHelper.cs" company="OxyPlot">
//   The MIT License (MIT)
//
//   Copyright (c) 2012 Oystein Bjorke
//
//   Permission is hereby granted, free of charge, to any person obtaining a
//   copy of this software and associated documentation files (the
//   "Software"), to deal in the Software without restriction, including
//   without limitation the rights to use, copy, modify, merge, publish,
//   distribute, sublicense, and/or sell copies of the Software, and to
//   permit persons to whom the Software is furnished to do so, subject to
//   the following conditions:
//
//   The above copyright notice and this permission notice shall be included
//   in all copies or substantial portions of the Software.
//
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//   OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//   MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//   IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//   CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//   TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//   SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary>
//   Provides support for string formatting.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.Collections;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides extended string formatting functionality.
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// The formatting expression.
        /// </summary>
        private static readonly Regex FormattingExpression = new Regex("{(?<Property>.+?)(?<Format>\\:.*?)?}");

        /// <summary>
        /// Replaces the format items in the specified string.
        /// </summary>
        /// <param name="provider">
        /// The culture specific format provider.
        /// </param>
        /// <param name="formatString">
        /// The format string.
        /// </param>
        /// <param name="item">
        /// The item.
        /// </param>
        /// <param name="values">
        /// The values.
        /// </param>
        /// <remarks>
        /// The formatString and values works as in string.Format. In addition, you can format properties of the item object by using the syntax {PropertyName:Formatstring}. E.g. if you have a "Value" property in your item's class, use "{Value:0.00}" to output the value with two digits. Note that this formatting is using reflection and does not have the same performance as string.Format.
        /// </remarks>
        /// <returns>
        /// The formatted string.
        /// </returns>
        public static string Format(IFormatProvider provider, string formatString, object item, params object[] values)
        {
            // Replace items on the format {Property[:Formatstring]}
            var s = FormattingExpression.Replace(
                formatString,
                delegate(Match match)
                    {
                        var property = match.Groups["Property"].Value;
                        if (property.Length > 0 && char.IsDigit(property[0]))
                        {
                            return match.Value;
                        }

                        var pi = item.GetType().GetProperty(property);
                        if (pi == null)
                        {
                            return string.Empty;
                        }

                        var v = pi.GetValue(item, null);
                        var format = match.Groups["Format"].Value;

                        var fs = "{0" + format + "}";
                        return string.Format(provider, fs, v);
                    });

            // Also apply the standard formatting
            s = string.Format(provider, s, values);
            return s;
        }

        /// <summary>
        /// Creates a valid file name.
        /// </summary>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="extension">
        /// The extension.
        /// </param>
        /// <returns>
        /// A file name.
        /// </returns>
        public static string CreateValidFileName(string title, string extension)
        {
            string validFileName = title.Trim();
            var invalidFileNameChars = "/?<>\\:*|\0\t\r\n".ToCharArray();
            foreach (char invalChar in invalidFileNameChars)
            {
                validFileName = validFileName.Replace(invalChar.ToString(), string.Empty);
            }

            foreach (char invalChar in invalidFileNameChars)
            {
                validFileName = validFileName.Replace(invalChar.ToString(), string.Empty);
            }

            if (validFileName.Length > 160)
            {
                // safe value threshold is 260
                validFileName = validFileName.Remove(156) + "...";
            }

            return validFileName + extension;
        }

        /// <summary>
        /// Creates a string from a collection of items.
        /// </summary>
        /// <param name="provider">
        /// The provider.
        /// </param>
        /// <param name="items">
        /// The items.
        /// </param>
        /// <param name="formatstring">
        /// The format string to apply to each item.
        /// </param>
        /// <param name="separator">
        /// The separator.
        /// </param>
        /// <returns>
        /// The collection as a string.
        /// </returns>
        public static object CreateList(
            IFormatProvider provider, IEnumerable items, string formatstring, string separator = ", ")
        {
            var sb = new StringBuilder();
            foreach (var item in items)
            {
                if (sb.Length > 0)
                {
                    sb.Append(separator);
                }

                sb.Append(string.Format(provider, formatstring, item));
            }

            return sb.ToString();
        }
    }
}