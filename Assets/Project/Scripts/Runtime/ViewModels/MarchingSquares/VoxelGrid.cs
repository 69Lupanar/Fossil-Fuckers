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
        /// Chunks
        /// </summary>
        public VoxelChunk[] Chunks { get; private set; }

        #endregion

        #region Variables Unity

        /// <summary>
        /// Nombre de chunks par dimension de la carte
        /// </summary>
        [field: SerializeField, Tooltip("Nombres de chunks par dimensions de la carte")]
        public float GridSize { get; private set; } = 2f;

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
        private float MaxFeatureAngle { get; set; } = 135f;

        /// <summary>
        /// Angle max d'une section du mesh qui peut apparaître
        /// </summary>
        [field: SerializeField, Tooltip("Angle max d'une section du mesh qui peut apparaître")]
        private float MaxParallelAngle { get; set; } = 5f;

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

        #endregion

        #region Méthodes Unity

        /// <summary>
        /// init
        /// </summary>
        private void Start()
        {
            _halfSize = GridSize * 0.5f;
            _chunkSize = GridSize / ChunkResolution;
            _voxelSize = _chunkSize / VoxelResolution;
            Chunks = new VoxelChunk[ChunkResolution * ChunkResolution];
            Vector3[] chunkPositions = new Vector3[ChunkResolution * ChunkResolution];
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(GridSize, GridSize);
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

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Màj l'état des voxels affectéspar la brosse active
        /// </summary>
        /// <param name="stencil">La brosse active</param>
        /// <param name="center">Position du curseur</param>
        public void EditVoxels(VoxelStencil stencil, Vector3 center)
        {
            int xStart = Mathf.Max(0, (int)((stencil.XStart - _voxelSize) / _chunkSize));
            int xEnd = Mathf.Min((int)((stencil.XEnd + _voxelSize) / _chunkSize), ChunkResolution - 1);
            int yStart = Mathf.Max(0, (int)((stencil.YStart - _voxelSize) / _chunkSize));
            int yEnd = Mathf.Min((int)((stencil.YEnd + _voxelSize) / _chunkSize), ChunkResolution - 1);

            for (int y = yEnd; y >= yStart; --y)
            {
                int chunkIndex = y * ChunkResolution + xEnd;

                for (int x = xEnd; x >= xStart; --x, --chunkIndex)
                {
                    stencil.SetCenter(center.x - x * _chunkSize, center.y - y * _chunkSize);
                    Apply(stencil, Chunks[chunkIndex], chunkIndex);
                }
            }
        }

        #endregion

        #region Méthodes privées

        /// <summary>
        /// Crée un chunk à partir des coordonnées renseignées
        /// </summary>
        /// <param name="i">Index du chunk dans la grille</param>
        /// <param name="x">Position X</param>
        /// <param name="y">Position Y</param>
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
        /// <param name="stencil">Brosse utilisée</param>
        /// <param name="chunk">Le chunk</param>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        private void Apply(VoxelStencil stencil, VoxelChunk chunk, int chunkIndex)
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

        #endregion
    }
}