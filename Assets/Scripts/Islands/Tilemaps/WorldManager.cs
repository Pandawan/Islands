using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Pandawan.Islands.Other;
using Pandawan.Islands.Serialization;
using UnityEngine;

// TODO: Might want to move this to a different namespace?
namespace Pandawan.Islands.Tilemaps
{
    public static class WorldManager
    {
        // TODO: Standardize the error messages
        // TODO: Find a way to make this work on separate threads
        /// <summary>
        ///     Save the given world to the file system.
        /// </summary>
        /// <param name="world">The world to save.</param>
        public static void Save(World world)
        {
            List<Chunk> chunks = world.GetDirtyChunks();
            string savePath = GetWorldSavePath(world.GetWorldInfo().GetId());
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

            // Save the world info
            string worldInfoPath = Path.Combine(savePath, "world.dat");
            try
            {
                using (Stream stream =
                    new FileStream(worldInfoPath, FileMode.Create, FileAccess.ReadWrite))
                {
                    formatter.Serialize(stream, world.GetWorldInfo());
                }
            }
            catch (IOException e)
            {
                Debug.LogError($"Error while saving WorldInfo for {world} at \"{worldInfoPath}\". {e}");
            }

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
                    Debug.LogError($"Error while saving {chunk} for {world} at \"{chunkPath}\". {e}");
                }
            }

            Debug.Log($"Successfully saved {world} at \"{savePath}\".");
        }

        /// <summary>
        ///     Load a world with the specified world id.
        /// </summary>
        /// <param name="worldId">The world id to look for.</param>
        /// <param name="world">The world to apply the changes to.</param>
        public static void Load(string worldId, World world)
        {
            string savePath = GetWorldSavePath(worldId);

            // Check that the World's Directory/Save exists
            if (!Directory.Exists(savePath))
            {
                Debug.LogError($"Could not load world {world} at \"{savePath}\". It does not exist.");
                return;
            }

            IFormatter formatter = GetBinaryFormatter();

            // Read the WorldInfo file
            string worldInfoPath = Path.Combine(savePath, "world.dat");
            try
            {
                using (Stream stream =
                    new FileStream(worldInfoPath, FileMode.Open, FileAccess.Read))
                {
                    WorldInfo info = (WorldInfo) formatter.Deserialize(stream);
                    world.SetWorldInfo(info);
                    Debug.Log($"Found valid world at \"{savePath}\".");
                }
            }
            catch (IOException e)
            {
                Debug.LogError($"Error while loading WorldInfo for {world} at {worldInfoPath}. {e}");
                return;
            }

            string chunksPath = Path.Combine(savePath, "chunks");

            // Check if there are chunks to load
            if (Directory.Exists(chunksPath))
            {
                List<Chunk> chunks = new List<Chunk>();

                // Get all file names in the chunks directory
                string[] chunkPaths = Directory.GetFiles(chunksPath);
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
                        Debug.LogError($"Error while loading chunk for {world} at \"{chunkPath}\". {e}");
                    }

                world.LoadChunks(chunks);
            }
        }

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

        #region Helper

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