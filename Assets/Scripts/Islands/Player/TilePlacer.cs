using Pandawan.Islands.Tilemaps;
using UnityEngine;

namespace Pandawan.Islands.Player
{
    // TODO: Remove this class
    // Temporary class to test TileMap Engine
    public class TilePlacer : MonoBehaviour
    {
        private void Update()
        {
            Vector3Int position =
                Vector3Int.FloorToInt(UnityEngine.Camera.main.ScreenToWorldPoint(Input.mousePosition));
            position.z = 1;
            if (Input.GetMouseButton(0))
            {
                World.instance.SetTileAt(position, "tree");
            }
            else if (Input.GetMouseButton(1))
            {
                World.instance.RemoveTileAt(position);
            }
        }
    }
}