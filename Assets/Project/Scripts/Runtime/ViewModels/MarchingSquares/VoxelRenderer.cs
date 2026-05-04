using System;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares;
using Unity.Mathematics;

namespace Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares
{
    /// <summary>
    /// Renderers des surfaces et murs
    /// </summary>
    [Serializable]
    public readonly struct VoxelRenderer
    {
        #region Variables d'instance

        /// <summary>
        /// Surface
        /// </summary>
        private readonly VoxelChunkSurface _surface;

        /// <summary>
        /// Mur
        /// </summary>
        private readonly VoxelChunkWall _wall;

        #endregion

        #region Constructeur

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="surface">Surface</param>
        /// <param name="wall">Mur</param>
        public VoxelRenderer(VoxelChunkSurface surface, VoxelChunkWall wall)
        {
            _surface = surface;
            _wall = wall;
        }

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Efface le mesh
        /// </summary>
        public readonly void Clear()
        {
            _surface.Clear();
            _wall.Clear();
        }

        /// <summary>
        /// Assigne au mesh ses nouveaux composants
        /// </summary>
        public readonly void Apply()
        {
            _surface.Apply();
            _wall.Apply();
        }

        /// <summary>
        /// Prépare le cache pour la cellule voisine
        /// </summary>
        public readonly void PrepareCacheForNextCell()
        {
            _surface.PrepareCacheForNextCell();
            _wall.PrepareCacheForNextCell();
        }

        /// <summary>
        /// Prépare le cache pour la ligne voisine
        /// </summary>
        public readonly void PrepareCacheForNextRow()
        {
            _surface.PrepareCacheForNextRow();
            _wall.PrepareCacheForNextRow();
        }

        /// <summary>
        /// Cache le 1er voxel (bas gauche)
        /// </summary>
        public readonly void CacheFirstCorner(float2 voxelPosition)
        {
            _surface.CacheFirstCorner(voxelPosition);
        }

        /// <summary>
        /// Cache le voxel suivant
        /// </summary>
        public readonly void CacheNextCorner(int i, float2 voxelPosition)
        {
            _surface.CacheNextCorner(i, voxelPosition);
        }

        /// <summary>
        /// Met en cache le point sur l'edge X
        /// </summary>
        public readonly void CacheXEdge(int i, float2 xEdgePoint)
        {
            _surface.CacheXEdge(i, xEdgePoint);
        }

        /// <summary>
        /// Met en cache le point sur l'edge X
        /// </summary>
        public readonly void CacheXEdgeWithWall(int i, float2 xEdgePoint, float2 xNormal)
        {
            _surface.CacheXEdge(i, xEdgePoint);
            _wall.CacheXEdge(i, xEdgePoint, xNormal);
        }

        /// <summary>
        /// Met en cache le point sur l'edge Y
        /// </summary>
        public readonly void CacheYEdge(float2 yEdgePoint)
        {
            _surface.CacheYEdge(yEdgePoint);
        }

        /// <summary>
        /// Met en cache le point sur l'edge Y
        /// </summary>
        public readonly void CacheYEdgeWithWall(float2 yEdgePoint, float2 yNormal)
        {
            _surface.CacheYEdge(yEdgePoint);
            _wall.CacheYEdge(yEdgePoint, yNormal);
        }

        public readonly void FillA(VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddQuadA(cell.i, f.Position);
                if (!cell.c.Filled)
                {
                    _wall.AddFromAC(cell.i, f.Position);
                }
                if (!cell.b.Filled)
                {
                    _wall.AddToAB(cell.i, f.Position);
                }
            }
            else
            {
                _surface.AddTriangleA(cell.i);
                if (!cell.b.Filled)
                {
                    _wall.AddACAB(cell.i);
                }
            }
        }

