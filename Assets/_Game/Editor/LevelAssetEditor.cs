using System.Collections.Generic;
using Game.Core;
using Game.Core.Levels;
using Game.Core.Path;
using Game.Runtime.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Level editor in the Inspector (§15.1): paint the grid, set buses and numbers, live checks, solve, play.
    // Every change goes through SerializedObject, so it is undoable and saved.
    [CustomEditor(typeof(LevelAsset))]
    public sealed class LevelAssetEditor : UnityEditor.Editor
    {
        private const float CellSize = 34f;
        private const int SolverGames = 1000;

        private static readonly (char symbol, string label, Color color)[] Palette =
        {
            ('.', "Floor", new Color(0.85f, 0.85f, 0.85f)),
            ('#', "Wall", new Color(0.30f, 0.30f, 0.35f)),
            ('-', "Void", new Color(0.12f, 0.12f, 0.12f)),
            ('R', "R", new Color(0.90f, 0.25f, 0.25f)),
            ('G', "G", new Color(0.25f, 0.70f, 0.35f)),
            ('B', "B", new Color(0.25f, 0.50f, 0.90f)),
            ('Y', "Y", new Color(0.98f, 0.82f, 0.20f)),
            ('P', "P", new Color(0.62f, 0.35f, 0.80f)),
            ('O', "O", new Color(0.98f, 0.55f, 0.15f)),
        };

        private SerializedProperty rows, buses, busCapacity, waitingSlots, version, timeLimitSeconds;
        private char brush = 'R';
        private string autoFillWarning;
        private string solveReport;

        private void OnEnable()
        {
            rows = serializedObject.FindProperty("rows");
            buses = serializedObject.FindProperty("buses");
            busCapacity = serializedObject.FindProperty("busCapacity");
            waitingSlots = serializedObject.FindProperty("waitingSlots");
            version = serializedObject.FindProperty("version");
            timeLimitSeconds = serializedObject.FindProperty("timeLimitSeconds");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            string[] grid = ReadRows();
            int height = grid.Length;
            int width = height > 0 ? grid[0].Length : 0;

            // --- Size and numbers
            EditorGUILayout.BeginHorizontal();
            int newWidth = Mathf.Max(1, EditorGUILayout.IntField("Width", width));
            int newHeight = Mathf.Max(1, EditorGUILayout.IntField("Height", height));
            EditorGUILayout.EndHorizontal();
            if (newWidth != width || newHeight != height)
            {
                grid = LevelEditing.Resize(grid, newWidth, newHeight);
                WriteRows(grid);
            }
            EditorGUILayout.PropertyField(waitingSlots, new GUIContent("Waiting Slots"));
            EditorGUILayout.PropertyField(busCapacity, new GUIContent("Bus Capacity"));
            EditorGUILayout.PropertyField(timeLimitSeconds, new GUIContent("Time Limit (s)", "Counted from the first tap. 0 = no time limit"));
            EditorGUILayout.PropertyField(version, new GUIContent("Version"));

            // --- Palette and grid
            EditorGUILayout.Space();
            DrawPalette();
            grid = DrawGrid(grid);

            // --- Buses
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(buses, new GUIContent("Buses"));
            if (GUILayout.Button("Auto Fill", GUILayout.Width(120)))
            {
                LevelEditing.AutoFillBuses(grid, busCapacity.intValue, out string filled, out autoFillWarning);
                buses.stringValue = filled;
            }
            EditorGUILayout.EndHorizontal();
            if (autoFillWarning != null) EditorGUILayout.HelpBox(autoFillWarning, MessageType.Warning);

            bool changed = serializedObject.ApplyModifiedProperties();
            if (changed) solveReport = null;

            // --- Live checks
            EditorGUILayout.Space();
            var level = (LevelAsset)target;
            IReadOnlyList<string> errors = LevelEditing.Check(level);
            if (errors.Count == 0) EditorGUILayout.HelpBox("Level is valid.", MessageType.Info);
            foreach (string error in errors) EditorGUILayout.HelpBox(error, MessageType.Error);

            // --- Solve and play
            using (new EditorGUI.DisabledScope(errors.Count > 0))
            {
                if (GUILayout.Button("Solve")) solveReport = Solve(level);
            }
            if (solveReport != null) EditorGUILayout.HelpBox(solveReport, MessageType.None);

            using (new EditorGUI.DisabledScope(errors.Count > 0 || EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Play This Level ▶")) EditorLevelLauncher.Play(level);
            }
        }

        private string[] ReadRows()
        {
            var result = new string[rows.arraySize];
            for (int i = 0; i < result.Length; i++) result[i] = rows.GetArrayElementAtIndex(i).stringValue ?? "";
            return result;
        }

        private void WriteRows(string[] grid)
        {
            rows.arraySize = grid.Length;
            for (int i = 0; i < grid.Length; i++) rows.GetArrayElementAtIndex(i).stringValue = grid[i];
        }

        private void DrawPalette()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Palette", GUILayout.Width(50));
            foreach ((char symbol, string label, Color color) in Palette)
            {
                Color old = GUI.backgroundColor;
                GUI.backgroundColor = color;
                bool selected = GUILayout.Toggle(brush == symbol, label, "Button", GUILayout.Width(label.Length > 1 ? 52 : 28));
                if (selected) brush = symbol;
                GUI.backgroundColor = old;
            }
            EditorGUILayout.EndHorizontal();
        }

        // Click or drag to paint.
        private string[] DrawGrid(string[] grid)
        {
            int height = grid.Length;
            int width = height > 0 ? grid[0].Length : 0;
            Rect area = GUILayoutUtility.GetRect(width * CellSize, height * CellSize, GUILayout.ExpandWidth(false));
            Event e = Event.current;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width && x < grid[y].Length; x++)
                {
                    var cell = new Rect(area.x + x * CellSize, area.y + y * CellSize, CellSize - 2f, CellSize - 2f);
                    char symbol = grid[y][x];
                    EditorGUI.DrawRect(cell, ColorOf(symbol));
                    if (symbol != '.' && symbol != '#' && symbol != '-')
                        GUI.Label(cell, symbol.ToString(), EditorStyles.centeredGreyMiniLabel);

                    bool paint = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                                 && e.button == 0 && cell.Contains(e.mousePosition);
                    if (paint && symbol != brush)
                    {
                        grid = LevelEditing.Paint(grid, x, y, brush);
                        WriteRows(grid);
                        e.Use();
                    }
                }
            }
            return grid;
        }

        private static Color ColorOf(char symbol)
        {
            foreach ((char s, string _, Color color) in Palette)
                if (s == symbol) return color;
            return Color.magenta;   // unknown character: the live check lists it
        }

        private static string Solve(LevelAsset level)
        {
            LevelData data = level.Parse().Level;
            var solver = new LevelSolver(new BfsPathFinder());
            SolveResult result = solver.Solve(data);
            if (result.HitLimit) return $"Unknown: the search limit was reached ({result.StatesExplored} states).";
            if (!result.IsSolvable) return $"Not solvable · {result.StatesExplored} states";

            float rate = solver.EstimateRandomWinRate(data, SolverGames, seed: 1);
            return $"Solvable · {result.StatesExplored} states · Random win rate {rate * 100f:0.#}%";
        }
    }
}
