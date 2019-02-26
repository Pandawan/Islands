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

        // How long should a chunk be kept alive if it wasn't requested (in seconds)
        [SerializeField] private float chunkKeepAliveDelay = 60;

        // TODO: This could be coupled to the Chunk itself by adding an extra public field and using chunks Dictionary directly.
        // Keeps track of the last "loading" request for each chunk
        private readonly Dictionary<Vector3Int, float> chunkRequestTime = new Dictionary<Vector3Int, float>();

        // Keeps track of every chunk
        private readonly Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

        public event WorldEvent GenerationEvent;

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

        #region World Lifecycle

        private void DiscardOldChunks()
        {
            List<Chunk> chunksToSave = new List<Chunk>();
            foreach (Vector3Int key in chunkRequestTime.Keys.ToList())
            {
                chunkRequestTime[key] += Time.deltaTime;

                if (chunkRequestTime[key] >= chunkKeepAliveDelay)
                {
                    // Keep a list of chunks to be saved (only Dirty)
                    if (chunks[key].IsDirty)
                        chunksToSave.Add(chunks[key]);
                    else
                        chunks[key].Clear(false);

                    // Remove all chunks from the current chunks dictionary
                    // Those that are to be saved still exist in the chunksToSave list
                    chunks.Remove(key);
                    chunkRequestTime.Remove(key);
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

        #endregion

        #region Chunk Loading & Saving

        /// <summary>
        ///     Request the loading of the chunk at the given position.
        /// </summary>
        /// <param name="chunkPosition">The chunk position</param>
        public void RequestChunkLoading(Vector3Int chunkPosition)
        {
            // Simply calling GetOrCreate works
            GetOrCreateChunk(chunkPosition);
        }


        /// <summary>
        ///     Request the loading of the chunk within the given bounds.
        /// </summary>
        /// <param name="chunkBounds">The bounds within wich to load the chunks.</param>
        public void RequestChunkLoading(BoundsInt chunkBounds)
        {
            // This will call GetOrCreate on each chunk
            GetChunksInBounds(chunkBounds);
        }

        public void RequestChunkUnLoading(Vector3Int chunkPosition, ChunkLoader requester)
        {
            if (!chunkLoadRequests[chunkPosition].Exists(loader => loader == requester))
            {
                Debug.LogError($"Given {requester} never requested for chunk loading at {chunkPosition}.");
                return;
            }

            chunkLoadRequests[chunkPosition].Remove(requester);

            // If that Chunk is no longer needed
            if (chunkLoadRequests[chunkPosition].Count == 0)
            {
                // Save the Chunk if Dirty
                if (chunks[chunkPosition].IsDirty)
                {
                    WorldManager.SaveChunks(new List<Chunk> { chunks[chunkPosition] }, worldInfo);
                }

                // Clear the chunk from the Tilemap & its data
                chunks[chunkPosition].Clear(false);
                // Remove that chunkPosition from all records/dictionaries
                chunks.Remove(chunkPosition);
                chunkLoadRequests.Remove(chunkPosition);
            }
        }

        /// <summary>
        ///     Get a dictionary of every chunk within the given bounds.
        /// </summary>
        /// <param name="chunkBounds">Bounds in Chunk Coordinates.</param>
        /// <returns>A Position-Chunk dictionary of the region.</returns>
        public Dictionary<Vector3Int, Chunk> GetChunksInBounds(BoundsInt chunkBounds)
        {
            Dictionary<Vector3Int, Chunk> data = new Dictionary<Vector3Int, Chunk>();
            foreach (Vector3Int chunkPosition in chunkBounds.allPositionsWithin)
            {
                // Get the chunk at the given position
                Chunk chunk = GetOrCreateChunk(chunkPosition);

                // If the chunk isn't empty, add it to the list
                if (!chunk.IsEmpty()) data.Add(chunkPosition, chunk);
            }

            return data;
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
                position.x / (position.x < 0 ? chunkSize.x + 1 : chunkSize.x) + (position.x < 0 ? -1 : 0),
                position.y / (position.y < 0 ? chunkSize.y + 1 : chunkSize.y) + (position.y < 0 ? -1 : 0),
                position.z / (position.z < 0 ? chunkSize.z + 1 : chunkSize.z) + (position.z < 0 ? -1 : 0)
            );
        }


        /// <summary>
        ///     Convert the given Tile Position to its corresponding Chunk's position
        /// </summary>
        /// <param name="position">The position of the tile</param>
        /// <returns>The position of the chunk</returns>
        public Vector3Int GetChunkPositionForTileCeil(Vector3Int position)
        {
            return new Vector3Int(
                Mathf.CeilToInt((float) position.x / (position.x < 0 ? chunkSize.x + 1 : chunkSize.x) +
                                (position.x < 0 ? -1 : 0)),
                Mathf.CeilToInt((float) position.y / (position.y < 0 ? chunkSize.y + 1 : chunkSize.y) +
                                (position.y < 0 ? -1 : 0)),
                Mathf.CeilToInt((float) position.z / (position.z < 0 ? chunkSize.z + 1 : chunkSize.z) +
                                (position.z < 0 ? -1 : 0))
            );
        }

        /// <summary>
        ///     Get, Load, or Create a chunk at the given position.
        /// </summary>
        /// <param name="chunkPosition">The chunk position.</param>
        private Chunk GetOrCreateChunk(Vector3Int chunkPosition)
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
                        chunk[0].Load();
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

            if (chunkRequestTime.ContainsKey(chunkPosition))
                chunkRequestTime[chunkPosition] = 0;
            else chunkRequestTime.Add(chunkPosition, 0);

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

            Debug.Log($"Setting tile {tilePosition} in chunk {chunk.position}");

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
        ///     Remove the tile at the given position.
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

        public WorldInfo(string name)
        {
            this.name = name;
        }
    }
}