using Pandawan.Islands.Tilemaps;
using UnityEditor;

namespace Pandawan.Islands
{
    [CustomEditor(typeof(ChunkLoader))]
    public class BasicTileEditor : UnityEditor.Editor
    {
        private SerializedProperty relativeChunkBounds;
        private SerializedProperty relativeTileBounds;

        private SerializedProperty showGizmos;
        private SerializedProperty useChunkCoordinates;

        private void OnEnable()
        {
            showGizmos = serializedObject.FindProperty("showGizmos");
            useChunkCoordinates = serializedObject.FindProperty("useChunkCoordinates");
            relativeChunkBounds = serializedObject.FindProperty("relativeChunkBounds");
            relativeTileBounds = serializedObject.FindProperty("relativeTileBounds");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(showGizmos);
            EditorGUILayout.PropertyField(useChunkCoordinates);

            if (useChunkCoordinates.boolValue)
                EditorGUILayout.PropertyField(relativeChunkBounds);
            else
                EditorGUILayout.PropertyField(relativeTileBounds);

            serializedObject.ApplyModifiedProperties();
        }
    }
}