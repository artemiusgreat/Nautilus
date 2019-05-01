﻿//--------------------------------------------------------------------------------------------------
// <copyright file="Precondition.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Core.Correctness
{
    using System;
    using System.Collections.Generic;
    using Nautilus.Core.Annotations;

    /// <summary>
    /// Provides various precondition checking methods which are executed in release configuration.
    /// If the check passes then the method does nothing. If the check fails a type of
    /// <see cref="ArgumentException"/> is thrown with a message.
    /// </summary>
    [Immutable]
    public static class Precondition
    {
        /// <summary>
        /// The check passes if the predicate is true.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <param name="description">The predicate description.</param>
        /// <exception cref="ArgumentException">If the predicate is false.</exception>
        public static void True(bool predicate, string description)
        {
            if (!predicate)
            {
                throw new ArgumentException(FailedMsg.WasFalse(description));
            }
        }

        /// <summary>
        /// The check passes if the condition is false, or both the condition and predicate are
        /// true.
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentException">If the condition is true and the predicate is false.</exception>
        public static void TrueIf(bool condition, bool predicate, string paramName)
        {
            if (condition && !predicate)
            {
                throw new ArgumentException(FailedMsg.WasFalse(paramName));
            }
        }

        /// <summary>
        /// The check passes if the argument is not null.
        /// </summary>
        /// <param name="argument">The argument.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <typeparam name="T">The arguments type.</typeparam>
        /// <exception cref="ArgumentNullException">If the argument is null.</exception>
        public static void NotNull<T>(T argument, string paramName)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(paramName, FailedMsg.WasNull(paramName));
            }
        }

        /// <summary>
        /// The check passes if the <see cref="string"/> argument is not null.
        /// </summary>
        /// <param name="argument">The string argument.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentException">If the string argument is null, empty or white space.</exception>
        public static void NotEmptyOrWhiteSpace(string argument, string paramName)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentNullException(paramName, FailedMsg.WasNullEmptyOrWhitespace(paramName));
            }
        }

        /// <summary>
        /// The check passes if the struct argument is not the default value.
        /// </summary>
        /// <typeparam name="T">The argument type.</typeparam>
        /// <param name="argument">The argument.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentException">If the argument is the default value.</exception>
        public static void NotDefault<T>(T argument, string paramName)
            where T : struct
        {
            if (argument.Equals(default(T)))
            {
                throw new ArgumentException(FailedMsg.WasDefault(argument, paramName));
            }
        }

        /// <summary>
        /// The check passes if the <see cref="ICollection{T}"/> is not null, or empty.
        /// </summary>
        /// <param name="list">The list being checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <typeparam name="T">The type of list.</typeparam>
        /// <exception cref="ArgumentException">If the list is empty.</exception>
        public static void NotEmpty<T>(ICollection<T> list, string paramName)
        {
            if (list.Count == 0)
            {
                throw new ArgumentException(FailedMsg.WasEmptyList(paramName));
            }
        }

        /// <summary>
        /// The check passes if the <see cref="ICollection{T}"/> is not null, or empty.
        /// </summary>
        /// <param name="dictionary">The list being checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <typeparam name="TK">The type of key.</typeparam>
        /// <typeparam name="TV">The type of value.</typeparam>
        /// <exception cref="ArgumentException">If the list is empty.</exception>
        public static void NotEmpty<TK, TV>(Dictionary<TK, TV> dictionary, string paramName)
        {
            if (dictionary.Count == 0)
            {
                throw new ArgumentException(FailedMsg.WasEmptyDictionary(paramName));
            }
        }

        /// <summary>
        /// The check passes if the <see cref="ICollection{T}"/> contains the given element.
        /// </summary>
        /// <typeparam name="T">The type of collection.</typeparam>
        /// <param name="element">The element to contain.</param>
        /// <param name="list">The list being checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="listName">The list name.</param>
        /// <exception cref="ArgumentException">If the list does not contain the element.</exception>
        public static void IsIn<T>(T element, List<T> list, string paramName, string listName)
        {
            if (!list.Contains(element))
            {
                throw new ArgumentException(FailedMsg.WasNotInCollection(element, paramName, listName));
            }
        }

        /// <summary>
        /// The check passes if the <see cref="ICollection{T}"/> contains the given element.
        /// </summary>
        /// <typeparam name="T">The type of collection.</typeparam>
        /// <param name="element">The element to contain.</param>
        /// <param name="list">The list being checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="listName">The list name.</param>
        /// <exception cref="ArgumentException">If the list does not contain the element.</exception>
        public static void IsNotIn<T>(T element, List<T> list, string paramName, string listName)
        {
            if (list.Contains(element))
            {
                throw new ArgumentException(FailedMsg.WasNotInCollection(element, paramName, listName));
            }
        }

        /// <summary>
        /// The check passes if the <see cref="IDictionary{TKey,TValue}"/> contains the given
        /// key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="dictName">The dictionary name.</param>
        /// <typeparam name="T1">The type of the keys.</typeparam>
        /// <typeparam name="T2">The type of the values.</typeparam>
        /// <exception cref="ArgumentException">If the dictionary does not contain the key.</exception>
        public static void KeyIn<T1, T2>(T1 key, Dictionary<T1, T2> dictionary, string paramName, string dictName)
        {
            if (!dictionary.ContainsKey(key))
            {
                throw new ArgumentException(FailedMsg.WasNotInDictionary(key, paramName, dictName));
            }
        }

        /// <summary>
        /// The check passes if the <see cref="IDictionary{TKey,TValue}"/> contains the given
        /// key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="dictName">The dictionary name.</param>
        /// <typeparam name="T1">The type of the keys.</typeparam>
        /// <typeparam name="T2">The type of the values.</typeparam>
        /// <exception cref="ArgumentException">If the dictionary does not contain the key.</exception>
        public static void KeyNotIn<T1, T2>(T1 key, Dictionary<T1, T2> dictionary, string paramName, string dictName)
        {
            if (dictionary.ContainsKey(key))
            {
                throw new ArgumentException(FailedMsg.WasInDictionary(key, paramName, dictName));
            }
        }

        /// <summary>
        /// The check passes if the object is not equal to the other object.
        /// </summary>
        /// <param name="obj">The input object.</param>
        /// <param name="objNotToEqual">The other object not to equal.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentException">If the objects are equal.</exception>
        public static void NotEqualTo(object obj, object objNotToEqual, string paramName)
        {
            if (obj.Equals(objNotToEqual))
            {
                throw new ArgumentException(FailedMsg.WasEqualTo(obj, objNotToEqual, paramName));
            }
        }

        /// <summary>
        /// The check passes if the object is equal to the other object.
        /// </summary>
        /// <param name="obj">The input object.</param>
        /// <param name="objToEqual">The other object to be equal to.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentException">If the objects are not equal.</exception>
        public static void EqualTo(object obj, object objToEqual, string paramName)
        {
            if (!obj.Equals(objToEqual))
            {
                throw new ArgumentException(FailedMsg.WasNotEqualTo(obj, objToEqual, paramName));
            }
        }

        /// <summary>
        /// The check passes if the value is greater than zero.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentOutOfRangeException">If the argument is not positive (> 0).</exception>
        public static void PositiveInt32(int value, string paramName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(paramName, FailedMsg.WasNotPositive(value, paramName));
            }
        }

        /// <summary>
        /// The check passes if the value is greater than zero.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentOutOfRangeException">If the argument is not positive (> 0).</exception>
        public static void PositiveInt64(
            long value,
            string paramName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(paramName, FailedMsg.WasNotPositive(value, paramName));
            }
        }

        /// <summary>
        /// The check passes if the value is greater than zero.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentOutOfRangeException">If the argument is not positive (> 0).</exception>
        public static void PositiveDouble(double value, string paramName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(paramName, FailedMsg.WasNotPositive(value, paramName));
            }
        }

        /// <summary>
        /// The check passes if the value is greater than zero.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentOutOfRangeException">If the argument is not positive (> 0).</exception>
        public static void PositiveDecimal(decimal value, string paramName)
        {
            if (value <= decimal.Zero)
            {
                throw new ArgumentOutOfRangeException(paramName, FailedMsg.WasNotPositive(value, paramName));
            }
        }

        /// <summary>
        /// The check passes if the value is greater than or equal to zero.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentException">If the argument less than zero.</exception>
        public static void NotNegativeInt32(
            int value,
            string paramName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, FailedMsg.WasNegative(value, paramName));
            }
        }

        /// <summary>
        /// The check passes if the value is greater than or equal to zero.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentException">If the argument is less than zero.</exception>
        public static void NotNegativeInt64(
            long value,
            string paramName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, FailedMsg.WasNegative(value, paramName));
            }
        }

        /// <summary>
        /// The check passes if the value is greater than or equal to zero.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentException">If the argument is less than zero.</exception>
        public static void NotNegativeDouble(
            double value,
            string paramName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, FailedMsg.WasNegative(value, paramName));
            }
        }

        /// <summary>
        /// The check passes if the value is greater than or equal to zero.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentOutOfRangeException">If the argument is less than zero.</exception>
        public static void NotNegativeDecimal(decimal value, string paramName)
        {
            if (value < decimal.Zero)
            {
                throw new ArgumentOutOfRangeException(paramName, FailedMsg.WasNegative(value, paramName));
            }
        }

        /// <summary>
        /// The check passes if the value is not out of the specified range.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="lowerBound">The range lower bound.</param>
        /// <param name="upperBound">The range upper bound.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentException">If the value is out of the specified range.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the range end points is not recognized.</exception>
        public static void NotOutOfRangeInt32(
            int value,
            int lowerBound,
            int upperBound,
            string paramName)
        {
            if (value < lowerBound || value > upperBound)
            {
                throw new ArgumentOutOfRangeException(paramName, FailedMsg.WasOutOfRange(value, lowerBound, upperBound, paramName));
            }
        }

        /// <summary>
        /// The check passes if the value is not out of the specified range.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="lowerBound">The range lower bound.</param>
        /// <param name="upperBound">The range upper bound.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentException">If the value is out of the specified range.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the range end points is not recognized.</exception>
        public static void NotOutOfRangeInt64(
            long value,
            long lowerBound,
            long upperBound,
            string paramName)
        {
            if (value < lowerBound || value > upperBound)
            {
                throw new ArgumentOutOfRangeException(paramName, FailedMsg.WasOutOfRange(value, lowerBound, upperBound, paramName));
            }
        }

        /// <summary>
        /// The check passes if the value is not out of the specified range.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="lowerBound">The range lower bound.</param>
        /// <param name="upperBound">The range upper bound.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentException">If the value is out of the specified range.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the range end points is not recognized.</exception>
        public static void NotOutOfRangeDouble(
            double value,
            double lowerBound,
            double upperBound,
            string paramName)
        {
            if (value < lowerBound || value > upperBound)
            {
                throw new ArgumentOutOfRangeException(paramName, FailedMsg.WasOutOfRange(value, lowerBound, upperBound, paramName));
            }
        }

        /// <summary>
        /// The check passes if the value is not out of the specified range.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="lowerBound">The range lower bound.</param>
        /// <param name="upperBound">The range upper bound.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ArgumentException">If the value is out of the specified range.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the range end points is not recognized.</exception>
        public static void NotOutOfRangeDecimal(
            decimal value,
            decimal lowerBound,
            decimal upperBound,
            string paramName)
        {
            if (value < lowerBound || value > upperBound)
            {
                throw new ArgumentOutOfRangeException(paramName, FailedMsg.WasOutOfRange(value, lowerBound, upperBound, paramName));
            }
        }
    }
}
