using Pandawan.Islands.Tilemaps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pandawan.Islands.Editor
{
    public class WorldToolsEditor : EditorWindow
    {
        private Tilemap tilemap;
        private World world;

        private bool importedTilemap = false;

        [MenuItem("Window/World Tools")]
        public static void ShowWindow()
        {
            WorldToolsEditor editor = GetWindow<WorldToolsEditor>("World Tools");
            editor.importedTilemap = false;
        }
        
        public void OnGUI()
        {
            // TODO: Add way to Import World from script quickly using this Editor


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("World Saver", EditorStyles.boldLabel);
            // TODO: Make it so you don't have to be in Play Mode to Save Tilemap as World
            // This is because World.instance and TileDB.instance aren't created in Edit Mode.
            // To fix this, remove WorldGeneration from World entirely. Then make both TileDB and World [ExecuteInEditMode]

            tilemap = EditorGUILayout.ObjectField(tilemap, typeof(Tilemap), true) as Tilemap;
            world = EditorGUILayout.ObjectField(world, typeof(World), true) as World;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Note: You have to be in Play Mode for this to work!");

            EditorGUILayout.Space();

            // Only enable Save button if Editor is in PlayMode
            EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
            bool tilemapToWorld = GUILayout.Button("Import Tilemap to World");
            EditorGUI.EndDisabledGroup();

            // Only enable Save button if Tilemap has been imported & Editor is in PlayMode
            EditorGUI.BeginDisabledGroup(!importedTilemap || !EditorApplication.isPlaying);
            bool saveWorld = GUILayout.Button("Save World");
            EditorGUI.EndDisabledGroup();
            
            // Import tilemap to world
            if (tilemapToWorld)
            {
                if (tilemap == null)
                {
                    Debug.LogError("Given Tilemap is null or invalid!");
                    return;
                }

                if (world == null)
                {
                    Debug.LogError("Given World is null or invalid!");
                    return;
                }

                if (!EditorApplication.isPlaying)
                {
                    Debug.LogError("You have to be in Play Mode to apply the Tilemap to the World!");
                    return;
                }

                // Import the Tilemap into the World
                WorldManager.ImportTilemap(tilemap, world);

                importedTilemap = true;

                Debug.Log("Successfully imported Tilemap to World!");
            }

            // Save world to path
            if (importedTilemap && saveWorld)
            {
                if (tilemap == null)
                {
                    Debug.LogError("Given Tilemap is null or invalid!");
                    return;
                }

                if (world == null)
                {
                    Debug.LogError("Given World is null or invalid!");
                    return;
                }

                if (!EditorApplication.isPlaying)
                {
                    Debug.LogError("You have to be in Play Mode to apply the Tilemap to the World!");
                    return;
                }

                string path = EditorUtility.SaveFolderPanel("Save Tilemap", "", world.GetWorldInfo().GetId());
                
                // Try saving the world
                WorldManager.Save(world, path);
            }
        }
    }
}