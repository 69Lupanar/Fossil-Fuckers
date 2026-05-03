using System;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares.EventArgs;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares
{
    /// <summary>
    /// Chunk contenant une grille de voxels
    /// </summary>
    [SelectionBase]
    public class VoxelChunk : MonoBehaviour
    {
        #region Evénements

        /// <summary>
        /// Appelé une fois le chunk initialisé
        /// </summary>
        public EventHandler<VoxelChunkInitializedEventArgs> OnInitialized { get; set; }

        /// <summary>
        /// Appelé après l'application d'un stencil sur le chunk
        /// </summary>
        public EventHandler<VoxelChunkStencilAppliedEventArgs> OnStencilApplied { get; set; }

        #endregion

        #region Propriétés

        /// <summary>
        /// Voxels
        /// </summary>
        public Voxel[] Voxels { get; private set; }

        /// <summary>
        /// Chunk voisin
        /// </summary>
        public VoxelChunk XNeighbor { get; internal set; }

        /// <summary>
        /// Chunk voisin
        /// </summary>
        public VoxelChunk YNeighbor { get; internal set; }

        /// <summary>
        /// Chunk voisin
        /// </summary>
        public VoxelChunk XYNeighbor { get; internal set; }

        #endregion

        #region Variables d'instance

        /// <summary>
        /// Taille du chunk
        /// </summary>
        private int _voxelResolution;

        /// <summary>
        /// Taille d'un voxel
        /// </summary>
        private float _voxelSize;

        /// <summary>
        /// Taille du chunk
        /// </summary>
        private float _chunkSize;

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// init
        /// </summary>
        /// <param name="voxelResolution">Résolution des voxels pour ce chunk</param>
        /// <param name="chunkSize">Taille du chunk</param>
        /// <param name="maxFeatureAngle">Angle max d'une section du mesh qui peut apparaître</param>
        /// <param name="maxParallelAngle">Angle max d'une section du mesh qui peut apparaître</param>
        public void Initialize(int voxelResolution, float chunkSize, float maxFeatureAngle, float maxParallelAngle)
        {
            _voxelResolution = voxelResolution;
            _chunkSize = chunkSize;
            _voxelSize = chunkSize / voxelResolution;
            Voxels = new Voxel[voxelResolution * voxelResolution];

            for (int i = 0, y = 0; y < voxelResolution; ++y)
            {
                for (int x = 0; x < voxelResolution; ++x, ++i)
                {
                    CreateVoxel(i, x, y);
                }
            }

            OnInitialized?.Invoke(this, new VoxelChunkInitializedEventArgs(voxelResolution, chunkSize, maxFeatureAngle, maxParallelAngle));
        }

        /// <summary>
        /// Modifie l'état d'un voxel
        /// </summary>
        /// <param name="stencil">Brosse utilisée</param>
        public void Apply(VoxelStencil stencil)
        {
            int xStart = Mathf.Max(0, (int)(stencil.XStart / _voxelSize));
            int xEnd = Mathf.Min((int)(stencil.XEnd / _voxelSize), _voxelResolution - 1);
            int yStart = Mathf.Max(0, (int)(stencil.YStart / _voxelSize));
            int yEnd = Mathf.Min((int)(stencil.YEnd / _voxelSize), _voxelResolution - 1);

            // On traverse toute la zone rectangulaire englobant la brosse
            // pour modifier les voxels concernés

            for (int y = yStart; y <= yEnd; ++y)
            {
                int i = y * _voxelResolution + xStart;

                for (int x = xStart; x <= xEnd; ++x, ++i)
                {
                    stencil.Apply(Voxels[i]);
                }
            }

            OnStencilApplied?.Invoke(this, new VoxelChunkStencilAppliedEventArgs(stencil, xStart, xEnd, yStart, yEnd));
        }

        #endregion

        #region Méthodes privées

        /// <summary>
        /// Crée un voxel aux coordonnées renseignées
        /// </summary>
        /// <param name="voxelSpacing">Espacement entre les voxels</param>
        private void CreateVoxel(int i, int x, int y)
        {
            Voxels[i] = new Voxel(x, y, _voxelSize);
        }

        #endregion
    }
}