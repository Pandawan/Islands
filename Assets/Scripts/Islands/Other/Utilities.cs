using System.Text.RegularExpressions;
using UnityEngine;

namespace Pandawan.Islands.Other
{
    public static class Utilities
    {
        public static string RemoveIllegalFileCharacters(string original)
        {
            Regex illegalInFileName = new Regex(@"[\\/:*?""<>|]");
            return illegalInFileName.Replace(original, "");
        }

        /// <summary>
        ///     Whether or not the given Vector3 object is empty
        /// </summary>
        /// <param name="obj">Vector3 Object</param>
        /// <returns>Is empty</returns>
        public static bool IsEmpty(Vector3 obj)
        {
            return obj.x == 0 && obj.y == 0 && obj.z == 0;
        }

        /// <summary>
        ///     Whether or not the given Vector3 object is empty
        /// </summary>
        /// <param name="obj">Vector3 Object</param>
        /// <returns>Is empty</returns>
        public static bool IsEmpty(Vector3Int obj)
        {
            return obj.x == 0 && obj.y == 0 && obj.z == 0;
        }

        /// <summary>
        ///     Whether or not the given Vector2 object is empty
        /// </summary>
        /// <param name="obj">Vector2 Object</param>
        /// <returns>Is empty</returns>
        public static bool IsEmpty(Vector2 obj)
        {
            return obj.x == 0 && obj.y == 0;
        }

        /// <summary>
        ///     Whether or not the given Vector2 object is empty
        /// </summary>
        /// <param name="obj">Vector2 Object</param>
        /// <returns>Is empty</returns>
        public static bool IsEmpty(Vector2Int obj)
        {
            return obj.x == 0 && obj.y == 0;
        }
    }
}