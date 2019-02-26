using UnityEngine;

namespace Pandawan.Islands.Tilemaps
{
    public class ChunkLoader : MonoBehaviour
    {

        [SerializeField] private bool showGizmos = false;
        [SerializeField] private bool useChunkCoordinates = true;

        // Boundaries (in Chunk coordinates) at which to load the chunks (relative to the current transform position).
        [SerializeField] private BoundsInt relativeChunkBounds = new BoundsInt(Vector3Int.zero, Vector3Int.one);

        // Boundaries (in Tile coordinates) at which to load the chunks (relative to the current transform position).
        [SerializeField] private BoundsInt relativeTileBounds = new BoundsInt(Vector3Int.zero, Vector3Int.one);

        private void Update()
        {
            BoundsInt absoluteChunkBounds =
                ChunkToAbsoluteBounds(useChunkCoordinates
                    ? relativeChunkBounds
                    : TileToChunkBounds(relativeTileBounds));

            World.instance.RequestChunkLoading(absoluteChunkBounds);
        }

        // TODO: Move these methods to central "position" converting class. Just one major place with all the position manipulation.
        // This just converts a TileBound to a ChunkBound (including those that are partially contained)
        private BoundsInt TileToChunkBounds(BoundsInt tileBounds)
        {
            // I want this to change from a Tile Position to a Chunk Position, accounting for every tile
            // If one tile is in a different chunk, also include that chunk
            Vector3Int min = World.instance.GetChunkPositionForTile(tileBounds.min);
            Vector3Int max = World.instance.GetChunkPositionForTileCeil(tileBounds.max);
            Vector3Int size = max - min;

            BoundsInt bounds = new BoundsInt(min.x, min.y, min.z, size.x, size.y, size.z);
            
            return bounds;
        }
        
        // This converts the relative chunk bounds to absolute chunk bounds by adding in the current position (in Chunks)
        private BoundsInt ChunkToAbsoluteBounds(BoundsInt chunkBounds)
        {
            Vector3Int currentPosition =
                World.instance.GetChunkPositionForTile(Vector3Int.FloorToInt(transform.position));

            BoundsInt absoluteChunkBounds = new BoundsInt(chunkBounds.position + currentPosition, chunkBounds.size);

            return absoluteChunkBounds;
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
                
                BoundsInt absoluteChunkBounds =
                    ChunkToAbsoluteBounds(useChunkCoordinates
                        ? relativeChunkBounds
                        : TileToChunkBounds(relativeTileBounds));
                
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(absoluteChunkBounds.center * 32, absoluteChunkBounds.size * 32);
            }
        }
    }
}