using System;
using System.Collections.Generic;
using System.Linq;
using Pandawan.Islands.Other;
using Pandawan.Islands.Tilemaps.Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pandawan.Islands.Tilemaps
{
    public class World : MonoBehaviour
    {
        public delegate void WorldEvent(World world);

        public static World instance;

        [SerializeField] private WorldInfo worldInfo;
        [SerializeField] private Vector3Int chunkSize;
        [SerializeField] private Tilemap tilemap;

        // TODO: This could be coupled to the Chunk itself by adding an extra public field and using chunks Dictionary directly.
        // Keeps track of the last "loading" request for each chunk
        private readonly Dictionary<Vector3Int, List<ChunkLoader>> chunkLoadRequests =
            new Dictionary<Vector3Int, List<ChunkLoader>>();

        // Keeps track of every chunk
        private readonly Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

        public event WorldEvent GenerationEvent;

        #region World Lifecycle

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
                Debug.LogError("Cannot have more than one World instance.");

            if (tilemap == null)
                Debug.LogError("No Tilemap set for World.");
        }

        private void Start()
        {
            // TODO: Call WorldManager.LoadWorld & Find place to call WorldManager.SaveWorld

            // Call any event handler subscribed to World.GenerationEvent
            GenerationEvent?.Invoke(this);
        }

        private void Update()
        {
            DiscardOldChunks();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 size = chunkSize;
            foreach (Vector3 position in chunks.Keys)
            {
                position.Scale(size);
                Gizmos.DrawWireCube(position + size / 2, size);
            }
        }

        private void DiscardOldChunks()
        {
            // TODO: Clean this up so I don't have to use null loaders
            // Removes every chunk request that has a null loader (aka one-time load request)
            foreach (KeyValuePair<Vector3Int, List<ChunkLoader>> chunkLoadRequest in chunkLoadRequests.ToList())
            {
                // Get every chunkLoadRequest which has a null loader
                List<ChunkLoader> loaders = chunkLoadRequest.Value.FindAll(loader => loader == null);
                foreach (ChunkLoader chunkLoader in loaders)
                    // Remove the chunk
                    RequestChunkUnLoading(chunkLoadRequest.Key, chunkLoader);
            }
        }

        #endregion

        #region Chunk Loading

        /// <summary>
        ///     Request the loading of the chunk at the given position.
        /// </summary>
        /// <param name="chunkPosition">The chunk position at which to load.</param>
        /// <param name="requester">The ChunkLoader that requested to load this chunk.</param>
        public void RequestChunkLoading(Vector3Int chunkPosition, ChunkLoader requester)
        {
            RequestChunkLoading(new List<Vector3Int> {chunkPosition}, requester);
        }

        /// <summary>
        ///     Request the loading of the chunk within the given bounds.
        /// </summary>
        /// <param name="chunkPositions">The chunk positions at which to load.</param>
        /// <param name="requester">The ChunkLoader that requested to load this chunk.</param>
        public void RequestChunkLoading(List<Vector3Int> chunkPositions, ChunkLoader requester)
        {
            // TODO: Optimize this so it loads multiple chunks at once (using the same FileStream in WorldManager)
            // Load every chunkPosition
            foreach (Vector3Int chunkPosition in chunkPositions)
                // Simply calling GetOrCreateChunk works
                GetOrCreateChunk(chunkPosition, requester);
        }

        /// <summary>
        ///     Request the unloading of the chunk at the given position.
        /// </summary>
        /// <param name="chunkPosition">The chunk position at which to unload.</param>
        /// <param name="requester">The ChunkLoader asking </param>
        public void RequestChunkUnLoading(Vector3Int chunkPosition, ChunkLoader requester)
        {
            RequestChunkUnLoading(new List<Vector3Int> {chunkPosition}, requester);
        }

        /// <summary>
        ///     Request the unloading of the chunk at the given position.
        /// </summary>
        /// <param name="chunkPositions">The chunk positions at which to unload.</param>
        /// <param name="requester">The ChunkLoader that requested to unload this chunk.</param>
        public void RequestChunkUnLoading(List<Vector3Int> chunkPositions, ChunkLoader requester)
        {
            List<Chunk> chunksToSave = new List<Chunk>();

            foreach (Vector3Int chunkPosition in chunkPositions)
            {
                if (!chunkLoadRequests[chunkPosition].Exists(loader => loader == requester))
                {
                    Debug.LogError($"Given {requester} never requested for chunk loading at {chunkPosition}.");
                    continue;
                }

                chunkLoadRequests[chunkPosition].Remove(requester);

                // If that Chunk is no longer needed
                if (chunkLoadRequests[chunkPosition].Count == 0)
                {
                    // Add the chunk to be saved if they have been modified
                    if (chunks[chunkPosition].IsDirty)
                        chunksToSave.Add(chunks[chunkPosition]);
                    else
                        chunks[chunkPosition].Clear(false);

                    // Remove that chunkPosition from all records/dictionaries
                    chunks.Remove(chunkPosition);
                    chunkLoadRequests.Remove(chunkPosition);
                }
            }

            // Save all of the chunks in the chunksToSave List
            if (chunksToSave.Count > 0)
            {
                WorldManager.SaveChunks(chunksToSave, worldInfo);
                // Clear them once saved
                foreach (Chunk chunk in chunksToSave) chunk.Clear(false);
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

        // TODO: Refactor so I don't have to pass in a loader (aka find another way to make the whole "loader" system)
        /// <summary>
        ///     Get, Load, or Create a chunk at the given position.
        /// </summary>
        /// <param name="chunkPosition">The chunk position.</param>
        /// <param name="loader">The loader that asked to get this chunk.</param>
        private Chunk GetOrCreateChunk(Vector3Int chunkPosition, ChunkLoader loader = null)
        {
            // If it doesn't exist, create a new one
            if (!chunks.ContainsKey(chunkPosition))
            {
                // TODO: Make it so WorldManager accepts both List and normal
                // WorldManager works in Lists, create a list with that element
                List<Vector3Int> chunkPositions = new List<Vector3Int> {chunkPosition};

                // TODO: Refactor this so I don't repeat that line twice.
                if (WorldManager.ChunksExist(chunkPositions, worldInfo))
                {
                    // Load the chunk from FS if possible
                    List<Chunk> chunk = WorldManager.LoadChunk(chunkPositions, worldInfo);
                    if (chunk != null && chunk.Count > 0)
                    {
                        // Add & Load new chunk into tilemap
                        chunks.Add(chunkPosition, chunk[0]);
                        chunk[0].Setup(chunkSize, tilemap);
                    }
                    else
                    {
                        chunks.Add(chunkPosition, new Chunk(chunkPosition, chunkSize, tilemap));
                    }
                }
                else
                {
                    chunks.Add(chunkPosition, new Chunk(chunkPosition, chunkSize, tilemap));
                }
            }

            // Add the loader to the chunkLoadRequests
            if (chunkLoadRequests.ContainsKey(chunkPosition))
                chunkLoadRequests[chunkPosition].Add(loader);
            else
                chunkLoadRequests.Add(chunkPosition, new List<ChunkLoader> {loader});


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
            Vector3Int chunkPosition = PositionUtilities.TileToChunkPosition(tilePosition, chunkSize);
            return GetChunkData(chunkPosition);
        }

        #endregion

        #region Tile Modification

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
            Vector3Int chunkPosition = PositionUtilities.TileToChunkPosition(tilePosition, chunkSize);
            Chunk chunk = GetOrCreateChunk(chunkPosition);
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
            Vector3Int chunkPosition = PositionUtilities.TileToChunkPosition(tilePosition, chunkSize);
            Chunk chunk = GetOrCreateChunk(chunkPosition);

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
            Vector3Int chunkPosition = PositionUtilities.TileToChunkPosition(tilePosition, chunkSize);
            Chunk chunk = GetOrCreateChunk(chunkPosition);

            chunk.SetTileAt(tilePosition, tile);
        }

        /// <summary>
        ///     Remove the tile at the given position.
        /// </summary>
        /// <param name="tilePosition">The position to remove the tile at.</param>
        public void RemoveTileAt(Vector3Int tilePosition)
        {
            // TODO: Might want to make it so that removing and getting a tile doesn't CREATE a new chunk if none exists (and if there is no loadable chunk)
            Vector3Int chunkPosition = PositionUtilities.TileToChunkPosition(tilePosition, chunkSize);
            Chunk chunk = GetOrCreateChunk(chunkPosition);

            chunk.RemoveTileAt(tilePosition);
        }

        #endregion

        #region World Info

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
        public WorldInfo GetWorldInfo()
        {
            return worldInfo;
        }

        #endregion

        #region Accessors & Overrides

        /// <summary>
        ///     Get the World's ChunkSize.
        /// </summary>
        public Vector3Int GetChunkSize()
        {
            return chunkSize;
        }

        public override string ToString()
        {
            return $"World {worldInfo.name}";
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

        public WorldInfo(string name)
        {
            this.name = name;
        }
    }
}