        public readonly void FillB(VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddQuadB(cell.i, f.Position);
                if (!cell.a.Filled)
                {
                    _wall.AddFromAB(cell.i, f.Position);
                }
                if (!cell.d.Filled)
                {
                    _wall.AddToBD(cell.i, f.Position);
                }
            }
            else
            {
                _surface.AddTriangleB(cell.i);
                if (!cell.a.Filled)
                {
                    _wall.AddABBD(cell.i);
                }
            }
        }

        public readonly void FillC(VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddQuadC(cell.i, f.Position);
                if (!cell.d.Filled)
                {
                    _wall.AddFromCD(cell.i, f.Position);
                }
                if (!cell.a.Filled)
                {
                    _wall.AddToAC(cell.i, f.Position);
                }
            }
            else
            {
                _surface.AddTriangleC(cell.i);
                if (!cell.a.Filled)
                {
                    _wall.AddCDAC(cell.i);
                }
            }
        }

        public readonly void FillD(VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddQuadD(cell.i, f.Position);
                if (!cell.b.Filled)
                {
                    _wall.AddFromBD(cell.i, f.Position);
                }
                if (!cell.c.Filled)
                {
                    _wall.AddToCD(cell.i, f.Position);
                }
            }
            else
            {
                _surface.AddTriangleD(cell.i);
                if (!cell.b.Filled)
                {
                    _wall.AddBDCD(cell.i);
                }
            }
        }

        public readonly void FillABC(VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddHexagonABC(cell.i, f.Position);
                if (!cell.d.Filled)
                {
                    _wall.AddCDBD(cell.i, f.Position);
                }
            }
            else
            {
                _surface.AddPentagonABC(cell.i);
                if (!cell.d.Filled)
                {
                    _wall.AddCDBD(cell.i);
                }
            }
        }

        public readonly void FillABD(VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddHexagonABD(cell.i, f.Position);
                if (!cell.c.Filled)
                {
                    _wall.AddACCD(cell.i, f.Position);
                }
            }
            else
            {
                _surface.AddPentagonABD(cell.i);
                if (!cell.c.Filled)
                {
                    _wall.AddACCD(cell.i);
                }
            }
        }

        public readonly void FillACD(VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddHexagonACD(cell.i, f.Position);
                if (!cell.b.Filled)
                {
                    _wall.AddBDAB(cell.i, f.Position);
                }
            }
            else
            {
                _surface.AddPentagonACD(cell.i);
                if (!cell.b.Filled)
                {
                    _wall.AddBDAB(cell.i);
                }
            }
        }

        public readonly void FillBCD(VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddHexagonBCD(cell.i, f.Position);
                if (!cell.a.Filled)
                {
                    _wall.AddABAC(cell.i, f.Position);
                }
            }
            else
            {
                _surface.AddPentagonBCD(cell.i);
                if (!cell.a.Filled)
                {
                    _wall.AddABAC(cell.i);
                }
            }
        }

        public readonly void FillAB(VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddPentagonAB(cell.i, f.Position);
                if (!cell.c.Filled)
                {
                    _wall.AddFromAC(cell.i, f.Position);
                }
                if (!cell.d.Filled)
                {
                    _wall.AddToBD(cell.i, f.Position);
                }
            }
            else
            {
                _surface.AddQuadAB(cell.i);
                if (!cell.c.Filled)
                {
                    _wall.AddACBD(cell.i);
                }
            }
        }

        public readonly void FillAC(VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddPentagonAC(cell.i, f.Position);
                if (!cell.d.Filled)
                {
                    _wall.AddFromCD(cell.i, f.Position);
                }
                if (!cell.b.Filled)
                {
                    _wall.AddToAB(cell.i, f.Position);
                }
            }
            else
            {
                _surface.AddQuadAC(cell.i);
                if (!cell.b.Filled)
                {
                    _wall.AddCDAB(cell.i);
                }
            }
        }

        public readonly void FillBD(VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddPentagonBD(cell.i, f.Position);
                if (!cell.a.Filled)
                {
                    _wall.AddFromAB(cell.i, f.Position);
                }
                if (!cell.c.Filled)
                {
                    _wall.AddToCD(cell.i, f.Position);
                }
            }
            else
            {
                _surface.AddQuadBD(cell.i);
                if (!cell.a.Filled)
                {
                    _wall.AddABCD(cell.i);
                }
            }
        }

        public readonly void FillCD(VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddPentagonCD(cell.i, f.Position);
                if (!cell.b.Filled)
                {
                    _wall.AddFromBD(cell.i, f.Position);
                }
                if (!cell.a.Filled)
                {
                    _wall.AddToAC(cell.i, f.Position);
                }
            }
            else
            {
                _surface.AddQuadCD(cell.i);
                if (!cell.a.Filled)
                {
                    _wall.AddBDAC(cell.i);
                }
            }
        }

        public readonly void FillADToB(VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddPentagonADToB(cell.i, f.Position);
                if (!cell.b.Filled)
                {
                    _wall.AddBDAB(cell.i, f.Position);
                }
            }
            else
            {
                _surface.AddQuadADToB(cell.i);
                if (!cell.b.Filled)
                {
                    _wall.AddBDAB(cell.i);
                }
            }
        }

        public readonly void FillADToC(VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddPentagonADToC(cell.i, f.Position);
                if (!cell.c.Filled)
                {
                    _wall.AddACCD(cell.i, f.Position);
                }
            }
            else
            {
                _surface.AddQuadADToC(cell.i);
                if (!cell.c.Filled)
                {
                    _wall.AddACCD(cell.i);
                }
            }
        }

        public readonly void FillBCToA(VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddPentagonBCToA(cell.i, f.Position);
                if (!cell.a.Filled)
                {
                    _wall.AddABAC(cell.i, f.Position);
                }
            }
            else
            {
                _surface.AddQuadBCToA(cell.i);
                if (!cell.a.Filled)
                {
                    _wall.AddABAC(cell.i);
                }
            }
        }

        public readonly void FillBCToD(VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddPentagonBCToD(cell.i, f.Position);
                if (!cell.d.Filled)
                {
                    _wall.AddCDBD(cell.i, f.Position);
                }
            }
            else
            {
                _surface.AddQuadBCToD(cell.i);
                if (!cell.d.Filled)
                {
                    _wall.AddCDBD(cell.i);
                }
            }
        }

        public readonly void FillABCD(VoxelCell cell)
        {
            _surface.AddQuadABCD(cell.i);
        }

        #endregion
    }
}