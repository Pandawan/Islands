using System;
using System.Collections.Generic;
using System.Linq;
using Pandawan.Islands.Other;
using Pandawan.Islands.Tilemaps.Generation;
using Pandawan.Islands.Tilemaps.Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pandawan.Islands.Tilemaps
{
    public class World : MonoBehaviour
    {
        public static World instance;

        [SerializeField] private WorldInfo worldInfo;
        [SerializeField] private WorldGeneration worldGen;
        [SerializeField] private Vector3Int chunkSize;
        [SerializeField] private Tilemap tilemap;

        private readonly Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
                Debug.LogError("Cannot have more than one World instance.");

            // TODO: Move WorldGen outside of world (see Start)
            if (worldGen == null) 
                Debug.LogError("No WorldGen set for World.");

            if (tilemap == null)
                Debug.LogError("No Tilemap set for World.");
        }

        private void Start()
        {
            // TODO: Move all of this outside of World
            // Load previous world save if exists
            // if (WorldManager.WorldExists(worldInfo.GetId())) WorldManager.Load(worldInfo.GetId(), this);

            // Initiate World Generation
            worldGen.Generate(this);

            // Save the newly generated world
            // WorldManager.Save(this);

            // Test ChunkData
            // Debug.Log(GetChunkDataForTile(Vector3Int.zero).GetPositionProperty(Vector3Int.zero, "test", "aaa"));
        }

        /// <summary>
        ///     Set the World's WorldInfo.
        /// </summary>
        /// <param name="info">WorldInfo object to set to.</param>
        public void SetWorldInfo(WorldInfo info)
        {
            worldInfo = info;
        }

        /// <summary>
        ///     Get the World's WorldInfo.
        /// </summary>
        /// <returns>The World Info object.</returns>
        public WorldInfo GetWorldInfo()
        {
            return worldInfo;
        }

        public override string ToString()
        {
            return $"World {worldInfo.name}";
        }

        #region Regions

        /// <summary>
        ///     Get a dictionary of every chunk within the given bounds.
        /// </summary>
        /// <param name="bounds">Bounds in Chunk Coordinates.</param>
        /// <returns>A Position-Chunk dictionary of the region.</returns>
        public Dictionary<Vector3Int, Chunk> GetRegion(BoundsInt bounds)
        {
            Dictionary<Vector3Int, Chunk> data = new Dictionary<Vector3Int, Chunk>();
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            for (int z = bounds.zMin; z < bounds.zMax; z++)
            {
                // Get the chunk at the given position
                Vector3Int chunkPosition = new Vector3Int(x, y, z);
                Chunk chunk = GetOrCreateChunk(chunkPosition);

                // If the chunk isn't empty, add it to the list
                if (!chunk.IsEmpty()) data.Add(chunkPosition, chunk);
            }

            return data;
        }

        /// <summary>
        ///     Loads every chunk in the given list into the world.
        /// </summary>
        /// <param name="chunksToLoad">The list of chunks to load.</param>
        public void LoadChunks(List<Chunk> chunksToLoad)
        {
            foreach (Chunk chunk in chunksToLoad)
            {
                // Load & Setup the chunk
                chunks.Add(chunk.position, chunk);
                chunk.Setup(chunkSize, tilemap);
                chunk.Load();
            }
        }


        /// <summary>
        ///     Find all the Dirty Chunks
        /// </summary>
        /// <returns>A List of all Dirty Chunks</returns>
        public List<Chunk> GetDirtyChunks()
        {
            // Find all the chunks that have IsDirty == true
            return chunks.Values.ToList().FindAll(x => x.IsDirty);
        }

        #endregion

        #region Chunk Abstraction

        /// <summary>
        ///     Convert the given Tile Position to its corresponding Chunk's position
        /// </summary>
        /// <param name="position">The position of the tile</param>
        /// <returns>The position of the chunk</returns>
        public Vector3Int GetChunkPositionForTile(Vector3Int position)
        {
            return new Vector3Int(
                position.x / chunkSize.x,
                position.y / chunkSize.y,
                position.z / chunkSize.z
            );
        }

        /// <summary>
        ///     Get, Load, or Create a chunk at the given position.
        /// </summary>
        /// <param name="chunkPosition">The chunk position.</param>
        private Chunk GetOrCreateChunk(Vector3Int chunkPosition)
        {
            // If it doesn't exist, create a new one
            if (!chunks.ContainsKey(chunkPosition)) chunks.Add(chunkPosition, new Chunk(chunkPosition, chunkSize, tilemap));

            return chunks[chunkPosition];
        }

        /// <summary>
        ///     Get the ChunkData object for the given chunk position.
        /// </summary>
        /// <param name="chunkPosition">The Chunk position.</param>
        public ChunkData GetChunkData(Vector3Int chunkPosition)
        {
            // If it doesn't exist
            if (!chunks.ContainsKey(chunkPosition)) Debug.LogError($"No Chunk found at position {chunkPosition}.");

            return chunks[chunkPosition].GetChunkData();
        }

        /// <summary>
        ///     Helper to get the ChunkData object for the Chunk that contains the given tile position.
        /// </summary>
        /// <param name="tilePosition">The Tile position to use.</param>
        public ChunkData GetChunkDataForTile(Vector3Int tilePosition)
        {
            return GetChunkData(GetChunkPositionForTile(tilePosition));
        }

        /// <summary>
        ///     Whether or not the given tile position is empty/has no tile.
        /// </summary>
        /// <param name="tilePosition">The position to check for.</param>
        /// <returns>True if there is no tile at the given position.</returns>
        public bool IsEmptyTileAt(Vector3Int tilePosition)
        {
            // If the Tile doesn't exist OR its value is empty
            return GetTileAt(tilePosition) == null ||
                   string.IsNullOrEmpty(GetTileAt(tilePosition).Id);
        }

        /// <summary>
        ///     Get the tile at the given position.
        /// </summary>
        /// <param name="tilePosition">The position to get the tile at.</param>
        public BasicTile GetTileAt(Vector3Int tilePosition)
        {
            // Get a chunk at the corresponding Chunk position for the given tile position
            Chunk chunk = GetOrCreateChunk(GetChunkPositionForTile(tilePosition));
            return chunk.GetTileAt(tilePosition);
        }

        /// <summary>
        ///     Set a tile in the world using an id.
        /// </summary>
        /// <param name="tilePosition">The position to set the tile at.</param>
        /// <param name="id">The id of tile to set.</param>
        public void SetTileAt(Vector3Int tilePosition, string id)
        {
            // Get a chunk at the corresponding Chunk position for the given tile position
            Chunk chunk = GetOrCreateChunk(GetChunkPositionForTile(tilePosition));

            chunk.SetTileAt(tilePosition, id);
        }

        /// <summary>
        ///     Set a tile in the world using a BasicTile.
        /// </summary>
        /// <param name="tilePosition">The position to set the tile at.</param>
        /// <param name="tile">The BasicTile object to set.</param>
        public void SetTileAt(Vector3Int tilePosition, BasicTile tile)
        {
            // Get a chunk at the corresponding Chunk position for the given tile position
            Chunk chunk = GetOrCreateChunk(GetChunkPositionForTile(tilePosition));

            chunk.SetTileAt(tilePosition, tile);
        }

        /// <summary>
        /// Remove the tile at the given position.
        /// </summary>
        /// <param name="tilePosition">The position to remove the tile at.</param>
        public void RemoveTileAt(Vector3Int tilePosition)
        {
            // TODO: Might want to make it so that removing and getting a tile doesn't CREATE a new chunk if none exists (and if there is no loadable chunk)
            Chunk chunk = GetOrCreateChunk(GetChunkPositionForTile(tilePosition));

            chunk.RemoveTileAt(tilePosition);
        }

        #endregion
    }

    [Serializable]
    public struct WorldInfo
    {
        [SerializeField] public string name;

        /// <summary>
        ///     Get the World Id.
        ///     This is a FileSystem-safe id in snake_case.
        /// </summary>
        /// <returns>The world's id.</returns>
        public string GetId()
        {
            return Utilities.RemoveIllegalFileCharacters(name.ToLower().Replace(" ", "_"));
        }
    }
}