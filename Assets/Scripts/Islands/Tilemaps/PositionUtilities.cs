using UnityEngine;

namespace Pandawan.Islands.Tilemaps
{
    public static class PositionUtilities
    {
        /*
         * Tile Position = A position to identify tiles in the world where 1 = 1 tile
         *
         * Chunk Position = A position to identify chunks where 1 = 1 chunk = [chunkSize] tiles
         *
         * Local Chunk Position A position to identify tiles inside a chunk.
         * These are relative to the chunk's position. They go from 0 to [chunkSize].
         */


        /// <summary>
        ///     Convert the given tile position to its corresponding chunk's position.
        /// </summary>
        /// <param name="tilePosition">The tile position to convert from.</param>
        /// <param name="chunkSize">The size of the chunks.</param>
        /// <param name="roundUp">Whether to round up (Ceil) instead of truncating (Floor)</param>
        /// <returns>The resulting chunk position.</returns>
        public static Vector3Int TileToChunkPosition(Vector3Int tilePosition, Vector3Int chunkSize, bool roundUp = false)
        {
            // Use Mathf.CeilToInt if rounding up
            if (roundUp)
                return new Vector3Int(
                    Mathf.CeilToInt((float) tilePosition.x / (tilePosition.x < 0 ? chunkSize.x + 1 : chunkSize.x) +
                                    (tilePosition.x < 0 ? -1 : 0)),
                    Mathf.CeilToInt((float) tilePosition.y / (tilePosition.y < 0 ? chunkSize.y + 1 : chunkSize.y) +
                                    (tilePosition.y < 0 ? -1 : 0)),
                    Mathf.CeilToInt((float) tilePosition.z / (tilePosition.z < 0 ? chunkSize.z + 1 : chunkSize.z) +
                                    (tilePosition.z < 0 ? -1 : 0))
                );
            
            // Not rounding up, just let int division take care of truncating
            return new Vector3Int(
                tilePosition.x / (tilePosition.x < 0 ? chunkSize.x + 1 : chunkSize.x) +
                (tilePosition.x < 0 ? -1 : 0),
                tilePosition.y / (tilePosition.y < 0 ? chunkSize.y + 1 : chunkSize.y) +
                (tilePosition.y < 0 ? -1 : 0),
                tilePosition.z / (tilePosition.z < 0 ? chunkSize.z + 1 : chunkSize.z) +
                (tilePosition.z < 0 ? -1 : 0)
            );
        }

        /// <summary>
        /// Convert the given tile position to a chunk's local position (from 0 to chunkSize - 1).
        /// </summary>
        /// <param name="tilePosition">The tile position to convert from.</param>
        /// <param name="chunkSize">The size of the chunks.</param>
        /// <returns>The resulting local chunk position.</returns>
        public static Vector3Int TileToLocalChunkPosition(Vector3Int tilePosition, Vector3Int chunkSize)
        {
            // Formula to convert to local position AND invert negative tile positions (arrays can't go below 0)
            return new Vector3Int((tilePosition.x % chunkSize.x + chunkSize.x) % chunkSize.x,
                (tilePosition.y % chunkSize.y + chunkSize.y) % chunkSize.y, (tilePosition.z % chunkSize.z + chunkSize.z) % chunkSize.z);
        }

        /// <summary>
        /// Convert the given local chunk position to a world tile position.
        /// </summary>
        /// <param name="localPosition">The local chunk position to convert from.</param>
        /// <param name="chunkPosition">The position of the chunk that contains the local position.</param>
        /// <param name="chunksize">The size of the chunks.</param>
        /// <returns>The resulting world tile position.</returns>
        public static Vector3Int LocalChunkToTilePosition(Vector3Int localPosition, Vector3Int chunkPosition, Vector3Int chunksize)
        {
            return new Vector3Int(localPosition.x + chunkPosition.x * chunksize.x, localPosition.y + chunkPosition.y * chunksize.y,
                localPosition.z + chunkPosition.z * chunksize.z);
        }

        /// <summary>
        /// Convert the given tile bounds to chunk bounds, making sure that all chunks are included.
        /// Including the chunks that are only partially contained within the tile bounds.
        /// </summary>
        /// <param name="tileBounds">The tile position boundaries to convert from.</param>
        /// <param name="chunkSize">The size of the chunks.</param>
        /// <returns>The resulting chunk bounds.</returns>
        public static BoundsInt TileToChunkBounds(BoundsInt tileBounds, Vector3Int chunkSize)
        {
            Vector3Int min = TileToChunkPosition(tileBounds.min, chunkSize);
            Vector3Int max = TileToChunkPosition(tileBounds.max, chunkSize, true);
            Vector3Int size = max - min;

            BoundsInt bounds = new BoundsInt(min.x, min.y, min.z, size.x, size.y, size.z);

            return bounds;
        }
    }
}