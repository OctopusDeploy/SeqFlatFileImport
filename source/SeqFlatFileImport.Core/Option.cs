using System;
using System.Collections.Generic;

namespace SeqFlatFileImport.Core
{
    public class Option<T>
    {
        private readonly T _value;
        public static readonly Option<T> ToNone = new(false, default);

        public static Option<T> ToSome(T value) => new(true, value);

        private Option(bool some, T value)
        {
            _value = value;
            Some = some;
        }

        public bool Some { get; }

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
        private static Option<T> Some<T>(this T value) => Option<T>.ToSome(value);


        public static Option<TSource> FirstOrNone<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            foreach (var item in source)
            {
                if (predicate(item))
                    return item.Some();
            }
            return Option<TSource>.ToNone;
        } 
    }
}