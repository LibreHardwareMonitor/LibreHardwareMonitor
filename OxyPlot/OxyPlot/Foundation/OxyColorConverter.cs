// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OxyColorConverter.cs" company="OxyPlot">
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
//   Converts colors from one data type to another. Access this class through the TypeDescriptor.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.ComponentModel;
    using System.Globalization;

    /// <summary>
    /// Converts between <see cref="OxyColor"/> and <see cref="System.String"/>. Access this class through the TypeDescriptor.
    /// </summary>
    public class OxyColorConverter : TypeConverter
    {
        /// <summary>
        /// Determines whether an object can be converted from a given type to an instance of a <see cref="OxyColor"/>.
        /// </summary>
        /// <param name="context">
        /// Describes the context information of a type.
        /// </param>
        /// <param name="sourceType">
        /// The type of the source that is being evaluated for conversion.
        /// </param>
        /// <returns>
        /// True if the type can be converted to a <see cref="OxyColor"/>; otherwise, false.
        /// </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Determines whether an instance of a <see cref="OxyColor"/> can be converted to a different type.
        /// </summary>
        /// <param name="context">
        /// Describes the context information of a type.
        /// </param>
        /// <param name="destinationType">
        /// The desired type this <see cref="OxyColor"/> is being evaluated for conversion.
        /// </param>
        /// <returns>
        /// True if this <see cref="OxyColor"/> can be converted to destinationType; otherwise, false.
        /// </returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return (destinationType == typeof(string)) || base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Attempts to convert the specified object to a <see cref="OxyColor"/>.
        /// </summary>
        /// <param name="context">
        /// Describes the context information of a type.
        /// </param>
        /// <param name="culture">
        /// Cultural information to respect during conversion.
        /// </param>
        /// <param name="value">
        /// The object being converted.
        /// </param>
        /// <returns>
        /// The <see cref="OxyColor"/> created from converting value.
        /// </returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var str = value as string;
            if (str == null)
            {
                return base.ConvertFrom(context, culture, value);
            }

            return OxyColor.Parse(str);
        }

        /// <summary>
        /// Attempts to convert a <see cref="OxyColor"/> to a specified type.
        /// </summary>
        /// <param name="context">
        /// Describes the context information of a type.
        /// </param>
        /// <param name="culture">
        /// Describes the <see cref="CultureInfo"/> of the type being converted.
        /// </param>
        /// <param name="value">
        /// The <see cref="OxyColor"/> to convert.
        /// </param>
        /// <param name="destinationType">
        /// The type to convert this <see cref="OxyColor"/> to.
        /// </param>
        /// <returns>
        /// The object created from converting this <see cref="OxyColor"/>.
        /// </returns>
        public override object ConvertTo(
            ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            if (destinationType == typeof(string))
            {
                return value != null ? value.ToString() : null;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}