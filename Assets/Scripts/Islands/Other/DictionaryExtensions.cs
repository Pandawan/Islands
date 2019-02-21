using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pandawan.Islands.Other
{
    public static class DictionaryExtensions
    {
        /// <summary>
        ///     Remove all the items in the dictionary.
        /// </summary>
        /// <typeparam name="TKey">TKey</typeparam>
        /// <typeparam name="TValue">TValue</typeparam>
        /// <param name="dictionary">Dictionary to remove from.</param>
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

        /// <summary>
        /// Flattens the given Dictionary into a string (using ToString()).
        /// </summary>
        /// <typeparam name="TKey">TKey</typeparam>
        /// <typeparam name="TValue">TValue</typeparam>
        /// <param name="source">Dictionary to flatten.</param>
        /// <param name="keyValueSeparator">The separator to use between the key and value.</param>
        /// <param name="sequenceSeparator">The separator to use between the different pairs.</param>
        /// <returns></returns>
        public static string ToStringFlattened<TKey, TValue>(this Dictionary<TKey, TValue> source,
            string keyValueSeparator = "=", string sequenceSeparator = "|")
        {
            StringBuilder sb = new StringBuilder();

            foreach (KeyValuePair<TKey, TValue> x in source)
            {
                sb.Append(x.Key);
                sb.Append(keyValueSeparator);
                sb.Append(x.Value);
                sb.Append(sequenceSeparator);
            }
            
            return sb.ToString(0, sb.Length - 1);
        }
    }
}