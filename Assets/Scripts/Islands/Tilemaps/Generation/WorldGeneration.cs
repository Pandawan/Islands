using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pandawan.Islands.Tilemaps.Generation
{
    [Serializable]
    public class WorldGeneration
    {
        [SerializeField] private GenerationType type;
        [SerializeField] private BoundsInt islandSize;

        public void Generate(World world)
        {
            // TODO: Find a seed system so that auto-generated tiles aren't saved by the world? Or perhaps something like, replace if empty? Idk...
            switch (type)
            {
                case GenerationType.Circle:
                    CircleGeneration(world);
                    break;
                case GenerationType.SquareNoBorders:
                    SquareNoBordersGeneration(world);
                    break;
                case GenerationType.Square:
                default:
                    SquareGeneration(world);
                    break;
            }

            world.GetChunkDataForTile(Vector3Int.zero).SetPositionProperty(Vector3Int.zero, "test", "123");
        }

        private void SquareGeneration(World world)
        {
            for (int x = islandSize.xMin; x < islandSize.xMax; x++)
            {
                for (int y = islandSize.yMin; y < islandSize.yMax; y++)
                {
                    world.SetTile(new Vector3Int(x, y, 0), "grass");
                }
            }
        }

        private void SquareNoBordersGeneration(World world)
        {
            for (int x = islandSize.xMin; x < islandSize.xMax; x++)
            {
                for (int y = islandSize.yMin; y < islandSize.yMax; y++)
                {
                    if ((x != islandSize.xMin && x != islandSize.xMax - 1) ||
                        (y != islandSize.yMin && y != islandSize.yMax - 1))
                    {
                        world.SetTile(new Vector3Int(x, y, 0), "grass");
                    }
                }
            }
        }

        private void CircleGeneration(World world)
        {
            throw new NotImplementedException();
        }

        private enum GenerationType
        {
            Square,
            SquareNoBorders,
            Circle
        }
    }
}