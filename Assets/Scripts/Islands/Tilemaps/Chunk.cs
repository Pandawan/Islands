using System;
using System.Collections.Generic;
using System.Linq;
using Pandawan.Islands.Tilemaps.Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pandawan.Islands.Tilemaps
{
    [Serializable]
    public class Chunk
    {
        // Actual saved tiles
        [SerializeField] private string[] tiles;

        // Used to save anything dynamic/runtime in the chunk
        [SerializeField] private ChunkData chunkData;

        // The Chunk's actual position
        [SerializeField] public Vector3Int position;

        // Size of a chunk
        [NonSerialized] private Vector3Int size;

        // Keep a reference to the tilemap
        [NonSerialized] private Tilemap Tilemap;

        // Whether or not this Chunk is different from the saved one
        public bool IsDirty { get; protected set; }

        #region Constructors & Setup

        public Chunk(Vector3Int position, Vector3Int size, Tilemap tilemap)
        {
            tiles = new string[size.x * size.y * size.z];
            chunkData = new ChunkData(this, new BoundsInt(position, size));
            this.position = position;
            this.size = size;
            Tilemap = tilemap;
            IsDirty = false;
        }

        // TODO: Use this to import a tilemap instead of World.SetTile every time?
        public Chunk(BoundsInt bounds, Tilemap tilemap, ChunkData chunkData)
        {
            tiles = new string[size.x * size.y * size.z];

            // Loop through every position in the bounds and add the tile if not empty
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            for (int z = bounds.zMin; z < bounds.zMax; z++)
            {
                // Get the tile at this local position
                Vector3Int tilePosition = new Vector3Int(x, y, z);
                string tile = (tilemap.GetTile(tilePosition) as BasicTile)?.TileName ?? "";
                // Add it to the tiles list
                tiles[PositionToIndex(tilePosition)] = tile;
            }

            // Apply the position
            position = bounds.position;
            size = bounds.size;
            Tilemap = tilemap;
            this.chunkData = chunkData;
            IsDirty = false;
        }


        /// <summary>
        ///     Setup private fields if Chunk was created through serialization.
        /// </summary>
        /// <param name="newSize">The size of the chunk.</param>
        /// <param name="newTilemap">The tilemap of the world.</param>
        public void Setup(Vector3Int newSize, Tilemap newTilemap)
        {
            // Setup variables from defaults
            size = newSize;
            Tilemap = newTilemap;
            // If tiles array doesn't exist, create it 
            if (tiles == null) tiles = new string[size.x * size.y * size.z];
            // If ChunkData doesn't exist, create it
            if (chunkData == null) chunkData = new ChunkData(this, new BoundsInt(position, size));
            // Otherwise, set it up (because some fields are not serialized
            else chunkData.Setup(this, new BoundsInt(position, size));
            IsDirty = false;

            if (tiles.Length > 0) LoadTilesToTilemap();
        }

        /// <summary>
        ///     Loads the current tiles array into the tilemap.
        /// </summary>
        private void LoadTilesToTilemap()
        {
            // Keep a list of tiles/positions to push to the tilemap later
            Dictionary<Vector3Int, TileBase> tilesToAdd = new Dictionary<Vector3Int, TileBase>();

            // Load every tile
            for (int index = 0; index < tiles.GetLength(0); index++)
            {
                Vector3Int tilePosition =
                    LocalChunkToTilePosition(IndexToPosition(index));
                string tileId = tiles[index];
                if (!string.IsNullOrEmpty(tileId)) tilesToAdd.Add(tilePosition, TileDB.instance.GetTile(tileId));
            }

            // Set all of the tiles that aren't empty in the tilemap
            Tilemap.SetTiles(tilesToAdd.Keys.ToArray(), tilesToAdd.Values.ToArray());
        }

        #endregion

        #region Getters

        /// <summary>
        ///     Get the ChunkData object.
        /// </summary>
        /// <returns></returns>
        public ChunkData GetChunkData()
        {
            return chunkData;
        }

        /// <summary>
        ///     Get the Chunk Id.
        ///     This is a FileSystem-safe id in snake_case.
        /// </summary>
        /// <returns>The chunk's id.</returns>
        public string GetId()
        {
            return GetIdForPosition(position);
        }

        public static string GetIdForPosition(Vector3Int position)
        {
            return $"chunk_{position.x}_{position.y}_{position.z}";
        }

        public override string ToString()
        {
            return $"Chunk {position.ToString()}";
        }

        #endregion

        #region Tile Abstraction

        /// <summary>
        ///     Get the BasicTile object at the given position.
        /// </summary>
        /// <param name="tilePosition">The position to get the tile at.</param>
        /// <returns>The BasicTile object.</returns>
        public BasicTile GetTileAt(Vector3Int tilePosition)
        {
            if (!IsValidPosition(tilePosition))
            {
                Debug.LogError($"Position {tilePosition} is not valid for Chunk at {position}");
                return null;
            }

            return Tilemap.GetTile(tilePosition) as BasicTile;
        }

        /// <summary>
        ///     Set a tile at the given position using an id.
        /// </summary>
        /// <param name="tilePosition">The position at which to set the tile.</param>
        /// <param name="tileId">The id of the tile to set.</param>
        public void SetTileAt(Vector3Int tilePosition, string tileId)
        {
            if (!IsValidPosition(tilePosition))
            {
                Debug.LogError($"Position {tilePosition} is not valid for Chunk at {position}");
                return;
            }

            // If it's an empty id, trying to remove the tile
            if (string.IsNullOrEmpty(tileId))
            {
                RemoveTileAt(tilePosition);
                return;
            }

            // Get the corresponding BasicTile and call SetTileAt with the BasicTile
            BasicTile tile = TileDB.instance.GetTile(tileId);
            SetTileAt(tilePosition, tile);
        }

        /// <summary>
        ///     Set a tile at the given position.
        /// </summary>
        /// <param name="tilePosition">The position at which to set the tile.</param>
        /// <param name="tile">The Tile object to set</param>
        public void SetTileAt(Vector3Int tilePosition, BasicTile tile)
        {
            if (!IsValidPosition(tilePosition))
            {
                Debug.LogError($"Position {tilePosition} is not valid for Chunk at {position}");
                return;
            }

            // If the tile is null, trying to remove the tile
            if (tile == null)
            {
                RemoveTileAt(tilePosition);
                return;
            }

            // Set the new tile in the Chunk's tiles list
            Vector3Int localPosition = TileToLocalChunkPosition(tilePosition);
            tiles[PositionToIndex(localPosition)] = tile.Id;

            // Set the new tile in the Tilemap
            Tilemap.SetTile(tilePosition, tile);

            // Set the Chunk as Dirty
            IsDirty = true;

            // Reset the ChunkData for this position
            chunkData.ErasePositionProperty(tilePosition);
        }

        /// <summary>
        ///     Remove the tile at the given position.
        /// </summary>
        /// <param name="tilePosition">The position at which to remove the tile.</param>
        public void RemoveTileAt(Vector3Int tilePosition)
        {
            if (!IsValidPosition(tilePosition))
            {
                Debug.LogError($"Position {tilePosition} is not valid for Chunk at {position}");
                return;
            }

            Vector3Int localPosition = TileToLocalChunkPosition(tilePosition);
            tiles[PositionToIndex(localPosition)] = "";

            IsDirty = true;

            // Remove the tile in the Tilemap
            Tilemap.SetTile(tilePosition, null);

            // Reset the ChunkData for this position
            chunkData.ErasePositionProperty(tilePosition);
        }

        #endregion

        #region Chunk Position Utilities

        /// <summary>
        ///     Convert a Vector3 position into a 1D array index.
        /// </summary>
        /// <param name="tilePosition">The position of the tile.</param>
        /// <returns>The 1D array index</returns>
        public int PositionToIndex(Vector3Int tilePosition)
        {
            return tilePosition.z + size.z * (tilePosition.x * size.x + tilePosition.y);
        }

        /// <summary>
        ///     Convert a 1D array index into a Vector3D Position.
        /// </summary>
        /// <param name="index">The array index.</param>
        /// <returns>The local Vector3 position.</returns>
        public Vector3Int IndexToPosition(int index)
        {
            int z = index % size.z;
            int y = index / size.z % size.y;
            int x = index / (size.y * size.z);
            return new Vector3Int(x, y, z);
        }

        /// <summary>
        ///     Whether or not the given Tile Position is valid in this chunk.
        /// </summary>
        /// <param name="tilePosition">The position of the tile to check.</param>
        /// <returns>True if it is valid in this chunk.</returns>
        public bool IsValidPosition(Vector3Int tilePosition)
        {
            // TODO: Figure out a way to decouple world from chunk (aka make it so Chunk and World both have easy access to chunkSize without one knowing about the other).
            // Use the World's formula for chunk positions and check that they match
            return World.instance.TileToChunkPosition(tilePosition) == position;
        }


        /// <summary>
        ///     Convert the given tile position to a chunk's local position (from 0 to chunkSize - 1).
        /// </summary>
        /// <param name="tilePosition">The tile position to convert from.</param>
        /// <returns>The resulting local chunk position.</returns>
        public Vector3Int TileToLocalChunkPosition(Vector3Int tilePosition)
        {
            // Formula to convert to local position AND invert negative tile positions (arrays can't go below 0)
            return new Vector3Int((tilePosition.x % size.x + size.x) % size.x,
                (tilePosition.y % size.y + size.y) % size.y, (tilePosition.z % size.z + size.z) % size.z);
        }

        /// <summary>
        ///     Convert the given local chunk position to a world tile position.
        /// </summary>
        /// <param name="localPosition">The local chunk position to convert from.</param>
        /// <returns>The resulting world tile position.</returns>
        public Vector3Int LocalChunkToTilePosition(Vector3Int localPosition)
        {
            return new Vector3Int(localPosition.x + position.x * size.x, localPosition.y + position.y * size.y,
                localPosition.z + position.z * size.z);
        }

        #endregion

        #region Chunk Utilities 

        /// <summary>
        ///     Whether or not the given Chunk is empty.
        /// </summary>
        /// <returns>True if empty.</returns>
        public bool IsEmpty()
        {
            return tiles.Length == 0;
        }

        /// <summary>
        ///     Clear the entire chunk, making it brand new
        /// </summary>
        /// <param name="setDirty">Whether or not to count these changes as "dirty"</param>
        public void Clear(bool setDirty)
        {
            BoundsInt bounds = new BoundsInt(position * size, size);
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            for (int z = bounds.zMin; z < bounds.zMax; z++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y, z);
                Tilemap.SetTile(tilePosition, null);
            }

            // Reset tile and chunkData
            tiles = null;
            chunkData.Reset();

            IsDirty = setDirty;
            // TODO: Should I Object.Destroy here?
        }

        #endregion
    }
}