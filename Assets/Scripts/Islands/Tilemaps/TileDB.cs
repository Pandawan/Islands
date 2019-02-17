using System.Collections.Generic;
using Pandawan.Islands.Tilemaps.Tiles;
using UnityEngine;

namespace Pandawan.Islands.Tilemaps
{
    public class TileDB : MonoBehaviour
    {
        public static TileDB instance;

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
        }

        public BasicTile GetTile(string id)
        {
            if (tiles == null)
            {
                Debug.LogError("Tile list is null or empty in TileDB.");
                return null;
            }

            if (!tiles.Exists(x => x.Id == id))
            {
                Debug.LogError($"Could not find a tile with id {id}");
                return null;
            }

            return tiles.Find(x => x.Id == id);
        }
    }
}