using System.Collections;
using System.Collections.Generic;
using Pandawan.Islands.Tilemaps.Generation;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pandawan.Islands.Tilemaps
{
    public class World : MonoBehaviour
    {
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private GridInformation gridInfo;
        [SerializeField] private WorldGeneration worldGen;

        private void Start()
        {
            worldGen.Generate(this);
        }

        /// <summary>
        /// Set a tile in the world using an id.
        /// </summary>
        /// <param name="position">The position to set the tile at.</param>
        /// <param name="id">The id of tile to set.</param>
        public void SetTile(Vector3Int position, string id)
        {
            TileBase tile = TileDB.instance.GetTile(id);
            if (tile == null)
            {
                return;
            }

            tilemap.SetTile(position, TileDB.instance.GetTile(id));
        }

        /// <summary>
        /// Get the world's Tilemap to access Tiles
        /// </summary>
        /// <returns>Tilemap component</returns>
        public Tilemap GetTilemap()
        {
            return tilemap;
        }

        /// <summary>
        /// Get the GridInformation to store TileData
        /// </summary>
        /// <returns>GridInformation component</returns>
        public GridInformation GetGridInformation()
        {
            return gridInfo;
        }
    }
}