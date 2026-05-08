using Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils;
using Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Views.MarchingSquares
{
    /// <summary>
    /// Interface permettant d'éditer les propriétés
    /// de la brosse utilisée par le curseur
    /// </summary>
    public sealed class CursorStencilEditView : MonoBehaviour
    {
        #region Variables Unity

        /// <summary>
        /// La couleur/material de remplissage de la brosse active
        /// </summary>
        [SerializeField, Tooltip("La couleur/material de remplissage de la brosse active")]
        private int _materialTypeIndex = 1;

        /// <summary>
        /// Le rayon de la brosse active
        /// </summary>
        [SerializeField, Tooltip("Le rayon de la brosse active")]
        private int _radiusIndex = 0;

        /// <summary>
        /// La forme de la brosse active
        /// </summary>
        [SerializeField, Tooltip("La forme de la brosse active")]
        private int _stencilIndex = 1;

        /// <summary>
        /// true si la visualisation des brosses doit s'aligner avec la grille
        /// </summary>
        [SerializeField, Tooltip("true si la visualisation des brosses doit s'aligner avec la grille")]
        private bool _snapToGrid = false;

        /// <summary>
        /// Noms des types de remplissage des brosses
        /// </summary>
        [SerializeField, Tooltip("Noms des types de remplissage des brosses")]
        private string[] _materialTypeNames = { "X", "A", "B", "C", "D" };

        /// <summary>
        /// Tailles des brosses
        /// </summary>
        [SerializeField, Tooltip("Tailles des brosses")]
        private string[] _radiusNames = { "0", "1", "2", "3", "4", "5" };

        /// <summary>
        /// Types des brosses
        /// </summary>
        [SerializeField, Tooltip("Tailles des brosses")]
        private string[] _stencilNames = { "None", "Square", "Circle" };

        /// <summary>
        /// Les objets 3D représentant les brosses
        /// </summary>
        [SerializeField, Tooltip("Les objets 3D représentant les brosses")]
        private Transform[] _stencilVisualizations;

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
            _gridView = GetComponent<VoxelGridView>();
        }

        /// <summary>
        /// init
        /// </summary>
        private void Start()
        {
            _halfSize = _gridView.GridSize * 0.5f;
            _chunkSize = _gridView.GridSize / _gridView.ChunkResolution;
            _voxelSize = _chunkSize / _gridView.VoxelResolution;
            _gridView.CreateGrid();
        }

        /// <summary>
        /// UI
        /// </summary>
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(4f, 4f, 200f, 500f));
            GUILayout.Label("Fill Type");
            _materialTypeIndex = GUILayout.SelectionGrid(_materialTypeIndex, _materialTypeNames, 5);
            GUILayout.Label("Radius");
            _radiusIndex = GUILayout.SelectionGrid(_radiusIndex, _radiusNames, 6);
            GUILayout.Label("Stencil");
            _stencilIndex = GUILayout.SelectionGrid(_stencilIndex, _stencilNames, 3);

            if (GUILayout.Button("Fill Grid with current Material"))
            {
                _gridView.Fill(_materialTypeIndex);
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// màj à chaque frame
        /// </summary>
        private void Update()
        {
            // Si l'index = 0, aucune brosse n'est active

            if (_stencilIndex == 0)
                return;

            Transform visualization = _stencilVisualizations[_stencilIndex - 1];

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo) &&
                hitInfo.collider.gameObject == gameObject)
            {
                Vector2 center = transform.InverseTransformPoint(hitInfo.point);
                //center.x += _halfSize;
                //center.y += _halfSize;

                if (_snapToGrid)
                {
                    center.x = ((int)(center.x / _voxelSize) + 0.5f) * _voxelSize;
                    center.y = ((int)(center.y / _voxelSize) + 0.5f) * _voxelSize;
                }

                if (Input.GetMouseButton(0))
                {
                    VoxelStencil stencil = _stencils[_stencilIndex - 1];
                    stencil.Initialize(_materialTypeIndex, (_radiusIndex + 0.5f) * _voxelSize);
                    stencil.SetCenter(center.x, center.y);

                    _gridView.ApplyStencil(stencil, transform.InverseTransformPoint(center));
                }

                //center.x -= _halfSize;
                //center.y -= _halfSize;
                visualization.localPosition = center;
                visualization.localScale = Vector3.one * ((_radiusIndex + 0.5f) * _voxelSize * 2f);
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