using System;
using System.Threading.Tasks;
using Pandawan.Islands.Other;
using UnityEngine;

namespace Pandawan.Islands.Tilemaps.Generation
{
    [RequireComponent(typeof(World))]
    public class WorldGeneration : MonoBehaviour
    {
        [SerializeField] private GenerationType type = GenerationType.None;
        [SerializeField] private BoundsInt islandSize = new BoundsInt(Vector3Int.one * -1, Vector3Int.one);

        private World worldComponent;

        private void Awake()
        {
            worldComponent = GetComponent<World>();

            if (worldComponent == null)
            {
                Debug.LogError("World Component is required for WorldGeneration to act.");
                return;
            }

            worldComponent.GenerationEvent += Generate;
        }

        public async Task Generate(World world)
        {
            // TODO: Find a seed system so that auto-generated tiles aren't saved by the world? Or perhaps something like, replace if empty? Idk...
            switch (type)
            {
                case GenerationType.None:
                    return;
                case GenerationType.Perlin:
                    await PerlinGeneration(world);
                    break;
                case GenerationType.Circle:
                    CircleGeneration(world);
                    break;
                case GenerationType.SquareNoBorders:
                    await SquareNoBordersGeneration(world);
                    break;
                case GenerationType.Square:
                default:
                    await SquareGeneration(world);
                    break;
            }

            // Generate water everywhere around
            // WaterGeneration(world);

            world.GetChunkDataForTile(Vector3Int.zero).SetPositionProperty(Vector3Int.zero, "test", "123");
            Debug.Log(
                world.GetChunkDataForTile(Vector3Int.zero).GetAllPropertiesAt(Vector3Int.zero).ToStringFlattened());

            Debug.Log("Successfully Generated World.");
        }

        private async Task PerlinGeneration(World world)
        {
            // TODO: Negative values are inverse of positive, find a fix for this (maybe local coordinates?)
            // Perlin returns the same value for ints, use a scaler to prevent this
            float scaler = 0.125f;

            for (int x = islandSize.xMin; x < islandSize.xMax; x++)
            for (int y = islandSize.yMin; y < islandSize.yMax; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                float height = Mathf.PerlinNoise(x * scaler, y * scaler);
                string tile = "water";
                if (height > 0.35f) tile = "grass";
                if (await world.IsEmptyTileAt(position)) await world.SetTileAt(position, tile);
            }
        }

        private async Task SquareGeneration(World world)
        {
            for (int x = islandSize.xMin; x < islandSize.xMax; x++)
            for (int y = islandSize.yMin; y < islandSize.yMax; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                if (await world.IsEmptyTileAt(position)) await world.SetTileAt(position, "grass");
            }
        }

        private async Task SquareNoBordersGeneration(World world)
        {
            for (int x = islandSize.xMin; x < islandSize.xMax; x++)
            for (int y = islandSize.yMin; y < islandSize.yMax; y++)
                if (x != islandSize.xMin && x != islandSize.xMax - 1 ||
                    y != islandSize.yMin && y != islandSize.yMax - 1)
                {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    if (await world.IsEmptyTileAt(position)) await world.SetTileAt(position, "grass");
                }
        }

        private void CircleGeneration(World world)
        {
            throw new NotImplementedException();
        }

        private async Task WaterGeneration(World world)
        {
            for (int x = islandSize.xMin - islandSize.size.x; x < islandSize.xMax + islandSize.size.x; x++)
            for (int y = islandSize.yMin - islandSize.size.y; y < islandSize.yMax + islandSize.size.y; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                if (await world.IsEmptyTileAt(position)) await world.SetTileAt(position, "water");
            }
        }

        private enum GenerationType
        {
            None,
            Square,
            SquareNoBorders,
            Circle,
            Perlin
        }
    }
}