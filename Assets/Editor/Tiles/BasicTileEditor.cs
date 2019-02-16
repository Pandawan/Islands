using System;
using System.Reflection;
using Pandawan.Islands.Tilemaps.Tiles;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Pandawan.Islands.Editor
{
    [CustomEditor(typeof(BasicTile))]
    public class BasicTileEditor : UnityEditor.Editor
    {
        public BasicTile Target => target as BasicTile;

        #region Inspector

        // Custom Inspector
        public override void OnInspectorGUI()
        {
            // Set ReadOnly Id Field
            bool wasEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUILayout.TextField("Id", Target.Id);
            GUI.enabled = wasEnabled;

            // Show each property field

            Target.TileName = EditorGUILayout.TextField("Name", Target.TileName);

            // Sprite renders as big field if setting label in the method
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Source Image");
            Target.Sprite = (Sprite) EditorGUILayout.ObjectField(Target.Sprite, typeof(Sprite), false);
            EditorGUILayout.EndHorizontal();

            Target.Color = EditorGUILayout.ColorField("Color", Target.Color);

            Target.ColliderType =
                (Tile.ColliderType) EditorGUILayout.EnumPopup("Collider Type", Target.ColliderType);

            // If value changed, set dirty to be saved later
            EditorUtility.SetDirty(Target);
        }

        #endregion

        #region Icon

        // Set Asset icon in the File Explorer
        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (Target.Sprite != null)
            {
                Type t = GetType("UnityEditor.SpriteUtility");
                if (t != null)
                {
                    MethodInfo method = t.GetMethod("RenderStaticPreview",
                        new[] {typeof(Sprite), typeof(Color), typeof(int), typeof(int)});
                    if (method != null)
                    {
                        object ret = method.Invoke("RenderStaticPreview",
                            new object[] {Target.Sprite, Target.Color, width, height});
                        if (ret is Texture2D)
                            return ret as Texture2D;
                    }
                }
            }

            return base.RenderStaticPreview(assetPath, subAssets, width, height);
        }

        // Allow for reflection to call methods?
        private static Type GetType(string TypeName)
        {
            Type type = Type.GetType(TypeName);
            if (type != null)
                return type;

            if (TypeName.Contains("."))
            {
                string assemblyName = TypeName.Substring(0, TypeName.IndexOf('.'));
                Assembly assembly = Assembly.Load(assemblyName);
                if (assembly == null)
                    return null;
                type = assembly.GetType(TypeName);
                if (type != null)
                    return type;
            }

            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            AssemblyName[] referencedAssemblies = currentAssembly.GetReferencedAssemblies();
            foreach (AssemblyName assemblyName in referencedAssemblies)
            {
                Assembly assembly = Assembly.Load(assemblyName);
                if (assembly != null)
                {
                    type = assembly.GetType(TypeName);
                    if (type != null)
                        return type;
                }
            }

            return null;
        }

        #endregion

        #region Preview

        // Enable Preview
        public override bool HasPreviewGUI()
        {
            return true;
        }

        // Allow Preview at the bottom of inspector with the texture + Color applied
        public override void OnPreviewGUI(Rect rect, GUIStyle backgroundStyle)
        {
            if (Event.current.type == EventType.Repaint)
                if (Target.Sprite != null && Target.Sprite.texture != null)
                {
                    Texture2D texture = Target.Sprite.texture;
                    GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true,
                        (float) texture.width / texture.height, Target.Color, Vector4.zero, 0);
                }
        }

        // Set the title of the Preview
        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent(Target.TileName + " Preview");
        }

        #endregion
    }
}