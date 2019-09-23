// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ListFiller.cs" company="OxyPlot">
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
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    ///     Provides functionality to fill a list by specified properties of another list.
    /// </summary>
    /// <remarks>
    ///     This class uses reflection.
    /// </remarks>
    /// <typeparam name="T">
    ///     The target list item type.
    /// </typeparam>
    public class ListFiller<T>
        where T : class, new()
    {
        /// <summary>
        ///     The properties.
        /// </summary>
        private readonly Dictionary<string, Action<T, object>> properties;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ListFiller{T}" /> class.
        /// </summary>
        public ListFiller()
        {
            this.properties = new Dictionary<string, Action<T, object>>();
        }

        /// <summary>
        ///     Adds a setter for the specified property.
        /// </summary>
        /// <param name="propertyName">
        ///     Name of the property.
        /// </param>
        /// <param name="setter">
        ///     The setter.
        /// </param>
        public void Add(string propertyName, Action<T, object> setter)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return;
            }

            this.properties.Add(propertyName, setter);
        }

        /// <summary>
        ///     Fills the specified target list.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="source">The source.</param>
        public void FillT(IList<T> target, IEnumerable source)
        {
            this.Fill((IList)target, source);
        }

        /// <summary>
        ///     Fills the specified target list.
        /// </summary>
        /// <param name="target">
        ///     The target.
        /// </param>
        /// <param name="source">
        ///     The source list.
        /// </param>
        public void Fill(IList target, IEnumerable source)
        {
            PropertyInfo[] pi = null;
            Type t = null;
            foreach (var sourceItem in source)
            {
                if (pi == null || sourceItem.GetType() != t)
                {
                    t = sourceItem.GetType();
                    pi = new PropertyInfo[this.properties.Count];
                    int i = 0;
                    foreach (var p in this.properties)
                    {
                        if (string.IsNullOrEmpty(p.Key))
                        {
                            i++;
                            continue;
                        }

                        pi[i] = t.GetProperty(p.Key);
                        if (pi[i] == null)
                        {
                            throw new InvalidOperationException(
                                string.Format("Could not find field {0} on type {1}", p.Key, t));
                        }

                        i++;
                    }
                }

                var item = new T();

                int j = 0;
                foreach (var p in this.properties)
                {
                    if (pi[j] != null)
                    {
                        var value = pi[j].GetValue(sourceItem, null);
                        p.Value(item, value);
                    }

                    j++;
                }

                target.Add(item);
            }
        }       
    }
}