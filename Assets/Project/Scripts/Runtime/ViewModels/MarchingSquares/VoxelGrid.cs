using System;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares.EventArgs;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares
{
    /// <summary>
    /// Grille de voxels divisée en chunks
    /// </summary>
    [SelectionBase]
    public class VoxelGrid : MonoBehaviour
    {
        #region Evénements

        /// <summary>
        /// Appelé une fois les chunks créés
        /// </summary>
        public EventHandler<VoxelChunkInitializedEventArgs> OnChunksCreated { get; set; }

        /// <summary>
        /// Appelé après l'application d'un stencil sur le chunk
        /// </summary>
        public EventHandler<VoxelChunkStencilAppliedEventArgs> OnStencilApplied { get; set; }

        #endregion

        #region Propriétés

        /// <summary>
        /// Le type de remplissage de la brosse active
        /// </summary>
        public int FillTypeIndex { get; set; } = 1;

        /// <summary>
        /// Le rayon de la brosse active
        /// </summary>
        public int RadiusIndex { get; set; }

        /// <summary>
        /// Le type de la brosse active
        /// </summary>
        public int StencilIndex { get; set; }

        /// <summary>
        /// Chunks
        /// </summary>
        public VoxelChunk[] Chunks { get; private set; }

        #endregion

        #region Variables Unity

        /// <summary>
        /// Nombre de chunks par dimension de la carte
        /// </summary>
        [field: SerializeField, Tooltip("Nombres de chunks par dimensions de la carte")]
        public float MapSize { get; private set; } = 2f;

        /// <summary>
        /// Nombre de voxels par dimension de la carte
        /// </summary>
        [field: SerializeField, Tooltip("Nombre de voxels par dimension de la carte")]
        public int VoxelResolution { get; private set; } = 8;

        /// <summary>
        /// Espacement entre les voxels
        /// </summary>
        [field: SerializeField, Tooltip("Espacement entre les voxels"), Range(0f, 1f)]
        public float VoxelSpacing { get; private set; } = .1f;

        /// <summary>
        /// Taille d'un chunk
        /// </summary>
        [field: SerializeField, Tooltip("Taille d'un chunk")]
        public int ChunkResolution { get; private set; } = 2;

        /// <summary>
        /// Angle max d'une section du mesh qui peut apparaître
        /// </summary>
        [field: SerializeField, Tooltip("Angle max d'une section du mesh qui peut apparaître")]
        public float MaxFeatureAngle { get; private set; } = 135f;

        /// <summary>
        /// Angle max d'une section du mesh qui peut apparaître
        /// </summary>
        [field: SerializeField, Tooltip("Angle max d'une section du mesh qui peut apparaître")]
        public float MaxParallelAngle { get; private set; } = 5f;

        /// <summary>
        /// Les objets 3D représentant les brosses
        /// </summary>
        [field: SerializeField, Tooltip("Les objets 3D représentant les brosses")]
        public Transform[] StencilVisualizations { get; private set; }

        /// <summary>
        /// true si la visualisation des brosses doit s'aligner avec la grille
        /// </summary>
        [field: SerializeField, Tooltip("true si la visualisation des brosses doit s'aligner avec la grille")]
        public bool SnapToGrid { get; private set; } = false;

        #endregion

        #region Variables d'instance

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
        /// Brosses
        /// </summary>
        private VoxelStencil[] _stencils = { new VoxelStencil(), new VoxelStencilCircle() };

        #endregion

        #region Méthodes Unity

        /// <summary>
        /// init
        /// </summary>
        private void Start()
        {
            _halfSize = MapSize * 0.5f;
            _chunkSize = MapSize / ChunkResolution;
            _voxelSize = _chunkSize / VoxelResolution;
            Chunks = new VoxelChunk[ChunkResolution * ChunkResolution];
            Vector3[] chunkPositions = new Vector3[ChunkResolution * ChunkResolution];
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(MapSize, MapSize);
            box.center = new Vector3(_halfSize, _halfSize);

            for (int i = 0, y = 0; y < ChunkResolution; ++y)
            {
                for (int x = 0; x < ChunkResolution; ++x, ++i)
                {
                    Chunks[i] = CreateChunk(i, x, y);
                    chunkPositions[i] = new Vector3(x * _chunkSize/* - halfSize*/, y * _chunkSize/* - halfSize*/);
                }
            }

            OnChunksCreated?.Invoke(this, new VoxelChunkInitializedEventArgs(chunkPositions, VoxelResolution, _chunkSize, MaxFeatureAngle, MaxParallelAngle));
        }

        /// <summary>
        /// màj à chaque frame
        /// </summary>
        private void Update()
        {
            Transform visualization = StencilVisualizations[StencilIndex];

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo) &&
                hitInfo.collider.gameObject == gameObject)
            {
                Vector2 center = transform.InverseTransformPoint(hitInfo.point);
                //center.x += halfSize;
                //center.y += halfSize;
                if (SnapToGrid)
                {
                    center.x = ((int)(center.x / _voxelSize) + 0.5f) * _voxelSize;
                    center.y = ((int)(center.y / _voxelSize) + 0.5f) * _voxelSize;
                }

                if (Input.GetMouseButton(0))
                {
                    EditVoxels(transform.InverseTransformPoint(center));
                }

                //center.x -= halfSize;
                //center.y -= halfSize;
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

        /// <summary>
        /// Crée un chunk à partir des coordonnées renseignées
        /// </summary>
        /// <param name="i"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private VoxelChunk CreateChunk(int i, int x, int y)
        {
            VoxelChunk chunk = new(VoxelResolution, _chunkSize);
            Chunks[i] = chunk;

            if (x > 0)
            {
                Chunks[i - 1].XNeighbor = chunk;
            }
            if (y > 0)
            {
                Chunks[i - ChunkResolution].YNeighbor = chunk;

                if (x > 0)
                {
                    Chunks[i - ChunkResolution - 1].XYNeighbor = chunk;
                }
            }

            return chunk;
        }

        /// <summary>
        /// Màj l'état des voxels affectéspar la brosse active
        /// </summary>
        /// <param name="center">Position du curseur</param>
        private void EditVoxels(Vector3 center)
        {
            VoxelStencil activeStencil = _stencils[StencilIndex];
            activeStencil.Initialize(FillTypeIndex, (RadiusIndex + 0.5f) * _voxelSize);
            activeStencil.SetCenter(center.x, center.y);

            int xStart = Mathf.Max(0, (int)((activeStencil.XStart - _voxelSize) / _chunkSize));
            int xEnd = Mathf.Min((int)((activeStencil.XEnd + _voxelSize) / _chunkSize), ChunkResolution - 1);
            int yStart = Mathf.Max(0, (int)((activeStencil.YStart - _voxelSize) / _chunkSize));
            int yEnd = Mathf.Min((int)((activeStencil.YEnd + _voxelSize) / _chunkSize), ChunkResolution - 1);

            for (int y = yEnd; y >= yStart; --y)
            {
                int chunkIndex = y * ChunkResolution + xEnd;

                for (int x = xEnd; x >= xStart; --x, --chunkIndex)
                {
                    activeStencil.SetCenter(center.x - x * _chunkSize, center.y - y * _chunkSize);
                    Apply(activeStencil, Chunks[chunkIndex], chunkIndex);
                }
            }
        }

        /// <summary>
        /// Màj l'état des voxels affectéspar la brosse active
        /// </summary>
        /// <param name="stencil">Brosse utilisée</param>
        /// <param name="chunk">Le chunk</param>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        public void Apply(VoxelStencil stencil, VoxelChunk chunk, int chunkIndex)
        {
            int xStart = Mathf.Max(0, (int)(stencil.XStart / _voxelSize));
            int xEnd = Mathf.Min((int)(stencil.XEnd / _voxelSize), VoxelResolution - 1);
            int yStart = Mathf.Max(0, (int)(stencil.YStart / _voxelSize));
            int yEnd = Mathf.Min((int)(stencil.YEnd / _voxelSize), VoxelResolution - 1);

            // On traverse toute la zone rectangulaire englobant la brosse
            // pour modifier les voxels concernés

            for (int y = yStart; y <= yEnd; ++y)
            {
                int i = y * VoxelResolution + xStart;

                for (int x = xStart; x <= xEnd; ++x, ++i)
                {
                    stencil.Apply(chunk.Voxels[i]);
                }
            }

            OnStencilApplied?.Invoke(this, new VoxelChunkStencilAppliedEventArgs(stencil, chunkIndex, xStart, xEnd, yStart, yEnd));
        }
    }
}