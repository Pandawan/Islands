using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pandawan.Islands.Tilemaps
{
    [Serializable]
    public class Chunk
    {
        public Vector3Int position;

        // private GridInformation infos { get; set; }

        // Keep a reference to the tilemap
        private Tilemap tilemap;
        public List<TilePair> tiles;

        public Chunk(Vector3Int position, List<TilePair> tiles, Tilemap tilemap)
        {
            this.position = position;
            this.tiles = tiles;
            this.tilemap = tilemap;
        }

        public Chunk(BoundsInt bounds, Tilemap tilemap, GridInformation gridInfo)
        {
            tiles = new List<TilePair>();

            // Loop through every position in the bounds and add the tile if not empty
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    for (int z = bounds.zMin; z < bounds.zMax; z++)
                    {
                        // Check that there is a tile at this position
                        Vector3Int tilePosition = new Vector3Int(x, y, z);
                        string tile = tilemap.GetTile(tilePosition)?.name;
                        if (!string.IsNullOrEmpty(tile))
                        {
                            // Add it to the tiles list
                            tiles.Add(new TilePair(tilePosition, tile));
                        }
                    }
                }
            }

            // Apply the position
            position = bounds.position;
        }

        public void Load()
        {
            // Load every tile
            foreach (TilePair pair in tiles)
            {
                tilemap.SetTile(pair.position, TileDB.instance.GetTile(pair.id));
            }
        }

        [Serializable]
        public class TilePair
        {
            public string id;

            public Vector3Int position;
            // TODO: Perhaps store the GridInformation here when saving?

            public TilePair(Vector3Int position, string id)
            {
                this.position = position;
                this.id = id;
            }
        }


        #region Tile Abstraction

        /// <summary>
        /// Whether or not the given Chunk is empty.
        /// </summary>
        /// <returns>True if empty.</returns>
        public bool IsEmpty()
        {
            return tiles.Count == 0;
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
        /// Get the TileBase object at the given position.
        /// </summary>
        /// <param name="tilePosition">The position to get the tile at.</param>
        /// <returns>The TileBase object.</returns>
        public TileBase GetTile(Vector3Int tilePosition)
        {
            if (!IsValidPosition(tilePosition))
            {
                Debug.LogError($"Position {tilePosition} is not valid for Chunk at {position}");
                return null;
            }

            // TODO: Check that the position is in the correct chunk
            return tilemap.GetTile(tilePosition);
        }

        /// <summary>
        /// Get the TilePair object at the given position.
        /// WARNING: You almost never want this!
        /// </summary>
        /// <param name="tilePosition">The position to get the TilePair at.</param>
        /// <returns>The TilePair object.</returns>
        public TilePair GetTilePair(Vector3Int tilePosition)
        {
            // TODO: Probably want to remove this method because it should only be used internally...
            if (!IsValidPosition(tilePosition))
            {
                Debug.LogError($"Position {tilePosition} is not valid for Chunk at {position}");
                return null;
            }

            return tiles.Find(x => x.position == tilePosition);
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

            TileBase tile = TileDB.instance.GetTile(tileId);
            SetTile(tilePosition, tile);
        }

        /// <summary>
        /// Set a tile at the given position.
        /// </summary>
        /// <param name="tilePosition">The position at which to set the tile.</param>
        /// <param name="tile">The Tile object to set</param>
        public void SetTile(Vector3Int tilePosition, TileBase tile)
        {
            // TODO: Update system to custom Tile instead of TileBase so I can have both id AND name
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

            // Create a TilePair for the Chunk
            TilePair pair = new TilePair(tilePosition, tile.name);

            // Set the new tile in the Chunk's tiles list
            int index = tiles.FindIndex(x => x.position == tilePosition);
            if (index != -1)
            {
                tiles[index] = pair;
            }
            else
            {
                tiles.Add(pair);
            }

            // Set the new tile in the Tilemap
            tilemap.SetTile(tilePosition, tile);

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

            int index = tiles.FindIndex(x => x.position == tilePosition);
            if (index != -1)
            {
                tiles.RemoveAt(index);
            }
        }

        #endregion
    }
}