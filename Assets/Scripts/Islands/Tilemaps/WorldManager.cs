using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Pandawan.Islands.Other;
using Pandawan.Islands.Serialization;
using UnityEngine;
using UnityEngine.Tilemaps;

// TODO: Might want to move this to a different namespace?
namespace Pandawan.Islands.Tilemaps
{
    public static class WorldManager
    {
        // TODO: Optimize this? Check if possible to import multiple tiles at once? Or maybe make it so that the Chunk doesn't "SET" the tilemap again.
        // TODO: Perhaps find way to make it so it doesn't set the chunk as Dirty?
        /// <summary>
        ///     Imports all of the tiles in the tilemap into the World
        /// </summary>
        /// <param name="tilemap">The Tilemap to import from</param>
        /// <param name="world">The World to import to</param>
        public static void ImportTilemap(Tilemap tilemap, World world)
        {
            foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
                if (tilemap.HasTile(pos))
                    world.SetTileAt(pos, tilemap.GetTile<TileBase>(pos).name);
        }

        #region World 

        public static void LoadWorld(string id, World world)
        {
            WorldInfo info = LoadWorldInfo(id);
            world.SetWorldInfo(info);

            // TODO: Do other World Loading Steps
        }

        public static void LoadWorldAt(string savePath, World world)
        {
            WorldInfo info = LoadWorldInfoAt(savePath);
            world.SetWorldInfo(info);

            // TODO: Do other World Loading Steps
        }

        public static void SaveWorld(World world)
        {
            WorldInfo info = world.GetWorldInfo();

            // Save the world info
            SaveWorldInfo(info);

            // Save all the dirty chunks
            SaveChunks(world.GetDirtyChunks(), info);
        }

        public static void SaveWorldAt(World world, string savePath)
        {
            WorldInfo info = world.GetWorldInfo();

            SaveWorldInfoAt(info, savePath);

            SaveChunksAt(world.GetDirtyChunks(), savePath);
        }

        #endregion

        #region WorldInfo Load

        public static WorldInfo LoadWorldInfo(string id)
        {
            string savePath = GetWorldSavePath(id);
            return LoadWorldInfoAt(savePath);
        }

        public static WorldInfo LoadWorldInfoAt(string savePath)
        {
            // Check that the World's Directory/Save exists
            if (!Directory.Exists(savePath))
            {
                Debug.LogError($"Could not load world at \"{savePath}\". It does not exist.");
                return WorldInfo.Default;
            }

            IFormatter formatter = GetBinaryFormatter();

            WorldInfo info = WorldInfo.Default;

            // Read the WorldInfo file
            string worldInfoPath = Path.Combine(savePath, "world.dat");
            try
            {
                using (Stream stream =
                    new FileStream(worldInfoPath, FileMode.Open, FileAccess.Read))
                {
                    info = (WorldInfo) formatter.Deserialize(stream);
                    Debug.Log($"Found valid world at \"{savePath}\".");
                }
            }
            catch (IOException e)
            {
                Debug.LogError($"Error while loading WorldInfo at {worldInfoPath}. {e}");
            }

            return info;
        }

        #endregion

        #region Chunk Save

        public static void SaveChunks(List<Chunk> chunks, WorldInfo worldInfo)
        {
            string savePath = GetWorldSavePath(worldInfo.GetId());

            SaveChunksAt(chunks, savePath);
        }

        public static void SaveChunksAt(List<Chunk> chunks, string savePath)
        {
            string chunksPath = Path.Combine(savePath, "chunks");

            // Check that the save path already exists
            try
            {
                // Create a Directory if it doesn't exist
                Directory.CreateDirectory(chunksPath);
            }
            catch (IOException e)
            {
                Debug.LogError($"Error while opening save path \"{savePath}\". {e}");
                return;
            }

            IFormatter formatter = GetBinaryFormatter();

            // Save all the chunk data
            foreach (Chunk chunk in chunks)
            {
                // Save chunk at SavesPath/chunks/chunk_id.dat
                string chunkPath = Path.Combine(chunksPath, $"{chunk.GetId()}.dat");
                try
                {
                    // Try opening a file path
                    using (Stream stream = new FileStream(chunkPath, FileMode.Create, FileAccess.ReadWrite))
                    {
                        formatter.Serialize(stream, chunk);
                    }
                }
                catch (IOException e)
                {
                    Debug.LogError($"Error while saving {chunk} at \"{chunkPath}\". {e}");
                }
            }

            Debug.Log($"Successfully saved chunk at \"{savePath}\".");
        }

        #endregion

        #region Chunk Load

        // TODO: Make <summary>s for every method here

