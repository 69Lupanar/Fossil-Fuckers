using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Models.MarchingSquares.EventArgs
{
    /// <summary>
    /// Infos sur l'initialisation d'un chunk
    /// </summary>
    public class VoxelChunkInitializedEventArgs : System.EventArgs
    {
        #region Propriétés

        /// <summary>
        /// Positions de chaque chunk
        /// </summary>
        public Vector3[] ChunkPositions { get; }

        /// <summary>
        /// Résolution des voxels pour ce chunk
        /// </summary>
        public int VoxelResolution { get; }

        /// <summary>
        /// Taille du chunk
        /// </summary>
        public float ChunkSize { get; }

        /// <summary>
        /// Angle max d'une section du mesh qui peut apparaître
        /// </summary>
        public float MaxFeatureAngle { get; set; }

        /// <summary>
        /// Angle max d'une section du mesh qui peut apparaître
        /// </summary>
        public float MaxParallelAngle { get; set; }

        #endregion

        #region Constructeur

        /// <summary>
        /// Constructeur
        /// </summary>4<param name="chunkPositions">Positions de chaque chunk</param>
        /// <param name="voxelResolution">Résolution des voxels pour ce chunk</param>
        /// <param name="chunkSize">Taille du chunk</param>
        /// <param name="maxFeatureAngle">Angle max d'une section du mesh qui peut apparaître</param>
        /// <param name="maxParallelAngle">Angle max d'une section du mesh qui peut apparaître</param>
        public VoxelChunkInitializedEventArgs(Vector3[] chunkPositions, int voxelResolution, float chunkSize, float maxFeatureAngle, float maxParallelAngle)
        {
            ChunkPositions = chunkPositions;
            VoxelResolution = voxelResolution;
            ChunkSize = chunkSize;
            MaxFeatureAngle = maxFeatureAngle;
            MaxParallelAngle = maxParallelAngle;
        }

        #endregion
    }
}