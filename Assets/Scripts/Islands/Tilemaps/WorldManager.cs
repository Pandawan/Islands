using System;
using System.Collections;
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
        // TODO: Find a way to make this work on separate threads
        /// <summary>
        /// Save the given world to the file system.
        /// </summary>
        /// <param name="world">The world to save.</param>
        public static void Save(World world)
        {
            List<Chunk> chunks = world.GetDirtyChunks();
            string savePath = GetWorldSavePath(world.GetId());
            string chunksPath = Path.Combine(savePath, "chunks");

            // Check that the save path already exists
            try
            {
                // Create a Directory if it doesn't exist
                Directory.CreateDirectory(chunksPath);
            }
            catch (IOException e)
            {
                Debug.LogError($"Error while opening save path {savePath}. {e}");
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
                catch (UnauthorizedAccessException e)
                {
                    Debug.LogError($"Error while saving {chunk}. {e}");
                }
            }

            Debug.Log($"Successfully saved {world} at \"{savePath}\"");
        }

        /// <summary>
        /// Get a Binary Formatter that is customized to serialize World data.
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
        /// Get the path to the Saves directory where world saves are stored.
        /// </summary>
        /// <returns>The saves path.</returns>
        public static string GetSavesPath()
        {
            return Path.GetFullPath(Path.Combine(Application.persistentDataPath, Config.SAVES_DIRECTORY));
        }

        /// <summary>
        /// Get the path to the given world's save directory.
        /// </summary>
        /// <param name="worldPath">The world to get.</param>
        /// <returns>The world's save path.</returns>
        public static string GetWorldSavePath(string worldPath)
        {
            return Path.GetFullPath(Path.Combine(GetSavesPath(), worldPath));
        }
    }
}