        /// <summary>
        ///     Whether or not the chunks with given position exist.
        /// </summary>
        /// <param name="chunkPos">The position to check.</param>
        /// <param name="worldInfo">The world to check in.</param>
        /// <returns>Returns true if ALL chunks exist.</returns>
        public static bool ChunksExist(List<Vector3Int> chunkPos, WorldInfo worldInfo)
        {
            string savePath = GetWorldSavePath(worldInfo.GetId());

            string chunksPath = Path.Combine(savePath, "chunks");

            // Get save path for each Chunk position
            string[] chunkPaths = chunkPos.Select(pos => Path.Combine(chunksPath,
                    $"{Chunk.GetIdForPosition(pos)}.dat"))
                .ToArray();

            foreach (string chunkPath in chunkPaths)
                if (!File.Exists(chunkPath))
                    return false;

            return true;
        }

        public static List<Chunk> LoadChunk(List<Vector3Int> chunkPos, WorldInfo worldInfo)
        {
            string savePath = GetWorldSavePath(worldInfo.GetId());
            return LoadChunkAt(chunkPos, savePath);
        }

        public static List<Chunk> LoadChunkAt(List<Vector3Int> chunkPos, string savePath)
        {
            // Check that the World's Directory/Save exists
            if (!Directory.Exists(savePath))
            {
                Debug.LogError($"Could not load chunks at \"{savePath}\". It does not exist.");
                return null;
            }

            IFormatter formatter = GetBinaryFormatter();

            string chunksPath = Path.Combine(savePath, "chunks");

            // Check if there are chunks to load
            if (Directory.Exists(chunksPath))
            {
                List<Chunk> chunks = new List<Chunk>();

                // Get save path for each Chunk position
                string[] chunkPaths = chunkPos.Select(pos => Path.Combine(chunksPath,
                        $"{Chunk.GetIdForPosition(pos)}.dat"))
                    .ToArray();
                foreach (string chunkPath in chunkPaths)
                    try
                    {
                        using (Stream stream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read))
                        {
                            chunks.Add((Chunk) formatter.Deserialize(stream));
                        }
                    }
                    catch (IOException e)
                    {
                        Debug.LogError($"Error while loading chunks at \"{chunkPath}\". {e}");
                    }

                return chunks;
            }

            return null;
        }

        #endregion

        #region WorldInfo Save

        public static void SaveWorldInfo(WorldInfo worldInfo)
        {
            string savePath = GetWorldSavePath(worldInfo.GetId());
            SaveWorldInfoAt(worldInfo, savePath);
        }

        public static void SaveWorldInfoAt(WorldInfo worldInfo, string savePath)
        {
            // Check that the save path already exists
            try
            {
                // Create a Directory if it doesn't exist
                Directory.CreateDirectory(savePath);
            }
            catch (IOException e)
            {
                Debug.LogError($"Error while opening save path \"{savePath}\". {e}");
                return;
            }

            IFormatter formatter = GetBinaryFormatter();

            // Save the world info
            string worldInfoPath = Path.Combine(savePath, "world.dat");
            try
            {
                using (Stream stream =
                    new FileStream(worldInfoPath, FileMode.Create, FileAccess.ReadWrite))
                {
                    formatter.Serialize(stream, worldInfo);
                }
            }
            catch (IOException e)
            {
                Debug.LogError($"Error while saving WorldInfo at \"{worldInfoPath}\". {e}");
            }
        }

        #endregion

        #region Helper

        /// <summary>
        ///     Whether or not there exists a World with the given Id
        /// </summary>
        /// <param name="worldId">The World Id to search for</param>
        /// <returns>True if it exists</returns>
        public static bool WorldExists(string worldId)
        {
            string savePath = GetWorldSavePath(worldId);
            return Directory.Exists(savePath);
        }

        /// <summary>
        ///     Get a Binary Formatter that is customized to serialize World data.
        /// </summary>
        /// <returns>The Binary Formatter object.</returns>
        private static BinaryFormatter GetBinaryFormatter()
        {
            // Create new Binary Formatter
            BinaryFormatter formatter = new BinaryFormatter();

            // Create surrogate selector to add new surrogates
            SurrogateSelector surrogateSelector = new SurrogateSelector();

            // Add all the surrogates to convert non-serializable to serializable
            Vector3IntSerializationSurrogate vector3IntSurrogate = new Vector3IntSerializationSurrogate();
            surrogateSelector.AddSurrogate(typeof(Vector3Int), new StreamingContext(StreamingContextStates.All),
                vector3IntSurrogate);

            // Apply SurrogateSelector
            formatter.SurrogateSelector = surrogateSelector;

            return formatter;
        }

        /// <summary>
        ///     Get the path to the Saves directory where world saves are stored.
        /// </summary>
        /// <returns>The saves path.</returns>
        public static string GetSavesPath()
        {
            return Path.GetFullPath(Path.Combine(Application.persistentDataPath, Config.SAVES_DIRECTORY));
        }

        /// <summary>
        ///     Get the path to the given world's save directory.
        /// </summary>
        /// <param name="worldPath">The world to get.</param>
        /// <returns>The world's save path.</returns>
        public static string GetWorldSavePath(string worldPath)
        {
            return Path.GetFullPath(Path.Combine(GetSavesPath(), worldPath));
        }

        #endregion
    }
}