using System;
using UnityEngine;

namespace Pandawan.Islands.Tilemaps.Generation
{
    [Serializable]
    public class WorldGeneration
    {
        [SerializeField] private GenerationType type;
        [SerializeField] private BoundsInt islandSize;

        public void Generate(World world)
        {
            // Generate water everywhere around
            WaterGeneration(world);

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
            for (int y = islandSize.yMin; y < islandSize.yMax; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                if (world.IsEmptyTile(position)) world.SetTile(position, "grass");
            }
        }

        private void SquareNoBordersGeneration(World world)
        {
            for (int x = islandSize.xMin; x < islandSize.xMax; x++)
            for (int y = islandSize.yMin; y < islandSize.yMax; y++)
                if (x != islandSize.xMin && x != islandSize.xMax - 1 ||
                    y != islandSize.yMin && y != islandSize.yMax - 1)
                {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    if (world.IsEmptyTile(position)) world.SetTile(position, "grass");
                }
        }

        private void CircleGeneration(World world)
        {
            throw new NotImplementedException();
        }

        private void WaterGeneration(World world)
        {
            for (int x = islandSize.xMin - islandSize.size.x; x < islandSize.xMax + islandSize.size.x; x++)
            for (int y = islandSize.yMin - islandSize.size.y; y < islandSize.yMax + islandSize.size.y; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                if (world.IsEmptyTile(position)) world.SetTile(position, "water");
            }
        }

        private enum GenerationType
        {
            Square,
            SquareNoBorders,
            Circle
        }
    }
}