using System.Collections.Generic;
using UnityEngine;

namespace Pandawan.Islands.Other
{
    public static class BoundsIntExtensions
    {
        public static List<Vector3Int> ToList(this BoundsInt bounds)
        {
            List<Vector3Int> positions = new List<Vector3Int>();
            foreach (Vector3Int position in bounds.allPositionsWithin) positions.Add(position);

            return positions;
        }
    }
}