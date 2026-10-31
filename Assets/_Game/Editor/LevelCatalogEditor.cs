using System.Collections.Generic;
using Game.Runtime.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // The catalog list with each level's check status and the "append only" warning.
    [CustomEditor(typeof(LevelCatalog))]
    public sealed class LevelCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "In a released game, only append levels at the end. Reordering shifts player progress.",
                MessageType.Warning);

            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            var catalog = (LevelCatalog)target;
            for (int i = 0; i < catalog.Count; i++)
            {
                LevelAsset level = catalog.GetAsset(i);
                if (level == null)
                {
                    EditorGUILayout.LabelField($"{i}: (empty)", "❌ Empty entry");
                    continue;
                }
                IReadOnlyList<string> errors = LevelEditing.Check(level);
                string status = errors.Count == 0 ? "✅ Valid" : $"❌ {errors.Count} error(s)";
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i}: {level.name}", status);
                if (GUILayout.Button("Select", GUILayout.Width(55))) Selection.activeObject = level;
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
