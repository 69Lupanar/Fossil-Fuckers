using System.Collections.Generic;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils;
using Unity.Mathematics;
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
        /// GameObject de la grille de voxels
        /// </summary>
        [SerializeField, Tooltip("GameObject de la grille de voxels")]
        private GameObject _voxelGridGO;

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
        private int _stencilShapeIndex = 1;

        /// <summary>
        /// Délai avant de remplir les voxels effacés par la brosse
        /// </summary>
        [SerializeField, Tooltip("Délai avant de remplir les voxels effacés par la brosse")]
        private float _delayBeforeRestore = 3f;

        /// <summary>
        /// true si l'on doit restaurer les voxels effacés après un certain temps
        /// </summary>
        [SerializeField, Tooltip("true si l'on doit restaurer les voxels effacés après un certain temps")]
        private bool _restoreEmptiedVoxels = false;

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
        private VoxelGridView _gridView;

        /// <summary>
        /// Renderer
        /// </summary>
        private VoxelGridMeshRendererView _renderer;

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

        /// <summary>
        /// Délais pour chaque voxel
        /// </summary>
        private readonly List<float> _timersQueue = new();

        /// <summary>
        /// Queue utilisée lors de la restauration
        /// </summary>
        private readonly Queue<int2> _chunkVoxelIDsQueue = new();

        /// <summary>
        /// Queue utilisée lors de la restauration
        /// </summary>
        private readonly Queue<float2> _voxelPosQueue = new();

        /// <summary>
        /// Queue utilisée lors de la restauration
        /// </summary>
        private readonly Queue<Voxel> _voxelsQueue = new();

        /// <summary>
        /// Queue utilisée lors de la restauration
        /// </summary>
        private readonly Queue<int> _voxelStatesQueue = new();

        /// <summary>
        /// Brosses utilisées lors de la restauration
        /// </summary>
        private readonly VoxelStencil _restoreStencil = new VoxelStencilSquare();

        #endregion

        #region Méthodes Unity

        /// <summary>
        /// init
        /// </summary>
        private void Awake()
        {
            _gridView = _voxelGridGO.GetComponent<VoxelGridView>();
            _renderer = _voxelGridGO.GetComponent<VoxelGridMeshRendererView>();
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
            _stencilShapeIndex = GUILayout.SelectionGrid(_stencilShapeIndex, _stencilNames, 3);

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

            if (_stencilShapeIndex == 0)
                return;

            // On restaure les voxels effacés après un certain temps

            RestoreVoxelsOverTime();

            Transform visualization = _stencilVisualizations[_stencilShapeIndex - 1];

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo) &&
                hitInfo.collider.gameObject == _voxelGridGO)
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
                    VoxelStencil stencil = _stencils[_stencilShapeIndex - 1];
                    stencil.Initialize(_materialTypeIndex, (_radiusIndex + 0.5f) * _voxelSize);
                    stencil.SetCenter(center.x, center.y);

                    // Avant d'appliquer la brosse, on enregistre les voxels
                    // que l'on compte restaurer s'ils doivent être effacés

                    if (_restoreEmptiedVoxels && _materialTypeIndex == 0)
                    {
                        RegisterVoxels(stencil, transform.InverseTransformPoint(center));
                        stencil.SetCenter(center.x, center.y);
                    }

                    // On applique la brosse

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

        #region Méthodes privées

        /// <summary>
        /// Restaure les voxels au cours de temps
        /// </summary>
        private void RestoreVoxelsOverTime()
        {
            for (int i = 0; i < _timersQueue.Count; ++i)
            {
                _timersQueue[i] -= Time.deltaTime;
            }

            while (_timersQueue.Count > 0 && _timersQueue[0] <= 0f)
            {
                _timersQueue.RemoveAt(0);
                int state = _voxelStatesQueue.Dequeue();
                float2 voxelPos = _voxelPosQueue.Dequeue();
                int2 ids = _chunkVoxelIDsQueue.Dequeue();
                VoxelChunk chunk = _gridView.Grid.Chunks[ids.x];

                _restoreStencil.Initialize(state, 0.5f * _voxelSize);
                _restoreStencil.SetCenter(voxelPos.x, voxelPos.y);
                _gridView.Grid.ApplyStencil(_restoreStencil, chunk, out int4 bounds);
                _gridView.Grid.Chunks[ids.x].Voxels[ids.y] = _voxelsQueue.Dequeue();    // Offre un peu plus de précision
                _renderer.SetCrossings(_restoreStencil, chunk, ids.x, bounds);
                _renderer.Refresh(chunk, ids.x);
            }
        }

        /// <summary>
        /// Enregistre les voxels pour leur restauration
        /// </summary>
        /// <param name="stencil">La brosse active</param>
        /// <param name="center">Position du curseur</param>
        private void RegisterVoxels(VoxelStencil stencil, Vector3 center)
        {
            int xStart = math.max(0, (int)((stencil.XStart - _voxelSize) / _chunkSize));
            int xEnd = math.min((int)((stencil.XEnd + _voxelSize) / _chunkSize), _gridView.ChunkResolution - 1);
            int yStart = math.max(0, (int)((stencil.YStart - _voxelSize) / _chunkSize));
            int yEnd = math.min((int)((stencil.YEnd + _voxelSize) / _chunkSize), _gridView.ChunkResolution - 1);

            for (int y = yEnd; y >= yStart; --y)
            {
                int chunkIndex = y * _gridView.ChunkResolution + xEnd;

                for (int x = xEnd; x >= xStart; --x, --chunkIndex)
                {
                    VoxelChunk chunk = _gridView.Grid.Chunks[chunkIndex];
                    stencil.SetCenter(center.x - x * _chunkSize, center.y - y * _chunkSize);
                    RegisterVoxels(stencil, chunk, chunkIndex);
                }
            }
        }

        /// <summary>
        /// Enregistre les voxels pour leur restauration
        /// </summary>
        /// <param name="stencil">La brosse active</param>
        /// <param name="chunk">Chunk affecté</param>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        private void RegisterVoxels(VoxelStencil stencil, VoxelChunk chunk, int chunkIndex)
        {
            int xStart = math.max(0, (int)(stencil.XStart / _voxelSize));
            int xEnd = math.min((int)(stencil.XEnd / _voxelSize), _gridView.VoxelResolution - 1);
            int yStart = math.max(0, (int)(stencil.YStart / _voxelSize));
            int yEnd = math.min((int)(stencil.YEnd / _voxelSize), _gridView.VoxelResolution - 1);

            // On traverse toute la zone rectangulaire englobant la brosse
            // pour modifier les voxels concernés

            for (int y = yStart; y <= yEnd; ++y)
            {
                int voxelIndex = y * _gridView.VoxelResolution + xStart;

                for (int x = xStart; x <= xEnd; ++x, ++voxelIndex)
                {
                    Voxel v = chunk.Voxels[voxelIndex];
                    int2 ids = new(chunkIndex, voxelIndex);

                    if (v.State != 0 && !_chunkVoxelIDsQueue.Contains(ids))
                    {
                        _timersQueue.Add(_delayBeforeRestore);
                        _voxelPosQueue.Enqueue(v.Position);
                        _voxelStatesQueue.Enqueue(v.State);
                        _chunkVoxelIDsQueue.Enqueue(ids);
                        _voxelsQueue.Enqueue(v);
                    }
                }
            }
        }

        #endregion
    }
}