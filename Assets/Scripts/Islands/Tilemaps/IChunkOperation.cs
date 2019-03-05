using System.Threading.Tasks;
using Pandawan.Islands.Tilemaps.Tiles;
using UnityEngine;

namespace Pandawan.Islands.Tilemaps
{
    public abstract class IChunkOperation
    {
        public string Type { get; }

        public Vector3Int ChunkPosition { get; }

        protected IChunkOperation(string type, Vector3Int chunkPosition)
        {
            Type = type;
            ChunkPosition = chunkPosition;
        }

        public abstract Task Execute(World world);
    }

    public class GetTileOperation : IChunkOperation
    {
        public Vector3Int TilePosition { get; }

        public BasicTile Result { get; private set; }

        public GetTileOperation(Vector3Int tilePosition, World world) : base("get",
            world.TileToChunkPosition(tilePosition))
        {
            TilePosition = tilePosition;
        }

        public override async Task Execute(World world)
        {
            Result = await world.GetTileAt(TilePosition);
        }
    }

    public class SetTileOperation : IChunkOperation
    {
        public Vector3Int TilePosition { get; }

        public BasicTile Tile { get; }

        public string TileId { get; }

        public SetTileOperation(Vector3Int tilePosition, BasicTile tile, World world) : base("set",
            world.TileToChunkPosition(tilePosition))
        {
            TilePosition = tilePosition;
            Tile = tile;
            TileId = tile.Id;
        }

        public SetTileOperation(Vector3Int tilePosition, string tileId, World world) : base("set",
            world.TileToChunkPosition(tilePosition))
        {
            TilePosition = tilePosition;
            TileId = tileId;
            Tile = null;
        }

        public override async Task Execute(World world)
        {
            if (Tile != null)
                await world.SetTileAt(TilePosition, Tile);
            else
                await world.SetTileAt(TilePosition, TileId);
        }
    }
}