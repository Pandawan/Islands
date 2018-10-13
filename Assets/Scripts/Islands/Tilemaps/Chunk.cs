using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pandawan.Islands.Tilemaps.Tiles;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pandawan.Islands.Tilemaps
{
    [Serializable]
    public class Chunk
    {
        // Actual saved tiles
        [SerializeField] private string[] tiles;
        // private GridInformation infos { get; set; }

        [SerializeField] public Vector3Int position;

        // Size of a chunk
        [NonSerialized] private Vector3Int size;

        // Keep a reference to the tilemap
        [NonSerialized] private Tilemap tilemap;


        public Chunk(Vector3Int position, Vector3Int size, Tilemap tilemap)
        {
            this.position = position;
            this.size = size;
            tiles = new string[size.x * size.y * size.z];
            this.tilemap = tilemap;
            IsDirty = false;
        }

        public Chunk(Vector3Int position, Vector3Int size, Tilemap tilemap, string[] tiles)
        {
            this.position = position;
            this.size = size;
            this.tiles = tiles;
            this.tilemap = tilemap;
            IsDirty = false;
        }

        public Chunk(BoundsInt bounds, Tilemap tilemap, GridInformation gridInfo)
        {
            tiles = new string[size.x * size.y * size.z];

            // Loop through every position in the bounds and add the tile if not empty
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    for (int z = bounds.zMin; z < bounds.zMax; z++)
                    {
                        // Get the tile at this local position
                        Vector3Int tilePosition = new Vector3Int(x, y, z);
                        string tile = (tilemap.GetTile(tilePosition) as BasicTile)?.TileName ?? "";
                        // Add it to the tiles list
                        tiles[PositionToIndex(tilePosition)] = tile;
                    }
                }
            }

            // Apply the position
            position = bounds.position;
        }

        // Whether or not this Chunk is different from the saved one
        public bool IsDirty { get; protected set; }

        public void Load()
        {
            // TODO: Check that it works AND maybe convert this so actual loading is external in WorldGeneration
            // Load every tile
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                for (int y = 0; y < tiles.GetLength(1); y++)
                {
                    for (int z = 0; z < tiles.GetLength(2); z++)
                    {
                        Vector3Int tilePosition = LocalToGlobalPosition(new Vector3Int(x, y, z));
                        string tileId = tiles[PositionToIndex(tilePosition)];
                        tilemap.SetTile(tilePosition, TileDB.instance.GetTile(tileId));
                    }
                }
            }
        }

        /// <summary>
        /// Get the Chunk Id.
        /// This is a FileSystem-safe id in snake_case.
        /// </summary>
        /// <returns>The chunk's id.</returns>
        public string GetId()
        {
            return $"chunk_{position.x}_{position.y}_{position.z}";
        }

        public override string ToString()
        {
            return $"Chunk {position.ToString()}";
        }

        #region Utilities

        /// <summary>
        /// Convert a Vector3 position into a 1D array index.
        /// </summary>
        /// <param name="tilePosition">The position of the tile.</param>
        /// <returns>The 1D array index</returns>
        public int PositionToIndex(Vector3Int tilePosition)
        {
            return (tilePosition.z + size.z * (tilePosition.x * size.x + tilePosition.y));
        }

        /// <summary>
        /// Convert a 1D array index into a Vector3D Position.
        /// </summary>
        /// <param name="index">The array index.</param>
        /// <returns>The local Vector3 position.</returns>
        public Vector3Int IndexToPosition(int index)
        {
            int z = index % size.z;
            int y = (index / size.z) % size.y;
            int x = index / (size.y * size.z);
            return new Vector3Int(x, y, z);
        }

        /// <summary>
        /// Convert a Global/World Position to a Local/Chunk Position
        /// </summary>
        /// <param name="globalPosition">The Global Position to convert.</param>
        /// <returns>The converted Local Position.</returns>
        public Vector3Int GlobalToLocalPosition(Vector3Int globalPosition)
        {
            // Formula to convert to local position AND invert negative tile positions (arrays can't go below 0)
            return new Vector3Int((globalPosition.x % size.x + size.x) % size.x,
                (globalPosition.y % size.y + size.y) % size.y, (globalPosition.z % size.z + size.z) % size.z);
        }

        /// <summary>
        /// Convert a Local/Chunk Position to a Global/World Position
        /// </summary>
        /// <param name="localPosition">The Global Position to convert.</param>
        /// <returns>The converted Local Position.</returns>
        public Vector3Int LocalToGlobalPosition(Vector3Int localPosition)
        {
            return new Vector3Int(localPosition.x + position.x * size.x, localPosition.y + position.y * size.y,
                localPosition.z + position.z * size.z);
        }

        #endregion

        #region Tile Abstraction

        /// <summary>
        /// Whether or not the given Chunk is empty.
        /// </summary>
        /// <returns>True if empty.</returns>
        public bool IsEmpty()
        {
            return tiles.Length == 0;
        }

        /// <summary>
        /// Whether or not the given Tile Position is valid in this chunk.
        /// </summary>
        /// <param name="tilePosition">The position of the tile to check.</param>
        /// <returns>True if it is valid in this chunk.</returns>
        private bool IsValidPosition(Vector3Int tilePosition)
        {
            // Use the World's formula for chunk positions and check that they match
            return (World.instance.GetChunkPositionForTile(tilePosition) == position);
        }

        /// <summary>
        /// Get the BasicTile object at the given position.
        /// </summary>
        /// <param name="tilePosition">The position to get the tile at.</param>
        /// <returns>The BasicTile object.</returns>
        public BasicTile GetTile(Vector3Int tilePosition)
        {
            if (!IsValidPosition(tilePosition))
            {
                Debug.LogError($"Position {tilePosition} is not valid for Chunk at {position}");
                return null;
            }

            return tilemap.GetTile(tilePosition) as BasicTile;
        }

        /// <summary>
        /// Get the TilePair object at the given position.
        /// WARNING: You almost never want this!
        /// </summary>
        /// <param name="tilePosition">The position to get the TilePair at.</param>
        /// <returns>The TilePair object.</returns>
        public string GetTileId(Vector3Int tilePosition)
        {
            // TODO: Probably want to remove this method because it should only be used internally...
            if (!IsValidPosition(tilePosition))
            {
                Debug.LogError($"Position {tilePosition} is not valid for Chunk at {position}");
                return null;
            }

            Vector3Int localPosition = GlobalToLocalPosition(tilePosition);

            return tiles[PositionToIndex(localPosition)];
        }

        /// <summary>
        /// Set a tile at the given position using an id.
        /// </summary>
        /// <param name="tilePosition">The position at which to set the tile.</param>
        /// <param name="tileId">The id of the tile to set.</param>
        public void SetTile(Vector3Int tilePosition, string tileId)
        {
            if (!IsValidPosition(tilePosition))
            {
                Debug.LogError($"Position {tilePosition} is not valid for Chunk at {position}");
                return;
            }

            // If it's an empty id, trying to remove the tile
            if (string.IsNullOrEmpty(tileId))
            {
                RemoveTile(tilePosition);
                return;
            }

            // Get the corresponding BasicTile and call SetTile with the BasicTile
            BasicTile tile = TileDB.instance.GetTile(tileId);
            SetTile(tilePosition, tile);
        }

        /// <summary>
        /// Set a tile at the given position.
        /// </summary>
        /// <param name="tilePosition">The position at which to set the tile.</param>
        /// <param name="tile">The Tile object to set</param>
        public void SetTile(Vector3Int tilePosition, BasicTile tile)
        {
            if (!IsValidPosition(tilePosition))
            {
                Debug.LogError($"Position {tilePosition} is not valid for Chunk at {position}");
                return;
            }

            // If the tile is null, trying to remove the tile
            if (tile == null)
            {
                RemoveTile(tilePosition);
                return;
            }

            // Set the new tile in the Chunk's tiles list
            Vector3Int localPosition = GlobalToLocalPosition(tilePosition);
            tiles[PositionToIndex(localPosition)] = tile.Id;

            // Set the new tile in the Tilemap
            tilemap.SetTile(tilePosition, tile);

            // Set the Chunk as Dirty
            IsDirty = true;

            // TODO: Don't forget to reset the GridInformation
        }

        /// <summary>
        /// Remove the tile at the given position.
        /// </summary>
        /// <param name="tilePosition">The position at which to remove the tile.</param>
        public void RemoveTile(Vector3Int tilePosition)
        {
            if (!IsValidPosition(tilePosition))
            {
                Debug.LogError($"Position {tilePosition} is not valid for Chunk at {position}");
                return;
            }

            Vector3Int localPosition = GlobalToLocalPosition(tilePosition);
            tiles[PositionToIndex(localPosition)] = "";

            IsDirty = true;

            // TODO: Reset GridInformation
        }

        #endregion
    }
}