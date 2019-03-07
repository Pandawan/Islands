using System.Collections.Generic;
using System.Threading.Tasks;
using Pandawan.Islands.Tilemaps.Tiles;
using UnityEngine;

namespace Pandawan.Islands.Tilemaps
{
    /**
     * ChunkOperations are used by the World to run async Tasks in series.
     * This allows for smooth chunk loading/unloading.
     *
     * To create a new ChunkOperation, extend from IChunkOperation.
     * Use the Execute() method to have your custom operation on the World.
     *
     * Make sure to call ExecuteCompletionSource.SetResult(true) (or false, or Exception) at the end of the Execute
     * so the original caller knows when the Task ended.
     *
     * Inside those operations, you usually want to use the World's internal "_Method()" because they work outside of ChunkOperations,
     * while the public ones create a new ChunkOperation every time they are called.
     *
     */


    public abstract class IChunkOperation
    {
        /// <summary>
        /// A string to identify this operation's type (if not using C# GetType).
        /// </summary>
        public string Type { get; }

        // public List<Vector3Int> ChunkPosition { get; }
        public Vector3Int ChunkPosition { get; }

        /// <summary>
        /// Used by the ChunkOperations Queue so the original caller can await the Execute task.
        /// </summary>
        public TaskCompletionSource<bool> ExecuteCompletionSource { get; }
        
        protected IChunkOperation(string type, Vector3Int chunkPosition)
        {
            Type = type;
            // ChunkPosition = new List<Vector3Int>() {chunkPosition};
            ChunkPosition = chunkPosition;

            ExecuteCompletionSource = new TaskCompletionSource<bool>();
        }
        
        /*
        protected IChunkOperation(string type, List<Vector3Int> chunkPosition)
        {
            Type = type;
            ChunkPosition = chunkPosition;
        }
        */
        
        // NOTE: Execute should always call "ExecuteCompletionSource.SetResult()" at the end.
        /// <summary>
        /// Execute the Operation.
        /// (Called by the World.ProcessOperations method).
        /// </summary>
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
            Result = await world._GetTileAt(TilePosition);

            ExecuteCompletionSource.SetResult(true);
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
                await world._SetTileAt(TilePosition, Tile);
            else
                await world._SetTileAt(TilePosition, TileId);
            
            ExecuteCompletionSource.SetResult(true);
        }
    }
    
    public class RemoveTileOperation : IChunkOperation {
        public Vector3Int TilePosition { get; }
        
        public RemoveTileOperation(Vector3Int tilePosition, World world) : base("remove",
            world.TileToChunkPosition(tilePosition))
        {
            TilePosition = tilePosition;
        }

        public override async Task Execute(World world)
        {
            await world._RemoveTileAt(TilePosition);
            ExecuteCompletionSource.SetResult(true);
        }
    }

    public class IsEmptyTileOperation : IChunkOperation {
        public Vector3Int TilePosition { get; }
        
        public bool Result { get; private set; }

        public IsEmptyTileOperation(Vector3Int tilePosition, World world) : base("is_empty",
            world.TileToChunkPosition(tilePosition))
        {
            TilePosition = tilePosition;
        }

        public override async Task Execute(World world)
        {
            Result = await world._IsEmptyTileAt(TilePosition);
            ExecuteCompletionSource.SetResult(true);
        }
    }

    /*
    public class LoadChunkOperation : IChunkOperation {

        public ChunkLoader Requester { get; }

        public LoadChunkOperation(List<Vector3Int> chunkPosition, ChunkLoader requester) : base("load",
            chunkPosition)
        {
            Requester = requester;
        }

        public override async Task Execute(World world)
        {
            await world.RequestChunkLoading(ChunkPosition, Requester);
        }
    }
    
    public class UnloadChunkOperation : IChunkOperation {

        public ChunkLoader Requester { get; }

        public UnloadChunkOperation(List<Vector3Int> chunkPosition, ChunkLoader requester) : base("unload",
            chunkPosition)
        {
            Requester = requester;
        }

        public override async Task Execute(World world)
        {
            await world.RequestChunkUnloading(ChunkPosition, Requester);
        }
    }
    */
}