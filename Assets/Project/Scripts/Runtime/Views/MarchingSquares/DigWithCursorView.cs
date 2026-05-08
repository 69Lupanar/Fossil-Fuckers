using Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils;
using Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Views.MarchingSquares
{
    /// <summary>
    /// Interface permettant d'éditer les propriétés
    /// de la brosse utilisée par le curseur
    /// </summary>
    public class DigWithCursorView : MonoBehaviour
    {
        #region Variables Unity

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
        /// Grille des voxels
        /// </summary>
        private VoxelGrid _grid;

        /// <summary>
        /// Grille des voxels
        /// </summary>
        private VoxelGridView _gridView;

        /// <summary>
        /// Brosses
        /// </summary>
        private readonly VoxelStencil[] _stencils = { new VoxelStencilSquare(), new VoxelStencilCircle() };

        /// <summary>
        /// Taille d'un chunk
        /// </summary>
        private float _chunkSize;

        /// <summary>
        /// Taille d'un voxel
        /// </summary>
        private float _voxelSize;

        /// <summary>
        /// Moitié de la taille de la grille
        /// </summary>
        private float _halfSize;

        #endregion

        #region Méthodes Unity

        /// <summary>
        /// init
        /// </summary>
        private void Awake()
        {
            _grid = GetComponent<VoxelGrid>();
            _gridView = GetComponent<VoxelGridView>();
        }

        /// <summary>
        /// init
        /// </summary>
        private void Start()
        {
            _halfSize = _grid.GridSize * 0.5f;
            _chunkSize = _grid.GridSize / _grid.ChunkResolution;
            _voxelSize = _chunkSize / _grid.VoxelResolution;
            _gridView.CreateGrid();
        }

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
                _gridView.Fill(MaterialTypeIndex);
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

            Transform visualization = StencilVisualizations[StencilIndex - 1];

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo) &&
                hitInfo.collider.gameObject == gameObject)
            {
                Vector2 center = transform.InverseTransformPoint(hitInfo.point);
                //center.x += _halfSize;
                //center.y += _halfSize;

                if (SnapToGrid)
                {
                    center.x = ((int)(center.x / _voxelSize) + 0.5f) * _voxelSize;
                    center.y = ((int)(center.y / _voxelSize) + 0.5f) * _voxelSize;
                }

                if (Input.GetMouseButton(0))
                {
                    VoxelStencil stencil = _stencils[StencilIndex - 1];
                    stencil.Initialize(MaterialTypeIndex, (RadiusIndex + 0.5f) * _voxelSize);
                    stencil.SetCenter(center.x, center.y);

                    _gridView.ApplyStencil(stencil, transform.InverseTransformPoint(center));
                }

                //center.x -= _halfSize;
                //center.y -= _halfSize;
                visualization.localPosition = center;
                visualization.localScale = Vector3.one * ((RadiusIndex + 0.5f) * _voxelSize * 2f);
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