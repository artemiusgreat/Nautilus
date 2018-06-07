﻿//--------------------------------------------------------------------------------------------------
// <copyright file="Validate.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Core.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Nautilus.Core.Annotations;

    /// <summary>
    /// Provides validation methods which are executed in both debug and release configurations.
    /// If validation passes the method does nothing. If the validation fails a
    /// <see cref="ValidationException"/> is thrown, which will contain the inner
    /// <see cref="ArgumentException"/> with details including a message and parameter name.
    /// </summary>
    [Immutable]
    public static class Validate
    {
        private const string ExMessage = "Validation Failed";

        /// <summary>
        /// The validation passes if the predicate is true.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ValidationException">Throws if the predicate is false.</exception>
        public static void True(bool predicate, string paramName)
        {
            if (!predicate)
            {
                throw new ValidationException(
                    new ArgumentException(
                        $"{ExMessage} (The predicate based on {paramName} is false).", paramName));
            }
        }

        /// <summary>
        /// The validation passes if the condition is false, or both the condition and predicate are
        /// true.
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ValidationException">Throws if the condition does not pass the predicate.</exception>
        public static void TrueIf(bool condition, bool predicate, string paramName)
        {
            if (condition && !predicate)
            {
                throw new ValidationException(
                    new ArgumentException(
                        $"{ExMessage} (The conditional predicate based on {paramName} is false).", paramName));
            }
        }

        /// <summary>
        /// The validation passes if the argument is not null.
        /// </summary>
        /// <param name="argument">The argument.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <typeparam name="T">The arguments type.</typeparam>
        /// <exception cref="ValidationException">Throws if the argument is null.</exception>
        public static void NotNull<T>(T argument, string paramName)
        {
            if (argument == null)
            {
                throw new ValidationException(
                    new ArgumentNullException(
                        paramName, $"{ExMessage} (The {paramName} argument is null)."));
            }
        }

        /// <summary>
        /// The validation passes if the <see cref="string"/> argument is not null.
        /// </summary>
        /// <param name="argument">The string argument.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ValidationException">Throws if the string argument is null, empty or white space.</exception>
        public static void NotNull(string argument, string paramName)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ValidationException(
                    new ArgumentNullException(
                        paramName, $"{ExMessage} (The {paramName} string argument is null or white space)."));
            }
        }

        /// <summary>
        /// The validation passes if the struct argument is not the default value.
        /// </summary>
        /// <typeparam name="T">The argument type.</typeparam>
        /// <param name="argument">The argument.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ValidationException">Throws if the argument is the default value.</exception>
        public static void NotDefault<T>(T argument, string paramName)
            where T : struct
        {
            if (argument.Equals(default(T)))
            {
                throw new ValidationException(
                    new ArgumentException(
                        paramName, $"{ExMessage} (The {paramName} is the default value)."));
            }
        }

        /// <summary>
        /// The validation passes if the <see cref="ICollection{T}"/> is not null, or empty.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <typeparam name="T">The type of collection.</typeparam>
        /// <exception cref="ValidationException">Throws if the collection is null or empty.</exception>
        public static void CollectionNotNullOrEmpty<T>(ICollection<T> collection, string paramName)
        {
            if (collection == null)
            {
                throw new ValidationException(
                    new ArgumentNullException(
                        paramName, $"{ExMessage} (The {paramName} collection is null)."));
            }

            if (collection.Count == 0)
            {
                throw new ValidationException(
                    new ArgumentException(
                        $"{ExMessage} (The {paramName} collection is empty).", paramName));
            }
        }

        /// <summary>
        /// The validation passes if the <see cref="IReadOnlyCollection{T}"/> is not null, or empty.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <typeparam name="T">The type of collection.</typeparam>
        /// <exception cref="ValidationException">Throws if the collection is null or empty.</exception>
        public static void ReadOnlyCollectionNotNullOrEmpty<T>(IReadOnlyCollection<T> collection, string paramName)
        {
            if (collection == null)
            {
                throw new ValidationException(
                    new ArgumentNullException(
                        paramName, $"{ExMessage} (The {paramName} collection is null)."));
            }

            if (collection.Count == 0)
            {
                throw new ValidationException(
                    new ArgumentException(
                        $"{ExMessage} (The {paramName} collection is empty).", paramName));
            }
        }

        /// <summary>
        /// The validation passes if the <see cref="ICollection{T}"/> is not null, and is empty
        /// (count zero).
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <typeparam name="T">The type of collection.</typeparam>
        /// <exception cref="ValidationException">Throws if the collection is not empty.</exception>
        public static void CollectionEmpty<T>(ICollection<T> collection, string paramName)
        {
            if (collection == null)
            {
                throw new ValidationException(
                    new ArgumentNullException(
                        paramName, $"{ExMessage} (The {paramName} collection is null)."));
            }

            if (collection.Count != 0)
            {
                throw new ValidationException(
                    new ArgumentException(
                        $"{ExMessage} (The {paramName} collection is not empty).", paramName));
            }
        }

        /// <summary>
        /// The validation passes if the <see cref="IReadOnlyCollection{T}"/> is not null, and is
        /// empty (count zero).
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <typeparam name="T">The type of collection.</typeparam>
        /// <exception cref="ValidationException">Throws if the collection is not empty.</exception>
        public static void ReadOnlyCollectionEmpty<T>(IReadOnlyCollection<T> collection, string paramName)
        {
            if (collection == null)
            {
                throw new ValidationException(
                    new ArgumentNullException(
                        paramName, $"{ExMessage} (The {paramName} collection is null)."));
            }

            if (collection.Count != 0)
            {
                throw new ValidationException(
                    new ArgumentException(
                        $"{ExMessage} (The {paramName} collection is not empty).", paramName));
            }
        }

        /// <summary>
        /// The validation passes if the <see cref="ICollection{T}"/> contains the given element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="collection">The collection.</param>
        /// <typeparam name="T">The type of collection.</typeparam>
        /// <exception cref="ValidationException">Throws if the collection does not contain the element.</exception>
        public static void CollectionContains<T>(T element, string paramName, ICollection<T> collection)
        {
            if (!collection.Contains(element))
            {
                throw new ValidationException(
                    new ArgumentException(
                        $"{ExMessage} (The collection does not contain the {paramName} element).", paramName));
            }
        }

        /// <summary>
        /// The validation passes if the <see cref="IReadOnlyCollection{T}"/> contains the
        /// given element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="collection">The collection.</param>
        /// <typeparam name="T">The type of collection.</typeparam>
        /// <exception cref="ValidationException">Throws if the collection does not contain the element.</exception>
        public static void ReadOnlyCollectionContains<T>(T element, string paramName, IReadOnlyCollection<T> collection)
        {
            if (!collection.Contains(element))
            {
                throw new ValidationException(
                    new ArgumentException(
                        $"{ExMessage} (The collection does not contain the {paramName} element).", paramName));
            }
        }

        /// <summary>
        /// The validation passes if the <see cref="ICollection{T}"/> does not contain the
        /// given element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="collection">The collection.</param>
        /// <typeparam name="T">The type of collection.</typeparam>
        /// <exception cref="ValidationException">Throws if the collection contains the element.</exception>
        public static void CollectionDoesNotContain<T>(T element, string paramName, ICollection<T> collection)
        {
            if (collection.Contains(element))
            {
                throw new ValidationException(
                    new ArgumentException(
                        $"{ExMessage} (The collection already contains the {paramName} element).", paramName));
            }
        }

        /// <summary>
        /// The validation passes if the <see cref="IReadOnlyCollection{T}"/> does not contain the
        /// given element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="collection">The collection.</param>
        /// <typeparam name="T">The type of collection.</typeparam>
        /// <exception cref="ValidationException">Throws if the collection contains the element.</exception>
        public static void ReadOnlyCollectionDoesNotContain<T>(T element, string paramName, IReadOnlyCollection<T> collection)
        {
            if (collection.Contains(element))
            {
                throw new ValidationException(
                    new ArgumentException(
                        $"{ExMessage} (The collection already contains the {paramName} element).", paramName));
            }
        }

        /// <summary>
        /// The validation passes if the <see cref="IDictionary{TKey,TValue}"/> contains the given
        /// key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="dictionary">The dictionary.</param>
        /// <typeparam name="T1">The type of the keys.</typeparam>
        /// <typeparam name="T2">The type of the values.</typeparam>
        /// <exception cref="ValidationException">Throws if the dictionary does not contain the key.</exception>
        public static void DictionaryContainsKey<T1, T2>(T1 key, string paramName, IDictionary<T1, T2> dictionary)
        {
            if (!dictionary.ContainsKey(key))
            {
                throw new ValidationException(
                    new ArgumentException(
                        $"{ExMessage} (The collection does not contain the {paramName} key).", paramName));
            }
        }

        /// <summary>
        /// The validation passes if the <see cref="IReadOnlyDictionary{TKey,TValue}"/> contains the
        /// given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="dictionary">The dictionary.</param>
        /// <typeparam name="T1">The type of the keys.</typeparam>
        /// <typeparam name="T2">The type of the values.</typeparam>
        /// <exception cref="ValidationException">Throws if the dictionary does not contain the key.</exception>
        public static void ReadOnlyDictionaryContainsKey<T1, T2>(T1 key, string paramName, IReadOnlyDictionary<T1, T2> dictionary)
        {
            if (!dictionary.ContainsKey(key))
            {
                throw new ValidationException(
                    new ArgumentException(
                        $"{ExMessage} (The collection does not contain the {paramName} key).", paramName));
            }
        }

        /// <summary>
        /// The validation passes if the <see cref="IDictionary{TKey,TValue}"/> does not contain the
        /// given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="dictionary">The dictionary.</param>
        /// <typeparam name="T1">The type of the keys.</typeparam>
        /// <typeparam name="T2">The type of the values.</typeparam>
        /// <exception cref="ValidationException">Throws if the dictionary contains the key.</exception>
        public static void DictionaryDoesNotContainKey<T1, T2>(T1 key, string paramName, IDictionary<T1, T2> dictionary)
        {
            if (dictionary.ContainsKey(key))
            {
                throw new ValidationException(
                    new ArgumentException(
                        $"{ExMessage} (The collection already contains the {paramName} key).", paramName));
            }
        }

        /// <summary>
        /// The validation passes if the <see cref="IReadOnlyDictionary{TKey,TValue}"/> does not
        /// contain the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="dictionary">The dictionary.</param>
        /// <typeparam name="T1">The type of the keys.</typeparam>
        /// <typeparam name="T2">The type of the values.</typeparam>
        /// <exception cref="ValidationException">Throws if the dictionary contains the key.</exception>
        public static void ReadOnlyDictionaryDoesNotContainKey<T1, T2>(T1 key, string paramName, IReadOnlyDictionary<T1, T2> dictionary)
        {
            if (dictionary.ContainsKey(key))
            {
                throw new ValidationException(
                    new ArgumentException(
                        $"{ExMessage} (The dictionary already contains the {paramName} key).", paramName));
            }
        }

        /// <summary>
        /// The validation passes if the object is not equal to the other object.
        /// </summary>
        /// <param name="obj">The input object.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="objNotToEqual">The other object not to equal.</param>
        /// <exception cref="ValidationException">Throws if the objects are equal.</exception>
        public static void NotEqualTo(object obj, string paramName, object objNotToEqual)
        {
            if (obj.Equals(objNotToEqual))
            {
                throw new ValidationException(
                    new ArgumentException(
                        $"{ExMessage} (The {paramName} should not be equal to {objNotToEqual}. Value = {obj}).", paramName));
            }
        }

        /// <summary>
        /// The validation passes if the object is equal to the other object.
        /// </summary>
        /// <param name="obj">The input object.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="objToEqual">The other object to be equal to.</param>
        /// <exception cref="ValidationException">Throws if the objects are not equal.</exception>
        public static void EqualTo(object obj, string paramName, object objToEqual)
        {
            if (!obj.Equals(objToEqual))
            {
                throw new ValidationException(
                    new ArgumentException(
                        $"{ExMessage} (The {paramName} should be equal to {objToEqual}. Value = {obj}).", paramName));
            }
        }

        /// <summary>
        /// The validation passes if the value is not out of the specified range.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="lowerBound">The range lower bound.</param>
        /// <param name="upperBound">The range upper bound.</param>
        /// <param name="endPoints">The range end points literal.</param>
        /// <exception cref="ValidationException">Throws if the value is out of the specified range.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Throws if the range end points is not recognized.</exception>
        public static void Int32NotOutOfRange(
            int value,
            string paramName,
            int lowerBound,
            int upperBound,
            RangeEndPoints endPoints = RangeEndPoints.Inclusive)
        {
            switch (endPoints)
            {
                case RangeEndPoints.Inclusive:
                    if (value < lowerBound || value > upperBound)
                    {
                        throw new ValidationException(
                            new ArgumentOutOfRangeException(
                            paramName,
                            $"{ExMessage} (The {paramName} is not within the specified range [{lowerBound}, {upperBound}]. Value = {value})."));
                    }

                    break;

                case RangeEndPoints.LowerExclusive:
                    if (value <= lowerBound || value > upperBound)
                    {
                        throw new ValidationException(
                            new ArgumentOutOfRangeException(
                            paramName,
                            $"{ExMessage} (The {paramName} is not within the specified range ({lowerBound}, {upperBound}]. Value = {value})."));
                    }

                    break;

                case RangeEndPoints.UpperExclusive:
                    if (value < lowerBound || value >= upperBound)
                    {
                        throw new ValidationException(
                            new ArgumentOutOfRangeException(
                            paramName,
                            $"{ExMessage} (The {paramName} is not within the specified range [{lowerBound}, {upperBound}). Value = {value})."));
                    }

                    break;

                case RangeEndPoints.Exclusive:
                    if (value <= lowerBound || value >= upperBound)
                    {
                        throw new ValidationException(
                            new ArgumentOutOfRangeException(
                            paramName,
                            $"{ExMessage} (The {paramName} is not within the specified range ({lowerBound}, {upperBound}). Value = {value})."));
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(endPoints), endPoints, "The range end points is not recognized.");
            }
        }

        /// <summary>
        /// The validation passes if the value is not out of the specified range.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="lowerBound">The range lower bound.</param>
        /// <param name="upperBound">The range upper bound.</param>
        /// <param name="endPoints">The range end points literal.</param>
        /// <exception cref="ValidationException">Throws if the value is out of the specified range.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Throws if the range end points is not recognized.</exception>
        public static void LongNotOutOfRange(
            long value,
            string paramName,
            long lowerBound,
            long upperBound,
            RangeEndPoints endPoints = RangeEndPoints.Inclusive)
        {
            switch (endPoints)
            {
                case RangeEndPoints.Inclusive:
                    if (value < lowerBound || value > upperBound)
                    {
                        throw new ValidationException(
                            new ArgumentOutOfRangeException(
                            paramName,
                                $"{ExMessage} (The {paramName} is not within the specified range [{lowerBound}, {upperBound}]. Value = {value})."));
                    }

                    break;

                case RangeEndPoints.LowerExclusive:
                    if (value <= lowerBound || value > upperBound)
                    {
                        throw new ValidationException(
                            new ArgumentOutOfRangeException(
                            paramName,
                                $"{ExMessage} (The {paramName} is not within the specified range ({lowerBound}, {upperBound}]. Value = {value})."));
                    }

                    break;

                case RangeEndPoints.UpperExclusive:
                    if (value < lowerBound || value >= upperBound)
                    {
                        throw new ValidationException(
                            new ArgumentOutOfRangeException(
                            paramName,
                                $"{ExMessage} (The {paramName} is not within the specified range [{lowerBound}, {upperBound}). Value = {value})."));
                    }

                    break;

                case RangeEndPoints.Exclusive:
                    if (value <= lowerBound || value >= upperBound)
                    {
                        throw new ValidationException(
                            new ArgumentOutOfRangeException(
                                paramName,
                                $"{ExMessage} (The {paramName} is not within the specified range ({lowerBound}, {upperBound}). Value = {value})."));
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(endPoints), endPoints, "The range end points is not recognized.");
            }
        }

        /// <summary>
        /// The validation passes if the value is not out of the specified range.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="lowerBound">The range lower bound.</param>
        /// <param name="upperBound">The range upper bound.</param>
        /// <param name="endPoints">The range end points literal.</param>
        /// <exception cref="ValidationException">Throws if the value is out of the specified range.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Throws if the range end points is not recognized.</exception>
        public static void DoubleNotOutOfRange(
            double value,
            string paramName,
            double lowerBound,
            double upperBound,
            RangeEndPoints endPoints = RangeEndPoints.Inclusive)
        {
            if (value.IsInvalidDouble())
            {
                throw new ValidationException(
                    new ArgumentOutOfRangeException(
                        paramName,
                        $"{ExMessage} (The {paramName} value is an invalid number)."));
            }

            switch (endPoints)
            {
                case RangeEndPoints.Inclusive:
                    if (value < lowerBound || value > upperBound)
                    {
                        throw new ValidationException(
                            new ArgumentOutOfRangeException(
                                paramName,
                                $"{ExMessage} (The {paramName} is not within the specified range [{lowerBound}, {upperBound}]. Value = {value})."));
                    }

                    break;

                case RangeEndPoints.LowerExclusive:
                    if (value <= lowerBound || value > upperBound)
                    {
                        throw new ValidationException(
                            new ArgumentOutOfRangeException(
                                paramName,
                                $"{ExMessage} (The {paramName} is not within the specified range ({lowerBound}, {upperBound}]. Value = {value})."));
                    }

                    break;

                case RangeEndPoints.UpperExclusive:
                    if (value < lowerBound || value >= upperBound)
                    {
                        throw new ValidationException(
                            new ArgumentOutOfRangeException(
                                paramName,
                                $"{ExMessage} (The {paramName} is not within the specified range [{lowerBound}, {upperBound}). Value = {value}."));
                    }

                    break;

                case RangeEndPoints.Exclusive:
                    if (value <= lowerBound || value >= upperBound)
                    {
                        throw new ValidationException(
                            new ArgumentOutOfRangeException(
                                paramName,
                                $"{ExMessage} (The {paramName} is not within the specified range ({lowerBound}, {upperBound}). Value = {value})."));
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(endPoints), endPoints, "The range end points is not recognized.");
            }
        }

        /// <summary>
        /// The validation passes if the value is not out of the specified range.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="lowerBound">The range lower bound.</param>
        /// <param name="upperBound">The range upper bound.</param>
        /// <param name="endPoints">The range end points literal.</param>
        /// <exception cref="ValidationException">Throws if the value is out of the specified range.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Throws if the range end points is not recognized.</exception>
        public static void DecimalNotOutOfRange(
            decimal value,
            string paramName,
            decimal lowerBound,
            decimal upperBound,
            RangeEndPoints endPoints = RangeEndPoints.Inclusive)
        {
            switch (endPoints)
            {
                case RangeEndPoints.Inclusive:
                    if (value < lowerBound || value > upperBound)
                    {
                        throw new ValidationException(
                            new ArgumentOutOfRangeException(
                                paramName,
                                $"{ExMessage} (The {paramName} is not within the specified range [{lowerBound}, {upperBound}]. Value = {value})."));
                    }

                    break;

                case RangeEndPoints.LowerExclusive:
                    if (value <= lowerBound || value > upperBound)
                    {
                        throw new ValidationException(
                            new ArgumentOutOfRangeException(
                                paramName,
                                $"{ExMessage} (The {paramName} is not within the specified range ({lowerBound}, {upperBound}]. Value = {value})."));
                    }

                    break;

                case RangeEndPoints.UpperExclusive:
                    if (value < lowerBound || value >= upperBound)
                    {
                        throw new ValidationException(
                            new ArgumentOutOfRangeException(
                                paramName,
                                $"{ExMessage} (The {paramName} is not within the specified range [{lowerBound}, {upperBound}). Value = {value}."));
                    }

                    break;

                case RangeEndPoints.Exclusive:
                    if (value <= lowerBound || value >= upperBound)
                    {
                        throw new ValidationException(
                            new ArgumentOutOfRangeException(
                                paramName,
                                $"{ExMessage} (The {paramName} is not within the specified range ({lowerBound}, {upperBound}). Value = {value})."));
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(endPoints), endPoints, "The range end points is not recognized.");
            }
        }

        /// <summary>
        /// The validation passes if the value is not an invalid number.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <exception cref="ValidationException">Throws if the value is not a valid number.</exception>
        public static void DoubleNotInvalidNumber(double value, string paramName)
        {
            if (value.IsInvalidDouble())
            {
                throw new ValidationException(
                    new ArgumentOutOfRangeException(
                        paramName, $"{ExMessage} (The {paramName} is an invalid number)."));
            }
        }

        /// <summary>
        /// Returns a value indicating whether the given <see cref="double"/> is a valid number.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns>True if the value is in range; otherwise returns false.</returns>
        private static bool IsInvalidDouble(this double value)
        {
            return double.IsNaN(value)
                || double.IsNegativeInfinity(value)
                || double.IsPositiveInfinity(value);
        }
    }
}