// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReflectionHelper.cs" company="OxyPlot">
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
//   Provides reflection based support methods.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;

    /// <summary>
    /// Provides utility methods reflection based support methods.
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        /// Fills a list by the specified property of a source list/enumerable.
        /// </summary>
        /// <param name="source">
        /// The source list.
        /// </param>
        /// <param name="propertyName">
        /// The property name.
        /// </param>
        /// <param name="list">
        /// The list to be filled.
        /// </param>
        /// <typeparam name="T">
        /// The type of the destination list items (and the source property).
        /// </typeparam>
        public static void FillList<T>(IEnumerable source, string propertyName, IList<T> list)
        {
            PropertyInfo pi = null;
            Type t = null;
            foreach (var o in source)
            {
                if (pi == null || o.GetType() != t)
                {
                    t = o.GetType();
                    pi = t.GetProperty(propertyName);
                    if (pi == null)
                    {
                        throw new InvalidOperationException(
                            string.Format("Could not find field {0} on type {1}", propertyName, t));
                    }
                }

                var v = pi.GetValue(o, null);
                var value = (T)Convert.ChangeType(v, typeof(T), CultureInfo.InvariantCulture);
                list.Add(value);
            }
        }
    }
}