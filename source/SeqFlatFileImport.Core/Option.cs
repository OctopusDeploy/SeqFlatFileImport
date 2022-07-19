﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace SeqFlatFileImport.Core
{
    public class Option<T>
    {
        private readonly T _value;
        public static readonly Option<T> ToNone = new Option<T>(false, default(T));

        public static Option<T> ToSome(T value) => new Option<T>(true, value);

        private Option(bool some, T value)
        {
            _value = value;
            Some = some;
        }

        public bool Some { get; }
        public bool None => !Some;

        public T Value
        {
            get
            {
                if(!Some)
                    throw new InvalidOperationException("This option doesn't have a value, check HasValue before calling Value");
                return _value;
            }
        }
    }

    public static class OptionExtensions
    {
        public static Option<T> Some<T>(this T value) => Option<T>.ToSome(value);
        public static Option<T> None<T>(this T value) => Option<T>.ToNone;	

             
        public static Option<TSource> FirstOrNone<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (predicate == null)
                throw new ArgumentNullException("predicate");

            foreach (TSource item in source)
            {
                if (predicate(item))
                    return item.Some();
            }
            return Option<TSource>.ToNone;
        } 
    }
}