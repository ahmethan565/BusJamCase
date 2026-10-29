using System.Collections.Generic;
using Game.Runtime.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // The catalog list with each level's check status and the "append only" warning (§15.2).
    [CustomEditor(typeof(LevelCatalog))]
    public sealed class LevelCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Yayındaki oyunda bölümleri sadece sona ekleyin. Sıra değişirse oyuncu ilerlemesi kayar.",
                MessageType.Warning);

            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Durum", EditorStyles.boldLabel);
            var catalog = (LevelCatalog)target;
            for (int i = 0; i < catalog.Count; i++)
            {
                LevelAsset level = catalog.GetAsset(i);
                if (level == null)
                {
                    EditorGUILayout.LabelField($"{i}: (boş)", "❌ Boş kayıt");
                    continue;
                }
                IReadOnlyList<string> errors = LevelEditing.Check(level);
                string status = errors.Count == 0 ? "✅ Geçerli" : $"❌ {errors.Count} hata";
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i}: {level.name}", status);
                if (GUILayout.Button("Seç", GUILayout.Width(40))) Selection.activeObject = level;
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
