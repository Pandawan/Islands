using System;
using System.Collections.Generic;
using System.Linq;

namespace Pandawan.Islands.Other
{
    public static class DictionaryExtensions
    {
        /// <summary>
        ///     Remove all the items in the dictionary.
        /// </summary>
        /// <typeparam name="TKey">TKey</typeparam>
        /// <typeparam name="TValue">TValue</typeparam>
        /// <param name="dictionary">Dictionary to remove from</param>
        /// <param name="predicate">The predicate/condition to select elements to delete.</param>
        /// <returns>Whether or not it succeeded in removing all of the elements.</returns>
        public static bool RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
            Func<TKey, TValue, bool> predicate)
        {
            bool result = true;

            List<TKey> keys = dictionary.Keys.Where(k => predicate(k, dictionary[k])).ToList();
            foreach (TKey key in keys)
                // Keep track of success/fails 
                if (!dictionary.Remove(key))
                    result = false;

            return result;
        }
    }
}