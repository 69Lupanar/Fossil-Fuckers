using System.Collections.Generic;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils;
using Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Views.MarchingSquares
{
    /// <summary>
    /// Interface de la grille de voxels
    /// </summary>
    public class VoxelGridView : MonoBehaviour
    {
        #region Variables d'instance

        /// <summary>
        /// Grille des voxels
        /// </summary>
        private VoxelGrid _grid;

        /// <summary>
        /// Le renderer
        /// </summary>
        private VoxelGridMeshRendererView _renderer;

        /// <summary>
        /// Taille d'un chunk
        /// </summary>
        private float _chunkSize;

        /// <summary>
        /// Taille d'un voxel
        /// </summary>
        private float _voxelSize;

        /// <summary>
        /// Chunks à màj après l'application d'un stencil
        /// </summary>
        private readonly List<VoxelChunk> _chunksToRefresh = new();

        /// <summary>
        /// Chunks à màj après l'application d'un stencil
        /// </summary>
        private readonly List<int> _chunksIDsToRefresh = new();

        #endregion

        #region Méthodes Unity

        /// <summary>
        /// init
        /// </summary>
        private void Awake()
        {
            _grid = GetComponent<VoxelGrid>();
            _renderer = GetComponent<VoxelGridMeshRendererView>();
        }

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// init
        /// </summary>
        [BurstCompile]
        public void CreateGrid()
        {
            _chunkSize = _grid.GridSize / _grid.ChunkResolution;
            _voxelSize = _chunkSize / _grid.VoxelResolution;
            _grid.CreateGrid(out Vector3[] chunkPositions);
            _renderer.Initialize(chunkPositions);
        }

        /// <summary>
        /// Remplit la grille entière avec le type de material renseigné
        /// </summary>
        /// <param name="voxelState">Le type de material</param>
        [BurstCompile]
        public void Fill(int voxelState)
        {
            _grid.Fill(voxelState);
            EmptyAllDeadCells();
            _renderer.Fill();
        }

        /// <summary>
        /// Màj l'état des voxels affectés par la brosse active
        /// </summary>
        /// <param name="stencil">La brosse active</param>
        /// <param name="center">Position du curseur</param>
        public void ApplyStencil(VoxelStencil stencil, Vector3 center)
        {
            int xStart = Mathf.Max(0, (int)((stencil.XStart - _voxelSize) / _chunkSize));
            int xEnd = Mathf.Min((int)((stencil.XEnd + _voxelSize) / _chunkSize), _grid.ChunkResolution - 1);
            int yStart = Mathf.Max(0, (int)((stencil.YStart - _voxelSize) / _chunkSize));
            int yEnd = Mathf.Min((int)((stencil.YEnd + _voxelSize) / _chunkSize), _grid.ChunkResolution - 1);
            _chunksToRefresh.Clear();
            _chunksIDsToRefresh.Clear();

            for (int y = yEnd; y >= yStart; --y)
            {
                int chunkIndex = y * _grid.ChunkResolution + xEnd;

                for (int x = xEnd; x >= xStart; --x, --chunkIndex)
                {
                    VoxelChunk chunk = _grid.Chunks[chunkIndex];
                    stencil.SetCenter(center.x - x * _chunkSize, center.y - y * _chunkSize);
                    _grid.ApplyStencil(stencil, chunk, out int4 bounds);
                    _renderer.SetCrossings(stencil, chunk, chunkIndex, bounds);

                    if (!_chunksIDsToRefresh.Contains(chunkIndex))
                    {
                        _chunksToRefresh.Add(chunk);
                        _chunksIDsToRefresh.Add(chunkIndex);
                    }
                }
            }

            VoxelStencil deadStencil = new VoxelStencilSquare();
            deadStencil.Initialize(0, 0.5f * _voxelSize);

            for (int i = 0; i < _chunksIDsToRefresh.Count; ++i)
            {
                EmptyDeadCellsInChunk(deadStencil, _chunksToRefresh[i], _chunksIDsToRefresh[i]);
                _renderer.Refresh(_chunksToRefresh[i], _chunksIDsToRefresh[i]);
            }
        }

        #endregion

        #region Méthodes privées

        /// <summary>
        /// Vide les voxels morts
        /// </summary>
        private void EmptyAllDeadCells()
        {
            VoxelStencil stencil = new VoxelStencilSquare();
            stencil.Initialize(0, 0.5f * _voxelSize);

            for (int chunkIndex = 0, y = 0; y < _grid.ChunkResolution; ++y)
            {
                for (int x = 0; x < _grid.ChunkResolution; ++x, ++chunkIndex)
                {
                    VoxelChunk chunk = _grid.Chunks[chunkIndex];
                    EmptyDeadCellsInChunk(stencil, chunk, chunkIndex);
                }
            }
        }

        /// <summary>
        /// Vide les voxels morts
        /// </summary>
        private void EmptyDeadCellsInChunk(VoxelStencil deadStencil, VoxelChunk chunk, int chunkIndex)
        {
            foreach (float2 pos in chunk.DeadPositions)
            {
                Vector3 center = new(pos.x, pos.y);
                deadStencil.SetCenter(center.x, center.y);
                _grid.ApplyStencil(deadStencil, chunk, out int4 bounds);
                _renderer.SetCrossings(deadStencil, chunk, chunkIndex, bounds);
            }
        }

        #endregion
    }
}