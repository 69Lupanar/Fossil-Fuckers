using Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Views.MarchingSquares
{
    /// <summary>
    /// Interface permettant d'éditer les propriétés
    /// de la brosse utilisée par le VoxelMap
    /// </summary>
    public class VoxelStencilEditView : MonoBehaviour
    {
        #region Variables Unity

        /// <summary>
        /// Noms des types de remplissage des brosses
        /// </summary>
        [Tooltip("Noms des types de remplissage des brosses")]
        public string[] fillTypeNames = { "Filled", "Empty" };

        /// <summary>
        /// Tailles des brosses
        /// </summary>
        [Tooltip("Tailles des brosses")]
        public string[] radiusNames = { "0", "1", "2", "3", "4", "5" };

        /// <summary>
        /// Types des brosses
        /// </summary>
        [Tooltip("Tailles des brosses")]
        public string[] stencilNames = { "Square", "Circle" };

        /// <summary>
        /// Grille des voxels
        /// </summary>
        [Tooltip("Grille des voxels")]
        public VoxelMap voxelMap;

        #endregion

        #region Méthodes Unity

        /// <summary>
        /// UI
        /// </summary>
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(4f, 4f, 150f, 500f));
            GUILayout.Label("Fill Type");
            voxelMap.FillTypeIndex = GUILayout.SelectionGrid(voxelMap.FillTypeIndex, fillTypeNames, 2);
            GUILayout.Label("Radius");
            voxelMap.RadiusIndex = GUILayout.SelectionGrid(voxelMap.RadiusIndex, radiusNames, 6);
            GUILayout.Label("Stencil");
            voxelMap.StencilIndex = GUILayout.SelectionGrid(voxelMap.StencilIndex, stencilNames, 2);
            GUILayout.EndArea();
        }

        #endregion
    }
}