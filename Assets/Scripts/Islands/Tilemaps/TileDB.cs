using System.Collections;
using System.Collections.Generic;
using Pandawan.Islands.Tilemaps.Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pandawan.Islands.Tilemaps
{
    public class TileDB : MonoBehaviour
    {
        public static TileDB instance;

        [SerializeField] private List<BasicTile> tiles;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Debug.LogError("Cannot have more than one TileDB instance.");
            }
        }

        public BasicTile GetTile(string id)
        {
            if (!tiles.Exists(x => x.Id == id))
            {
                Debug.LogError($"Could not find a tile with id {id}");
            }

            return tiles.Find(x => x.Id == id);
        }
    }
}