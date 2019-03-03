using System.Collections.Generic;
using Pandawan.Islands.Tilemaps.Tiles;
using UnityEngine;

namespace Pandawan.Islands.Tilemaps
{
    public class TileDB : MonoBehaviour
    {
        /**
         * The way this is going to work is: TileDB is to be used as a static class.
         * The MonoBehavior instance is there to make it easy to add new tiles through the inspector.
         * It simply transfers all of the tiles from the list to the actual Dictionary database.
         */

        // Keep track of an instance just in case I accidentally add more than one TileDB instance in the scene.
        private static TileDB instance;

        // Uses a dictionary under the hood to enable caching
        private static readonly Dictionary<string, BasicTile> tileDatabase = new Dictionary<string, BasicTile>();

        // TODO: Maybe add a not_found tile which is always returned rather than null
        [SerializeField] private List<BasicTile> tiles;

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
                Debug.LogError("Cannot have more than one TileDB instance.");

            if (tiles == null || tiles.Count == 0)
                Debug.LogError("Tile list is null or empty in TileDB.");
            else
                foreach (BasicTile tile in tiles)
                    tileDatabase.Add(tile.Id, tile);
        }

        public static BasicTile GetTile(string id)
        {
            if (tileDatabase == null || tileDatabase.Count == 0)
            {
                Debug.LogError("Tile list is null or empty in TileDB.");
                return null;
            }

            if (!tileDatabase.ContainsKey(id))
            {
                Debug.LogError($"Could not find a tile with id {id}");
                return null;
            }

            return tileDatabase[id];
        }
    }
}