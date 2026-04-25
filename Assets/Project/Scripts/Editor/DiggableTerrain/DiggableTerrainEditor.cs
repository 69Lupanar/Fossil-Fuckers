using UnityEditor;
using UnityEngine;
using DiggableTerrainTarget = Assets.Project.Scripts.Runtime.ViewModels.DiggableTerrain.DiggableTerrain;

namespace Assets.Project.Scripts.Editor.DiggableTerrain
{
    /// <summary>
    /// Custom Editor
    /// </summary>
    [CustomEditor(typeof(DiggableTerrainTarget))]
    public class DiggableTerrainEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Affiche l'inspecteur
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DiggableTerrainTarget terrain = target as DiggableTerrainTarget;

            if (GUILayout.Button("Generate terrain"))
            {
                terrain.GenerateTerrain();
                EditorUtility.SetDirty(terrain);
            }

            if (GUILayout.Button("Clear terrain"))
            {
                terrain.Clear();
                EditorUtility.SetDirty(terrain);
            }
        }
    }
}