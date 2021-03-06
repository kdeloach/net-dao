﻿// Copyright (c) 2004-2010 Azavea, Inc.
// 
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

namespace Azavea.Open.DAO.Criteria.Joins
{
    /// <summary>
    /// Base class for joins that use one field from a single DAO and compares on a value.
    /// </summary>
    public abstract class AbstractPropertyValueJoinExpression : AbstractJoinExpression
    {
        /// <summary>
        /// The name of the property on the object returned by the DAO that
        /// we are comparing.
        /// </summary>
        public readonly string Property;

        /// <summary>
        /// The value that the given property is compared to.
        /// </summary>
        public readonly object Value;

        /// <summary>
        /// Base class for joins  that use one field from a single DAO and compares on a value.
        /// </summary>
        /// <param name="property">The name of the property on the object returned by the
        ///                            DAO that we are comparing.</param>
        /// <param name="value">The value that the given property is compared to.</param>
        /// <param name="trueOrNot">True means look for matches (I.E. ==),
        ///                         false means look for non-matches (I.E. !=)</param>
        protected AbstractPropertyValueJoinExpression(string property,
            object value, bool trueOrNot)
            : base(trueOrNot)
        {
            Property = property;
            Value = value;
        }
    }
}