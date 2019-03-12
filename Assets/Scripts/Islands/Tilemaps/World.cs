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
    /// <summary>
    ///
    ///     TODO: Find out why rapid chunk loading/unloading (usually in diagonal) sometimes keeps one chunk loaded at the corners.
    ///     (This might be because of request order and could be solved with chunk operations, maybe).
    /// 
    ///     TODO: Verify that using multiple ChunkLoaders OR multiple Get/Set requests (or both) doesn't cause issues with
    ///     chunk loading. (This might require making Load/Unload a ChunkOperation).
    ///
    /// 
    ///     TODO TO RESEARCH: I'm unsure but it looks like the WorldManager.Save/Load are not using a separate thread, this might be causing small lags.
    /// 
    ///
    ///     TODO HACK FIXED:
    ///     - On line 77, there was an issue with ProcessOperations() being called multiple times (because Update) which lead to issues with loading/saving at the same time.
    ///     Resulting in file sharing errors. I was able to fix it by adding a simple if statement (that checks if ProcessOperations() is already running), but it would be nice to fix it correctly.
    ///     
    /// </summary>

    // This is a rewrite of the Chunk Loading systems for World.cs into a neater class
    public class World : MonoBehaviour
    {
        public delegate Task WorldEvent(World world);

        public static World instance;

        [SerializeField] private WorldInfo worldInfo = WorldInfo.Default;

        [SerializeField] private Vector3Int chunkSize = Vector3Int.one;

        [SerializeField] private Tilemap tilemap;

        // List of every chunk loader that requested for this chunk to be loaded
        private readonly Dictionary<Vector3Int, List<ChunkLoader>> chunkLoadingRequests =
            new Dictionary<Vector3Int, List<ChunkLoader>>();

        // Tasks of chunks that are currently being loaded
        private readonly Dictionary<Vector3Int, Task<List<Chunk>>> chunkLoadingTasks =
            new Dictionary<Vector3Int, Task<List<Chunk>>>();

        // Operations waiting to be applied to chunks
        private readonly Queue<IChunkOperation> chunkOperations = new Queue<IChunkOperation>();

        // Every chunk that is currently loaded in the world
        private readonly Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

        private bool isProcessingOperations;

        public event WorldEvent GenerationEvent;

        #region Lifecycle

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
            if (GenerationEvent != null)
                await GenerationEvent.Invoke(this);
        }

        // TODO: Make sure that all of these finish being processed before the game closes
        private async void Update()
        {
            // TODO: Find a better way to fix this issue. There must be a way...
            // Making sure only one "ProcessOperations()" is running at a time.
            if (!isProcessingOperations)
                await ProcessOperations();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            if (chunks.Count > 0)
                foreach (Vector3Int chunkPositions in chunks.Keys)
                    Gizmos.DrawWireCube(chunkPositions * chunkSize + (Vector3) chunkSize / 2, chunkSize);
        }

        #endregion

        #region Chunk Operations

        /// <summary>
        ///     <para>
        ///         Process all the ChunkOperations currently in the queue.
        ///         Everything is asynchronous and should load (at best) by grouping everything into lists before saving/loading.
        ///     </para>
        ///     <a href="http://www.stevevermeulen.com/index.php/2017/09/using-async-await-in-unity3d-2017/">
        ///         See for more info on async + Unity.
        ///     </a>
        /// </summary>
        private async Task ProcessOperations()
        {
            isProcessingOperations = true;

            List<Chunk> chunksToUnload = new List<Chunk>();

            // If there are any elements in the queue of operations
            while (chunkOperations.Any())
            {
                // Get the first operation and execute it
                IChunkOperation operation = chunkOperations.Dequeue();

                // Execute the task, and wait for it to return
                await operation.Execute(this);

                // TODO: What if a chunk is set to be unloaded here, but before the chunkOperations.Any() ends, there are other operations that were added?
                // If there aren't any other operations that need that chunk, add it to the list of chunks to unload
                if (ShouldUnloadChunkAtPosition(operation.ChunkPosition))
                    chunksToUnload.Add(chunks[operation.ChunkPosition]);
            }

            if (chunksToUnload.Count > 0)
                // Unload all the chunks that are no longer needed
                await UnloadChunks(chunksToUnload, worldInfo);

            isProcessingOperations = false;
        }

        /// <summary>
        /// Get whether or not the Chunk in the given operation needs to be unloaded.
        /// This will check if the chunk is needed for future operations OR if it is needed by a chunk loader.
        /// </summary>
        /// <param name="chunkPosition">The position for which to check the chunk.</param>
        private bool ShouldUnloadChunkAtPosition(Vector3Int chunkPosition)
        {
            // Check that no other operations need that chunk (aka ALL of them do NOT have that position)
            if (chunkOperations.All(chunkOperation => chunkOperation.ChunkPosition != chunkPosition))
                // Check that the chunk isn't requested to remain loaded (by a chunk loader)
                if (!chunkLoadingRequests.ContainsKey(chunkPosition))
                    // Also make sure that the chunk actually exists, maybe the operation is wrong...
                    if (chunks.ContainsKey(chunkPosition))
                        return true;

            return false;
        }

        /// <summary>
        ///     Add the given operation to the ChunkOperations Queue.
        /// </summary>
        /// <param name="operation">The operation to add</param>
        /// <returns>Returns the ExecuteTask to await for</returns>
        private Task AddChunkOperation(IChunkOperation operation)
        {
            chunkOperations.Enqueue(operation);

            return operation.ExecuteCompletionSource.Task;
        }

        #endregion

        #region Chunk Accessor

        /// <summary>
        ///     Get, Load, or Create the chunk at the given position.
        /// </summary>
        /// <param name="chunkPosition">The ChunkPosition at which to get the chunk from.</param>
        public async Task<Chunk> GetOrCreateChunk(Vector3Int chunkPosition)
        {
            return (await GetOrCreateChunk(new List<Vector3Int> {chunkPosition}))[0];
        }

        /// <summary>
        ///     Get, Load, or Create all the chunks at the given position.
        /// </summary>
        /// <param name="chunkPositions">The ChunkPositions at which to get the chunks from.</param>
        public async Task<List<Chunk>> GetOrCreateChunk(List<Vector3Int> chunkPositions)
        {
            // This is the final chunk list
            List<Chunk> chunkList = new List<Chunk>();

            // Keep track of all the chunks that need to be loaded (and aren't currently loading)
            List<Vector3Int> chunksToLoad = new List<Vector3Int>();

            // Await for the chunks that are already loading and add them to the chunkList
            foreach (Vector3Int chunkPosition in chunkPositions)
                // If that chunk is already loading
                if (chunkLoadingTasks.ContainsKey(chunkPosition))
                {
                    // Wait for it to load
                    List<Chunk> newChunks = await chunkLoadingTasks[chunkPosition];
                    // Once done loading, add that chunk to the list
                    foreach (Chunk newChunk in newChunks)
                        if (newChunk.position == chunkPosition)
                            chunkList.Add(newChunk);
                }
                // If if isn't currently loading, add it to a list of chunks to load
                else
                {
                    chunksToLoad.Add(chunkPosition);
                }

            // If there are some chunks that need to be loaded
            if (chunksToLoad.Count > 0)
            {
                // Prepare a task for all the chunks that weren't already loading
                Task<List<Chunk>> loadingTask = _GetOrCreateChunks(chunksToLoad);
                // Register this task for each chunk so that other tasks can await it
                foreach (Vector3Int chunkToLoad in chunksToLoad) chunkLoadingTasks.Add(chunkToLoad, loadingTask);

                // Wait for chunks to load, and add them to the list
                List<Chunk> loadedChunks = await loadingTask;

                // Add those chunks to the final list and remove their task
                foreach (Chunk loadedChunk in loadedChunks)
                {
                    // Add the newly loaded chunks to the final list
                    chunkList.Add(loadedChunk);
                    chunkLoadingTasks.Remove(loadedChunk.position);
                }
            }

            return chunkList;
        }

        /// <summary>
        ///     <para>DO NOT USE</para>
        ///     <para>
        ///         Like GetOrCreateChunk but without checking if a chunk is already loading (this would load the chunk a second
        ///         time).
        ///         This is used internally by GetOrCreateChunk.
        ///     </para>
        /// </summary>
        private async Task<List<Chunk>> _GetOrCreateChunks(List<Vector3Int> chunkPositions)
        {
            List<Chunk> chunkList = new List<Chunk>();

            List<Vector3Int> positionsToLoad = new List<Vector3Int>();

            foreach (Vector3Int chunkPosition in chunkPositions)
                // If it already exists, return it
                if (chunks.ContainsKey(chunkPosition))
                    chunkList.Add(chunks[chunkPosition]);
                // Otherwise, add it to the list to load
                else
                    positionsToLoad.Add(chunkPosition);

            // If there are no extra chunks to load, return early.
            if (positionsToLoad.Count == 0) return chunkList;

            // Prepare to load the chunks
            await LoadChunks(positionsToLoad);

            List<Vector3Int> positionsToCreate = new List<Vector3Int>();

            // Verify that every chunk was loaded correctly
            foreach (Vector3Int positionToLoad in positionsToLoad)
                // If the chunk was successfully loaded, add it
                if (chunks.ContainsKey(positionToLoad))
                    chunkList.Add(chunks[positionToLoad]);
                else
                    positionsToCreate.Add(positionToLoad);

            // If there are no extra chunks to create, return early.
            if (positionsToCreate.Count == 0) return chunkList;

            // Create the chunks that couldn't be loaded
            CreateChunks(positionsToCreate);

            foreach (Vector3Int positionToCreate in positionsToCreate)
            {
                if (!chunks.ContainsKey(positionToCreate))
                    Debug.LogError($"Unable to load or create Chunk at {positionToCreate}");

                chunkList.Add(chunks[positionToCreate]);
            }

            return chunkList;
        }

        /// <summary>
        ///     Get all chunks that are dirty.
        /// </summary>
        public List<Chunk> GetDirtyChunks()
        {
            // TODO: Remove LINQ? Or maybe make all of the World use Linq (which would be much easier).
            // Find all the chunks that have IsDirty == true
            return chunks.Values.ToList().FindAll(x => x.IsDirty);
        }

        #endregion

        #region Chunk Loading Requests

        /// <summary>
        ///     Request the loading of multiple chunks.
        ///     This will keep the chunks loaded until they are requested to be unloaded.
        /// </summary>
        /// <param name="chunkPositions">The positions at which to load the chunks from.</param>
        /// <param name="requester">The ChunkLoader that requested this.</param>
        public async Task RequestChunkLoading(List<Vector3Int> chunkPositions, ChunkLoader requester)
        {
            // If no positions passed, ignore
            if (chunkPositions == null || chunkPositions.Count == 0) return;

            foreach (Vector3Int chunkPosition in chunkPositions)
                // Add it the requester to the list
                if (chunkLoadingRequests.ContainsKey(chunkPosition))
                    chunkLoadingRequests[chunkPosition].Add(requester);
                else
                    chunkLoadingRequests.Add(chunkPosition, new List<ChunkLoader> {requester});

            // TODO: Should this load the chunk directly? Or wait for its turn in as a new LoadChunkOperation (Also same thing for Unloading)
            // Actually load the chunk into the chunks list
            await GetOrCreateChunk(chunkPositions);
        }

        /// <summary>
        ///     Request the unloading of multiple chunks.
        ///     This will not fail if the chunks weren't loaded in the first place.
        /// </summary>
        /// <param name="chunkPositions">The positions at which to unload the chunks from.</param>
        /// <param name="requester">The ChunkLoader that requested this.</param>
        public async Task RequestChunkUnloading(List<Vector3Int> chunkPositions, ChunkLoader requester)
        {
            // If no positions passed, ignore
            if (chunkPositions == null || chunkPositions.Count == 0) return;

            List<Chunk> chunksToUnload = new List<Chunk>();

            foreach (Vector3Int chunkPosition in chunkPositions)
            {
                // Check that this request exists and that the chunk loader has actually requested it
                if (chunkLoadingRequests.ContainsKey(chunkPosition) &&
                    chunkLoadingRequests[chunkPosition].Contains(requester))
                {
                    // Remove the request
                    chunkLoadingRequests.Remove(chunkPosition);

                    // Unload the chunk if it's not used by anything else
                    if (ShouldUnloadChunkAtPosition(chunkPosition))
                        chunksToUnload.Add(chunks[chunkPosition]);
                }
            }

            if (chunksToUnload.Count > 0)
                // Unload all the chunks that are no longer needed
                await UnloadChunks(chunksToUnload, worldInfo);
        }

        #endregion

        #region Chunk Loading

        /// <summary>
        ///     Used internally to load the Chunks at the given chunk positions.
        ///     This will not check for pre-loaded chunks or currently loading ones.
        /// </summary>
        /// <param name="chunkPositions">The positions at which to load the chunks from.</param>
        private async Task LoadChunks(List<Vector3Int> chunkPositions)
        {
            // Ignore if empty list
            if (chunkPositions == null || chunkPositions.Count == 0) return;

            Debug.Log("Loading chunk at " + chunkPositions.ToStringFlattened());

            // Keep every chunk that actually exists in the file system
            List<Vector3Int> chunksToLoad = WorldManager.GetExistingChunks(chunkPositions, worldInfo);
            
            if (chunksToLoad.Count > 0)
            {
                // Load chunks from file system
                List<Chunk> newChunks = await WorldManager.LoadChunk(chunksToLoad, worldInfo);

                // Loop through each to set them up
                foreach (Chunk newChunk in newChunks)
                {
                    // Ignore null chunks
                    if (newChunk == null) continue;

                    // Add the chunk and set it up
                    chunks.Add(newChunk.position, newChunk);
                    newChunk.Setup(chunkSize, tilemap);
                }
            }

            Debug.Log("Finished loading chunk at " + chunkPositions.ToStringFlattened());
        }


        /// <summary>
        ///     Used internally to unload the Chunks at the given chunk positions.
        ///     This will save dirt chunks to the file system.
        /// </summary>
        /// <param name="chunksToUnload">The positions at which to load the chunks from.</param>
        /// <param name="info">The WorldInfo object to find where to save the dirty chunks.</param>
        private async Task UnloadChunks(List<Chunk> chunksToUnload, WorldInfo info)
        {
            List<Chunk> chunksToSave = new List<Chunk>();

            Debug.Log("Unloading chunk at " + chunksToUnload.ToStringFlattened());

            foreach (Chunk chunk in chunksToUnload)
            {
                // Remove it from the list
                chunks.Remove(chunk.position);

                // If the chunk is dirty, add it to the list of chunks to save
                if (chunk.IsDirty)
                    chunksToSave.Add(chunk);
            }

            // Save chunks if any
            if (chunksToSave.Count > 0) await WorldManager.SaveChunks(chunksToSave, info);

            // Clear all chunks (once done saving those that are important)
            foreach (Chunk chunk in chunksToUnload) chunk.Clear(false);

            Debug.Log("Done unloading chunks at " + chunksToUnload.ToStringFlattened());
        }

        /// <summary>
        ///     Create fresh new chunks at the given positions.
        /// </summary>
        /// <param name="chunkPositions">The positions at which to create the chunk from.</param>
        private void CreateChunks(List<Vector3Int> chunkPositions)
        {
            foreach (Vector3Int chunkPosition in chunkPositions)
                chunks.Add(chunkPosition, new Chunk(chunkPosition, chunkSize, tilemap));
        }

        #endregion

        #region ChunkData Accessor

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
            // Await the operation and return it's Result value
            IsEmptyTileOperation operation = new IsEmptyTileOperation(tilePosition, this);
            await AddChunkOperation(operation);
            return operation.Result;
        }

        /// <summary>
        ///     Get the tile at the given position.
        /// </summary>
        /// <param name="tilePosition">The position to get the tile at.</param>
        public async Task<BasicTile> GetTileAt(Vector3Int tilePosition)
        {
            // Await the operation and return it's Result value
            GetTileOperation operation = new GetTileOperation(tilePosition, this);
            await AddChunkOperation(operation);
            return operation.Result;
        }

        /// <summary>
        ///     Set a tile in the world using a BasicTile.
        /// </summary>
        /// <param name="tilePosition">The position to set the tile at.</param>
        /// <param name="id">The id of the tile to set.</param>
        public async Task SetTileAt(Vector3Int tilePosition, string id)
        {
            // Await the operation
            SetTileOperation operation = new SetTileOperation(tilePosition, id, this);
            await AddChunkOperation(operation);
        }

        /// <summary>
        ///     Set a tile in the world using a BasicTile.
        /// </summary>
        /// <param name="tilePosition">The position to set the tile at.</param>
        /// <param name="tile">The BasicTile object to set.</param>
        public async Task SetTileAt(Vector3Int tilePosition, BasicTile tile)
        {
            // Await the operation
            SetTileOperation operation = new SetTileOperation(tilePosition, tile, this);
            await AddChunkOperation(operation);
        }

        /// <summary>
        ///     Remove the tile at the given position.
        /// </summary>
        /// <param name="tilePosition">The position to remove the tile at.</param>
        public async Task RemoveTileAt(Vector3Int tilePosition)
        {
            // Await the operation
            RemoveTileOperation operation = new RemoveTileOperation(tilePosition, this);
            await AddChunkOperation(operation);
        }

        #region Internal Version

        // This is the internal version of IsEmptyTileAt, it does not work using Operations
        public async Task<bool> _IsEmptyTileAt(Vector3Int tilePosition)
        {
            // If the Tile doesn't exist OR its value is empty
            return await _GetTileAt(tilePosition) == null ||
                   string.IsNullOrEmpty((await _GetTileAt(tilePosition)).Id);
        }

        // This is the internal version of GetTileAt, it does not work using Operations
        public async Task<BasicTile> _GetTileAt(Vector3Int tilePosition)
        {
            // Get a chunk at the corresponding Chunk position for the given tile position
            Vector3Int chunkPosition = TileToChunkPosition(tilePosition);
            Chunk chunk = await GetOrCreateChunk(chunkPosition);
            return chunk.GetTileAt(tilePosition);
        }

        // This is the internal version of SetTileAt, it does not work using Operations
        public async Task _SetTileAt(Vector3Int tilePosition, string id)
        {
            // Get a chunk at the corresponding Chunk position for the given tile position
            Vector3Int chunkPosition = TileToChunkPosition(tilePosition);
            Chunk chunk = await GetOrCreateChunk(chunkPosition);

            chunk.SetTileAt(tilePosition, id);
        }

        // This is the internal version of GetTileAt, it does not work using Operations
        public async Task _SetTileAt(Vector3Int tilePosition, BasicTile tile)
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
        public async Task _RemoveTileAt(Vector3Int tilePosition)
        {
            // TODO: Might want to make it so that removing and getting a tile doesn't CREATE a new chunk if none exists (and if there is no loadable chunk)
            Vector3Int chunkPosition = TileToChunkPosition(tilePosition);
            Chunk chunk = await GetOrCreateChunk(chunkPosition);

            chunk.RemoveTileAt(tilePosition);
        }

        #endregion

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