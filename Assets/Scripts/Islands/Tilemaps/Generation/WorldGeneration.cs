using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pandawan.Islands.Tilemaps.Generation
{
    public class WorldGeneration : MonoBehaviour
    {
        [SerializeField] private BoundsInt islandSize;

        public void Generate(World world)
        {
            for (int x = islandSize.xMin; x < islandSize.xMax; x++)
            {
                for (int y = islandSize.yMin; y < islandSize.yMax; y++)
                {
                    world.SetTile(new Vector3Int(x, y, 0), "grass");
                }
            }
        }
    }
}