using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pandawan.Islands.Other;
using UnityEngine;

namespace Pandawan.Islands.Tilemaps
{
    public class ChunkLoader : MonoBehaviour
    {
        [SerializeField] private bool showGizmos;
        [SerializeField] private bool useChunkCoordinates = true;

        // Boundaries (in Chunk coordinates) at which to load the chunks (relative to the current transform position).
        [SerializeField] private BoundsInt relativeChunkBounds = new BoundsInt(Vector3Int.zero, Vector3Int.one);

        // Boundaries (in Tile coordinates) at which to load the chunks (relative to the current transform position).
        [SerializeField] private BoundsInt relativeTileBounds = new BoundsInt(Vector3Int.zero, Vector3Int.one);

        private List<Vector3Int> previousLoadedChunks = new List<Vector3Int>();

        private async void Update()
        {
            await LoadChunks();
        }

        /// <summary>
        ///     Gets every chunk within the given bounds.
        ///     Loads new ones.
        ///     Unloads old/non-needed ones.
        /// </summary>
        private async Task LoadChunks()
        {
            // Get all the chunks that are within the bounds
            BoundsInt absoluteChunkBounds = GetAbsoluteBounds();

            List<Vector3Int> newChunksList = absoluteChunkBounds.ToList();

            // Get new chunks (chunks that are in new list but not in old one) 
            List<Vector3Int> chunksToLoad = new List<Vector3Int>(newChunksList
                .Where(pos => !previousLoadedChunks.Contains(pos)));

            // Get new chunks (chunks that are in old list but not in new one) 
            List<Vector3Int> chunksToUnload = new List<Vector3Int>(previousLoadedChunks.ToList()
                .Where(pos => !newChunksList.Contains(pos)));

            // Update previous list
            previousLoadedChunks = newChunksList;

            if (chunksToLoad.Count > 0)
                // Fire and forget chunk loading
                await World.instance.RequestChunkLoading(chunksToLoad, this);

            if (chunksToUnload.Count > 0)
                // Fire and forget chunk unloading
                await World.instance.RequestChunkUnloading(chunksToUnload, this);
        }

        private void OnDrawGizmosSelected()
        {
            if (World.instance != null && showGizmos)
            {
                if (!useChunkCoordinates)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(relativeTileBounds.center, relativeTileBounds.size);
                }

                BoundsInt absoluteChunkBounds = GetAbsoluteBounds();
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(absoluteChunkBounds.center * 32, absoluteChunkBounds.size * 32);
            }
        }

        #region Position Helpers

        /// <summary>
        ///     Get the Absolute Chunk Boundaries (using either chunk or tile boundaries)
        /// </summary>
        private BoundsInt GetAbsoluteBounds()
        {
            return ChunkToAbsoluteBounds(useChunkCoordinates
                ? relativeChunkBounds
                : World.instance.TileToChunkBounds(relativeTileBounds));
        }

        // This converts the relative chunk bounds to absolute chunk bounds by adding in the current position (in Chunks)
        private BoundsInt ChunkToAbsoluteBounds(BoundsInt chunkBounds)
        {
            Vector3Int currentPosition =
                World.instance.TileToChunkPosition(Vector3Int.FloorToInt(transform.position));

            BoundsInt absoluteChunkBounds = new BoundsInt(chunkBounds.position + currentPosition, chunkBounds.size);

            return absoluteChunkBounds;
        }

        #endregion
    }
}