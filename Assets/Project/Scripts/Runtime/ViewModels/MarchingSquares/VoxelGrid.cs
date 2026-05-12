using Assets.Project.Scripts.Runtime.Models.MarchingSquares;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares
{
    /// <summary>
    /// Grille de voxels divisée en chunks
    /// </summary>
    public sealed class VoxelGrid
    {
        #region Propriétés

        /// <summary>
        /// Chunks
        /// </summary>
        public VoxelChunk[] Chunks { get; private set; }

        /// <summary>
        /// Nombre de chunks par dimension de la carte
        /// </summary>
        public int GridSize { get; }

        /// <summary>
        /// Taille d'un chunk
        /// </summary>
        public int ChunkResolution { get; }

        /// <summary>
        /// Nombre de voxels par dimension de la carte
        /// </summary>
        public int VoxelResolution { get; }

        #endregion

        #region Variables d'instance

        /// <summary>
        /// Taille d'un chunk
        /// </summary>
        private readonly float _chunkSize;

        /// <summary>
        /// Taille d'un voxel
        /// </summary>
        private readonly float _voxelSize;

        #endregion

        #region Constructeur

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="gridSize">Taille de la grille</param>
        /// <param name="chunkResolution"> Nombre de voxels par dimension de la carte</param>
        /// <param name="voxelResolution">Taille d'un chunk</param>
        public VoxelGrid(int gridSize, int chunkResolution, int voxelResolution)
        {
            GridSize = gridSize;
            ChunkResolution = chunkResolution;
            VoxelResolution = voxelResolution;
            _chunkSize = GridSize / ChunkResolution;
            _voxelSize = _chunkSize / VoxelResolution;
        }

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Crée les chunks de la grille
        /// </summary>
        /// <param name="chunkPositions">Positions de chaque chunk</param>
        [BurstCompile, SkipLocalsInit]
        public void CreateGrid(out Vector3[] chunkPositions)
        {
            Chunks = new VoxelChunk[ChunkResolution * ChunkResolution];
            chunkPositions = new Vector3[ChunkResolution * ChunkResolution];
            //float halfSize = GridSize * 0.5f;

            for (int i = 0, y = 0; y < ChunkResolution; ++y)
            {
                for (int x = 0; x < ChunkResolution; ++x, ++i)
                {
                    VoxelChunk chunk = new(VoxelResolution, _voxelSize);

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
        [BurstCompile]
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
            int xStart = math.max(0, (int)(stencil.XStart / _voxelSize));
            int xEnd = math.min((int)(stencil.XEnd / _voxelSize), VoxelResolution - 1);
            int yStart = math.max(0, (int)(stencil.YStart / _voxelSize));
            int yEnd = math.min((int)(stencil.YEnd / _voxelSize), VoxelResolution - 1);
            bounds = new int4(xStart, xEnd, yStart, yEnd);

            // On traverse toute la zone rectangulaire englobant la brosse
            // pour modifier les voxels concernés

            for (int y = yStart; y <= yEnd; ++y)
            {
                int voxelIndex = y * VoxelResolution + xStart;

                for (int x = xStart; x <= xEnd; ++x, ++voxelIndex)
                {
                    stencil.Apply(ref chunk.Voxels[voxelIndex]);
                }
            }
        }

        #endregion
    }
}