using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pandawan.Islands.Tilemaps.Generation
{
    [RequireComponent(typeof(World))]
    public class TilemapImporter : MonoBehaviour
    {
        [SerializeField] private Tilemap tilemap;

        private World worldComponent;

        private void Awake()
        {
            worldComponent = GetComponent<World>();

            if (tilemap == null)
            {
                Debug.LogError("No Tilemap set for TilemapImporter.");
                return;
            }

            if (worldComponent == null)
            {
                Debug.LogError("World Component is required for WorldGeneration to act.");
                return;
            }

            worldComponent.GenerationEvent += Import;
        }

        public async Task Import(World world)
        {
            await WorldManager.ImportTilemap(tilemap, world);
            Debug.Log("Successfully imported Tilemap to World.");
        }
    }
}