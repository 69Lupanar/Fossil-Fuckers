using System.Collections.Generic;
using Unity.Mathematics;

namespace Assets.Project.Scripts.Runtime.Models.MarchingSquares
{
    /// <summary>
    /// Chunk contenant une grille de voxels
    /// </summary>
    public sealed class VoxelChunk
    {
        #region Propriétés

        /// <summary>
        /// Voxels
        /// </summary>
        public Voxel[] Voxels { get; private set; }

        /// <summary>
        /// Positions des voxels morts
        /// </summary>
        public List<float2> DeadPositions { get; private set; }

        /// <summary>
        /// Chunk voisin
        /// </summary>
        public VoxelChunk XNeighbor { get; set; }

        /// <summary>
        /// Chunk voisin
        /// </summary>
        public VoxelChunk YNeighbor { get; set; }

        /// <summary>
        /// Chunk voisin
        /// </summary>
        public VoxelChunk XYNeighbor { get; set; }

        #endregion

        #region Constructeur

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="voxelResolution">Résolution des voxels pour ce chunk</param>
        /// <param name="voxelSize">Taille du voxel</param>
        public VoxelChunk(int voxelResolution, float voxelSize)
        {
            Voxels = new Voxel[voxelResolution * voxelResolution];
            DeadPositions = new List<float2>();

            for (int i = 0, y = 0; y < voxelResolution; ++y)
            {
                for (int x = 0; x < voxelResolution; ++x, ++i)
                {
                    Voxels[i] = new Voxel(x, y, voxelSize);
                }
            }
        }

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Marque les voxels aux positions renseignées comme morts
        /// </summary>
        /// <param name="deadVoxelsPositions">Les positions des voxels morts</param>
        public void SetDeadVoxels(IEnumerable<float2> deadVoxelsPositions)
        {
            DeadPositions.AddRange(deadVoxelsPositions);
        }

        #endregion
    }
}