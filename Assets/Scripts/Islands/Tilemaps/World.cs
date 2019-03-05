using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pandawan.Islands.Other;
using Pandawan.Islands.Tilemaps.Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pandawan.Islands.Tilemaps
{
    public class World : MonoBehaviour
    {
        // TODO: Should this allow for Tasks
        public delegate Task WorldEvent(World world);

        public static World instance;

        [SerializeField] private WorldInfo worldInfo = WorldInfo.Default;
        [SerializeField] private Vector3Int chunkSize = Vector3Int.one;
        [SerializeField] private Tilemap tilemap;

        // Keeps track of current loading tasks for each chunk
        private readonly Dictionary<Vector3Int, Task<List<Chunk>>> chunkLoadingTasks =
            new Dictionary<Vector3Int, Task<List<Chunk>>>();

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

        private async void Start()
        {
            // TODO: Call WorldManager.LoadWorld & Find place to call WorldManager.SaveWorld

            // Call any event handler subscribed to World.GenerationEvent
            // TODO: Should this use "await GenerationEvent.Invoke()" ? 
            if (GenerationEvent != null)
                await GenerationEvent.Invoke(this);
        }

        private async void Update()
        {
            await DiscardOldChunks();
        }

        private void OnDisable()
        {
            foreach (Task<List<Chunk>> chunkLoadingTask in chunkLoadingTasks.Values)
                // TODO: Instead of Dispose() use a CancellationToken and cancel all of these tasks
                chunkLoadingTask.Dispose();
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

        private async Task DiscardOldChunks()
        {
            // TODO: Clean this up so I don't have to use null loaders
            // Removes every chunk request that has a null loader (aka one-time load request)
            foreach (KeyValuePair<Vector3Int, List<ChunkLoader>> chunkLoadRequest in chunkLoadRequests.ToList())
            {
                // Get every chunkLoadRequest which has a null loader
                List<ChunkLoader> loaders = chunkLoadRequest.Value.FindAll(loader => loader == null);
                foreach (ChunkLoader chunkLoader in loaders)
                    // Remove the chunk
                    await RequestChunkUnLoading(chunkLoadRequest.Key, chunkLoader);
            }
        }

        #endregion

        #region Chunk Loading

        /// <summary>
        ///     Request the loading of the chunk at the given position.
        /// </summary>
        /// <param name="chunkPosition">The chunk position at which to load.</param>
        /// <param name="requester">The ChunkLoader that requested to load this chunk.</param>
        public async Task RequestChunkLoading(Vector3Int chunkPosition, ChunkLoader requester)
        {
            await RequestChunkLoading(new List<Vector3Int> {chunkPosition}, requester);
        }

        /// <summary>
        ///     Request the loading of the chunk within the given bounds.
        /// </summary>
        /// <param name="chunkPositions">The chunk positions at which to load.</param>
        /// <param name="requester">The ChunkLoader that requested to load this chunk.</param>
        public async Task RequestChunkLoading(List<Vector3Int> chunkPositions, ChunkLoader requester)
        {
            // TODO: Optimize this so it loads multiple chunks at once (using the same FileStream in WorldManager)
            // TODO: Maybe make this a coroutine so that it can wait until the end of the frame and load multiple chunks at once?
            // Load every chunkPosition
            foreach (Vector3Int chunkPosition in chunkPositions)
            {
                Debug.Log("Loading chunk " + chunkPosition);

                // Simply calling GetOrCreateChunk works
                await GetOrCreateChunk(chunkPosition, requester);
            }
        }

        /// <summary>
        ///     Request the unloading of the chunk at the given position.
        /// </summary>
        /// <param name="chunkPosition">The chunk position at which to unload.</param>
        /// <param name="requester">The ChunkLoader asking </param>
        public async Task RequestChunkUnLoading(Vector3Int chunkPosition, ChunkLoader requester)
        {
            await RequestChunkUnLoading(new List<Vector3Int> {chunkPosition}, requester);
        }

        /// <summary>
        ///     Request the unloading of the chunk at the given position.
        /// </summary>
        /// <param name="chunkPositions">The chunk positions at which to unload.</param>
        /// <param name="requester">The ChunkLoader that requested to unload this chunk.</param>
        public async Task RequestChunkUnLoading(List<Vector3Int> chunkPositions, ChunkLoader requester)
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
                    Debug.Log("Unloading chunk " + chunkPosition);
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
                await WorldManager.SaveChunks(chunksToSave, worldInfo);
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
        // TODO: Refactor so it's all in multiple methods...
        /// <summary>
        ///     Get, Load, or Create a chunk at the given position.
        /// </summary>
        /// <param name="chunkPosition">The chunk position.</param>
        /// <param name="loader">The loader that asked to get this chunk.</param>
        private async Task<Chunk> GetOrCreateChunk(Vector3Int chunkPosition, ChunkLoader loader = null)
        {
            // If that chunk is currently loading, wait until it's done
            if (chunkLoadingTasks.ContainsKey(chunkPosition) && chunkLoadingTasks != null)
                await chunkLoadingTasks[chunkPosition];

            // If it doesn't exist, create a new one
            if (!chunks.ContainsKey(chunkPosition))
            {
                // TODO: Make it so WorldManager accepts both List and normal
                // WorldManager works in Lists, create a list with that element
                List<Vector3Int> chunkPositions = new List<Vector3Int> {chunkPosition};

                // TODO: Refactor this so I don't repeat that line twice.
                if (WorldManager.ChunksExist(chunkPositions, worldInfo))
                {
                    // TODO: Stop WorldManager using List<Chunk> for everything
                    // If the chunk is not currently loading, loading it
                    // Add the loading task to the list
                    chunkLoadingTasks.Add(chunkPosition, WorldManager.LoadChunk(chunkPositions, worldInfo));

                    // Load the chunk from FS if possible
                    // WorldManager uses List<Chunk> for everything so keep it all in that
                    List<Chunk> chunkList = await chunkLoadingTasks[chunkPosition];

                    // Once done loading (after await), remove it from the loading task list
                    chunkLoadingTasks.Remove(chunkPosition);

                    // If it was able to load a chunk
                    if (chunkList != null && chunkList.Count > 0)
                    {
                        // Add & Load new chunk into tilemap
                        chunks.Add(chunkPosition, chunkList[0]);

                        chunkList[0].Setup(chunkSize, tilemap);
                    }
                    // If it couldn't load any chunk
                    else
                    {
                        chunks.Add(chunkPosition, new Chunk(chunkPosition, chunkSize, tilemap));
                        // TODO: Add Chunk generation.
                    }
                }
                else
                {
                    chunks.Add(chunkPosition, new Chunk(chunkPosition, chunkSize, tilemap));
                    // TODO: Add Chunk generation.
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
            // TODO: Make this use GetOrCreateChunk?
            return chunks[chunkPosition].GetChunkData();
        }

        /// <summary>
        ///     Helper to get the ChunkData object for the Chunk that contains the given tile position.
        /// </summary>
        /// <param name="tilePosition">The Tile position to use.</param>
        public ChunkData GetChunkDataForTile(Vector3Int tilePosition)
        {
            Vector3Int chunkPosition = TileToChunkPosition(tilePosition);
            return GetChunkData(chunkPosition);
        }

        #endregion

        #region Tile Modification

        /// <summary>
        ///     Whether or not the given tile position is empty/has no tile.
        /// </summary>
        /// <param name="tilePosition">The position to check for.</param>
        /// <returns>True if there is no tile at the given position.</returns>
        public async Task<bool> IsEmptyTileAt(Vector3Int tilePosition)
        {
            // If the Tile doesn't exist OR its value is empty
            return await GetTileAt(tilePosition) == null ||
                   string.IsNullOrEmpty((await GetTileAt(tilePosition)).Id);
        }

        /// <summary>
        ///     Get the tile at the given position.
        /// </summary>
        /// <param name="tilePosition">The position to get the tile at.</param>
        public async Task<BasicTile> GetTileAt(Vector3Int tilePosition)
        {
            // Get a chunk at the corresponding Chunk position for the given tile position
            Vector3Int chunkPosition = TileToChunkPosition(tilePosition);
            Chunk chunk = await GetOrCreateChunk(chunkPosition);
            return chunk.GetTileAt(tilePosition);
        }

        /// <summary>
        ///     Set a tile in the world using an id.
        /// </summary>
        /// <param name="tilePosition">The position to set the tile at.</param>
        /// <param name="id">The id of tile to set.</param>
        public async Task SetTileAt(Vector3Int tilePosition, string id)
        {
            // Get a chunk at the corresponding Chunk position for the given tile position
            Vector3Int chunkPosition = TileToChunkPosition(tilePosition);
            Chunk chunk = await GetOrCreateChunk(chunkPosition);

            chunk.SetTileAt(tilePosition, id);
        }

        /// <summary>
        ///     Set a tile in the world using a BasicTile.
        /// </summary>
        /// <param name="tilePosition">The position to set the tile at.</param>
        /// <param name="tile">The BasicTile object to set.</param>
        public async Task SetTileAt(Vector3Int tilePosition, BasicTile tile)
        {
            // Get a chunk at the corresponding Chunk position for the given tile position
            Vector3Int chunkPosition = TileToChunkPosition(tilePosition);
            Chunk chunk = await GetOrCreateChunk(chunkPosition);

            chunk.SetTileAt(tilePosition, tile);
        }

        /// <summary>
        ///     Remove the tile at the given position.
        /// </summary>
        /// <param name="tilePosition">The position to remove the tile at.</param>
        public async Task RemoveTileAt(Vector3Int tilePosition)
        {
            // TODO: Might want to make it so that removing and getting a tile doesn't CREATE a new chunk if none exists (and if there is no loadable chunk)
            Vector3Int chunkPosition = TileToChunkPosition(tilePosition);
            Chunk chunk = await GetOrCreateChunk(chunkPosition);

            chunk.RemoveTileAt(tilePosition);
        }

        #endregion

        #region Position Utilities

        /// <summary>
        ///     Convert the given tile position to its corresponding chunk's position.
        /// </summary>
        /// <param name="tilePosition">The tile position to convert from.</param>
        /// <param name="roundUp">Whether to round up (Ceil) instead of truncating (Floor)</param>
        /// <returns>The resulting chunk position.</returns>
        public Vector3Int TileToChunkPosition(Vector3Int tilePosition, bool roundUp = false)
        {
            // Use Mathf.CeilToInt if rounding up
            if (roundUp)
                return new Vector3Int(
                    Mathf.CeilToInt((float) tilePosition.x / (tilePosition.x < 0 ? chunkSize.x + 1 : chunkSize.x) +
                                    (tilePosition.x < 0 ? -1 : 0)),
                    Mathf.CeilToInt((float) tilePosition.y / (tilePosition.y < 0 ? chunkSize.y + 1 : chunkSize.y) +
                                    (tilePosition.y < 0 ? -1 : 0)),
                    Mathf.CeilToInt((float) tilePosition.z / (tilePosition.z < 0 ? chunkSize.z + 1 : chunkSize.z) +
                                    (tilePosition.z < 0 ? -1 : 0))
                );

            // Not rounding up, just let int division take care of truncating
            return new Vector3Int(
                tilePosition.x / (tilePosition.x < 0 ? chunkSize.x + 1 : chunkSize.x) +
                (tilePosition.x < 0 ? -1 : 0),
                tilePosition.y / (tilePosition.y < 0 ? chunkSize.y + 1 : chunkSize.y) +
                (tilePosition.y < 0 ? -1 : 0),
                tilePosition.z / (tilePosition.z < 0 ? chunkSize.z + 1 : chunkSize.z) +
                (tilePosition.z < 0 ? -1 : 0)
            );
        }

        /// <summary>
        ///     Convert the given tile bounds to chunk bounds, making sure that all chunks are included.
        ///     Including the chunks that are only partially contained within the tile bounds.
        /// </summary>
        /// <param name="tileBounds">The tile position boundaries to convert from.</param>
        /// <returns>The resulting chunk bounds.</returns>
        public BoundsInt TileToChunkBounds(BoundsInt tileBounds)
        {
            Vector3Int min = TileToChunkPosition(tileBounds.min);
            Vector3Int max = TileToChunkPosition(tileBounds.max, true);
            Vector3Int size = max - min;

            BoundsInt bounds = new BoundsInt(min.x, min.y, min.z, size.x, size.y, size.z);

            return bounds;
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

        /// <summary>
        /// NOTE YOU ALMOST NEVER WANT TO DO THIS!
        /// </summary>
        /// <returns></returns>
        public Tilemap GetTilemap()
        {
            return tilemap;
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
        public static WorldInfo Default { get; } = new WorldInfo("world");

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