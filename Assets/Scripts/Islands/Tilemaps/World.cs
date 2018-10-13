using System.Collections;
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

        [SerializeField] private string worldName;
        [SerializeField] private Vector3Int chunkSize;
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private GridInformation gridInfo;
        [SerializeField] private WorldGeneration worldGen;

        private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Debug.LogError("Cannot have more than one World instance.");
            }
        }

        private void Start()
        {
            worldGen.Generate(this);
            WorldManager.Save(this);
        }

        /// <summary>
        /// Get the GridInformation to store TileData
        /// </summary>
        /// <returns>GridInformation component</returns>
        public GridInformation GetGridInformation()
        {
            return gridInfo;
        }

        /// <summary>
        /// Get the World Id.
        /// This is a FileSystem-safe id in snake_case.
        /// </summary>
        /// <returns>The world's id.</returns>
        public string GetId()
        {
            return Utilities.RemoveIllegalFileCharacters(worldName.ToLower().Replace(" ", "_"));
        }

        public override string ToString()
        {
            return $"World {worldName}";
        }

        #region Regions

        /// <summary>
        /// Get a dictionary of every chunk within the given bounds.
        /// </summary>
        /// <param name="bounds">Bounds in Chunk Coordinates.</param>
        /// <returns>A Position-Chunk dictionary of the region.</returns>
        public Dictionary<Vector3Int, Chunk> GetRegion(BoundsInt bounds)
        {
            Dictionary<Vector3Int, Chunk> data = new Dictionary<Vector3Int, Chunk>();
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    for (int z = bounds.zMin; z < bounds.zMax; z++)
                    {
                        // Get the chunk at the given position
                        Vector3Int chunkPosition = new Vector3Int(x, y, z);
                        Chunk chunk = GetOrCreateChunk(chunkPosition);

                        // If the chunk isn't empty, add it to the list
                        if (!chunk.IsEmpty())
                        {
                            data.Add(chunkPosition, chunk);
                        }
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Find all the Dirty Chunks
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
        /// Convert the given Tile Position to it's corresponding Chunk's position
        /// </summary>
        /// <param name="position">The position of the tile</param>
        /// <returns>The position of the chunk</returns>
        public Vector3Int GetChunkPositionForTile(Vector3Int position)
        {
            // Chunk Position Formula
            // 1. (tilePosition.x / (chunkSize + 1)) -> Separate every chunk into "slices" of 16 (doing +1 so it wraps at 17, not 16)
            // 2. (tilePosition.x < 0 ? -1 : 0) -> For negative positions, go 1 lower (because -5 / 16 = 0, but it should be chunk -1)
            return new Vector3Int(
                (position.x / (chunkSize.x + 1)) + (position.x < 0 ? -1 : 0),
                (position.y / (chunkSize.y + 1)) + (position.y < 0 ? -1 : 0),
                (position.z / (chunkSize.z + 1)) + (position.z < 0 ? -1 : 0)
            );
        }

        /// <summary>
        /// Get, Load, or Create a chunk at the given position.
        /// </summary>
        /// <param name="position">The position of the chunk</param>
        /// <returns>The Chunk</returns>
        private Chunk GetOrCreateChunk(Vector3Int position)
        {
            // If it doesn't exist, create a new one
            if (!chunks.ContainsKey(position))
            {
                // TODO: Check that it doesn't exist in FileSystem, if not, create empty, if it does, deserialize
                chunks.Add(position, new Chunk(position, chunkSize, tilemap));
            }

            return chunks[position];
        }

        /// <summary>
        /// Get the tile at the given position.
        /// </summary>
        /// <param name="position">The position to get the tile at.</param>
        /// <returns>The BasicTile object.</returns>
        public BasicTile GetTile(Vector3Int position)
        {
            // Get a chunk at the corresponding Chunk position for the given tile position
            Chunk chunk = GetOrCreateChunk(GetChunkPositionForTile(position));
            return chunk.GetTile(position);
        }

        /// <summary>
        /// Set a tile in the world using an id.
        /// </summary>
        /// <param name="position">The position to set the tile at.</param>
        /// <param name="id">The id of tile to set.</param>
        public void SetTile(Vector3Int position, string id)
        {
            // Get a chunk at the corresponding Chunk position for the given tile position
            Chunk chunk = GetOrCreateChunk(GetChunkPositionForTile(position));

            chunk.SetTile(position, id);
        }

        /// <summary>
        /// Set a tile in the world using a BasicTile.
        /// </summary>
        /// <param name="position">The position to set the tile at.</param>
        /// <param name="tile">The BasicTile object to set.</param>
        public void SetTile(Vector3Int position, BasicTile tile)
        {
            // Get a chunk at the corresponding Chunk position for the given tile position
            Chunk chunk = GetOrCreateChunk(GetChunkPositionForTile(position));

            chunk.SetTile(position, tile);
        }

        #endregion
    }
}