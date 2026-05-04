using Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils;
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
        /// Grille des voxels
        /// </summary>
        [field: SerializeField, Tooltip("Grille des voxels")]
        private VoxelGrid Grid { get; set; }

        /// <summary>
        /// La couleur/material de remplissage de la brosse active
        /// </summary>
        [field: SerializeField, Tooltip("La couleur/material de remplissage de la brosse active")]
        private int MaterialTypeIndex { get; set; } = 1;

        /// <summary>
        /// Le rayon de la brosse active
        /// </summary>
        [field: SerializeField, Tooltip("Le rayon de la brosse active")]
        private int RadiusIndex { get; set; } = 0;

        /// <summary>
        /// La forme de la brosse active
        /// </summary>
        [field: SerializeField, Tooltip("La forme de la brosse active")]
        private int StencilIndex { get; set; } = 1;

        /// <summary>
        /// true si la visualisation des brosses doit s'aligner avec la grille
        /// </summary>
        [field: SerializeField, Tooltip("true si la visualisation des brosses doit s'aligner avec la grille")]
        private bool SnapToGrid { get; set; } = false;

        /// <summary>
        /// Noms des types de remplissage des brosses
        /// </summary>
        [field: SerializeField, Tooltip("Noms des types de remplissage des brosses")]
        private string[] MaterialTypeNames { get; set; } = { "X", "A", "B", "C", "D" };

        /// <summary>
        /// Tailles des brosses
        /// </summary>
        [field: SerializeField, Tooltip("Tailles des brosses")]
        private string[] RadiusNames { get; set; } = { "0", "1", "2", "3", "4", "5" };

        /// <summary>
        /// Types des brosses
        /// </summary>
        [field: SerializeField, Tooltip("Tailles des brosses")]
        private string[] StencilNames { get; set; } = { "None", "Square", "Circle" };

        /// <summary>
        /// Les objets 3D représentant les brosses
        /// </summary>
        [field: SerializeField, Tooltip("Les objets 3D représentant les brosses")]
        private Transform[] StencilVisualizations { get; set; }

        #endregion

        #region Variables d'instance

        /// <summary>
        /// Brosses
        /// </summary>
        private readonly VoxelStencil[] _stencils = { new VoxelStencil(), new VoxelStencilCircle() };

        #endregion

        #region Méthodes Unity

        /// <summary>
        /// UI
        /// </summary>
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(4f, 4f, 150f, 500f));
            GUILayout.Label("Fill Type");
            MaterialTypeIndex = GUILayout.SelectionGrid(MaterialTypeIndex, MaterialTypeNames, 5);
            GUILayout.Label("Radius");
            RadiusIndex = GUILayout.SelectionGrid(RadiusIndex, RadiusNames, 6);
            GUILayout.Label("Stencil");
            StencilIndex = GUILayout.SelectionGrid(StencilIndex, StencilNames, 3);

            if (GUILayout.Button("Fill Grid with current Material"))
            {
                Grid.Fill(MaterialTypeIndex);
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// màj à chaque frame
        /// </summary>
        private void Update()
        {
            // Si l'index = 0, aucune brosse n'est active

            if (StencilIndex == 0)
                return;

            //float halfSize = _grid.MapSize * 0.5f;
            float chunkSize = Grid.GridSize / Grid.ChunkResolution;
            float voxelSize = chunkSize / Grid.VoxelResolution;
            Transform visualization = StencilVisualizations[StencilIndex - 1];

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo) &&
                hitInfo.collider.gameObject == gameObject)
            {
                Vector2 center = transform.InverseTransformPoint(hitInfo.point);
                //center.x += halfSize;
                //center.y += halfSize;

                if (SnapToGrid)
                {
                    center.x = ((int)(center.x / voxelSize) + 0.5f) * voxelSize;
                    center.y = ((int)(center.y / voxelSize) + 0.5f) * voxelSize;
                }

                if (Input.GetMouseButton(0))
                {
                    VoxelStencil stencil = _stencils[StencilIndex - 1];
                    stencil.Initialize(MaterialTypeIndex, (RadiusIndex + 0.5f) * voxelSize);
                    stencil.SetCenter(center.x, center.y);

                    Grid.EditVoxels(stencil, transform.InverseTransformPoint(center));
                }

                //center.x -= halfSize;
                //center.y -= halfSize;
                visualization.localPosition = center;
                visualization.localScale = Vector3.one * ((RadiusIndex + 0.5f) * voxelSize * 2f);
                visualization.gameObject.SetActive(true);
            }
            else
            {
                visualization.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}