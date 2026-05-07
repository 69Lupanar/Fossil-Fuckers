using System;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares
{
    /// <summary>
    /// Grille de voxels divisée en chunks
    /// </summary>
    [SelectionBase]
    public class VoxelGrid : MonoBehaviour
    {
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
        public float MaxFeatureAngle { get; private set; } = 135f;

        /// <summary>
        /// Angle max d'une section du mesh qui peut apparaître
        /// </summary>
        [field: SerializeField, Tooltip("Angle max d'une section du mesh qui peut apparaître")]
        public float MaxParallelAngle { get; private set; } = 5f;

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

        #region Méthodes publiques

        /// <summary>
        /// Crée les chunks de la grille
        /// </summary>
        /// <param name="chunkPositions">Positions de chaque chunk</param>
        public void CreateGrid(out Vector3[] chunkPositions)
        {
            _halfSize = GridSize * 0.5f;
            _chunkSize = GridSize / ChunkResolution;
            _voxelSize = _chunkSize / VoxelResolution;
            Chunks = new VoxelChunk[ChunkResolution * ChunkResolution];

            chunkPositions = new Vector3[ChunkResolution * ChunkResolution];
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(GridSize, GridSize);
            box.center = new Vector3(_halfSize, _halfSize);

            for (int i = 0, y = 0; y < ChunkResolution; ++y)
            {
                for (int x = 0; x < ChunkResolution; ++x, ++i)
                {
                    VoxelChunk chunk = new(VoxelResolution, _chunkSize);

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

                    Chunks[i] = chunk;
                    chunkPositions[i] = new Vector3(x * _chunkSize/* - halfSize*/, y * _chunkSize/* - halfSize*/);
                }
            }
        }

        /// <summary>
        /// Remplit la grille entière avec le type de material renseigné
        /// </summary>
        /// <param name="voxelState">Le type de material</param>
        public void Fill(int voxelState)
        {
            for (int i = 0; i < Chunks.Length; ++i)
            {
                for (int j = 0; j < Chunks[i].Voxels.Length; ++j)
                {
                    Chunks[i].Voxels[j].State = voxelState;
                }
            }
        }

        /// <summary>
        /// Màj l'état des voxels affectés par la brosse active
        /// </summary>
        /// <param name="stencil">Brosse utilisée</param>
        /// <param name="chunk">Le chunk</param>
        /// <param name="bounds">Limite de la zone rectangulaire affectée par la brosse</param>
        public void ApplyStencil(VoxelStencil stencil, VoxelChunk chunk, out int4 bounds)
        {
            int xStart = Mathf.Max(0, (int)(stencil.XStart / _voxelSize));
            int xEnd = Mathf.Min((int)(stencil.XEnd / _voxelSize), VoxelResolution - 1);
            int yStart = Mathf.Max(0, (int)(stencil.YStart / _voxelSize));
            int yEnd = Mathf.Min((int)(stencil.YEnd / _voxelSize), VoxelResolution - 1);
            bounds = new int4(xStart, xEnd, yStart, yEnd);

            // On traverse toute la zone rectangulaire englobant la brosse
            // pour modifier les voxels concernés

            for (int y = yStart; y <= yEnd; ++y)
            {
                int i = y * VoxelResolution + xStart;

                for (int x = xStart; x <= xEnd; ++x, ++i)
                {
                    stencil.Apply(ref chunk.Voxels[i]);
                }
            }
        }

        #endregion
    }
}