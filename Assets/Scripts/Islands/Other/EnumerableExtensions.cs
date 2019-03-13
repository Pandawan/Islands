using System.Collections.Generic;
using System.Text;

namespace Pandawan.Islands.Other
{
    public static class EnumerableExtensions
    {
        /// <summary>
        ///     Flattens the given IEnumerable into a string (using ToString()).
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="source">IEnumerable to flatten.</param>
        /// <param name="sequenceSeparator">The separator to use between the different values.</param>
        public static string ToStringFlattened<T>(this IEnumerable<T> source, string sequenceSeparator = ", ")
        {
            StringBuilder sb = new StringBuilder();

            foreach (T x in source)
            {
                sb.Append(x);
                sb.Append(sequenceSeparator);
            }

            if (sb.Length == 0) return "";

            return sb.ToString(0, sb.Length - sequenceSeparator.Length);
        }
    }
}