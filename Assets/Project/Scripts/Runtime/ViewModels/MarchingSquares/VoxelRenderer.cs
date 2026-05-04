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

        public readonly void FillA(in VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddQuadA(cell.I, f.Position);
                if (!cell.C.Filled)
                {
                    _wall.AddFromAC(cell.I, f.Position);
                }
                if (!cell.B.Filled)
                {
                    _wall.AddToAB(cell.I, f.Position);
                }
            }
            else
            {
                _surface.AddTriangleA(cell.I);
                if (!cell.B.Filled)
                {
                    _wall.AddACAB(cell.I);
                }
            }
        }

        public readonly void FillB(in VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddQuadB(cell.I, f.Position);
                if (!cell.A.Filled)
                {
                    _wall.AddFromAB(cell.I, f.Position);
                }
                if (!cell.D.Filled)
                {
                    _wall.AddToBD(cell.I, f.Position);
                }
            }
            else
            {
                _surface.AddTriangleB(cell.I);
                if (!cell.A.Filled)
                {
                    _wall.AddABBD(cell.I);
                }
            }
        }

        public readonly void FillC(in VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddQuadC(cell.I, f.Position);
                if (!cell.D.Filled)
                {
                    _wall.AddFromCD(cell.I, f.Position);
                }
                if (!cell.A.Filled)
                {
                    _wall.AddToAC(cell.I, f.Position);
                }
            }
            else
            {
                _surface.AddTriangleC(cell.I);
                if (!cell.A.Filled)
                {
                    _wall.AddCDAC(cell.I);
                }
            }
        }

        public readonly void FillD(in VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddQuadD(cell.I, f.Position);
                if (!cell.B.Filled)
                {
                    _wall.AddFromBD(cell.I, f.Position);
                }
                if (!cell.C.Filled)
                {
                    _wall.AddToCD(cell.I, f.Position);
                }
            }
            else
            {
                _surface.AddTriangleD(cell.I);
                if (!cell.B.Filled)
                {
                    _wall.AddBDCD(cell.I);
                }
            }
        }

        public readonly void FillABC(in VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddHexagonABC(cell.I, f.Position);
                if (!cell.D.Filled)
                {
                    _wall.AddCDBD(cell.I, f.Position);
                }
            }
            else
            {
                _surface.AddPentagonABC(cell.I);
                if (!cell.D.Filled)
                {
                    _wall.AddCDBD(cell.I);
                }
            }
        }

        public readonly void FillABD(in VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddHexagonABD(cell.I, f.Position);
                if (!cell.C.Filled)
                {
                    _wall.AddACCD(cell.I, f.Position);
                }
            }
            else
            {
                _surface.AddPentagonABD(cell.I);
                if (!cell.C.Filled)
                {
                    _wall.AddACCD(cell.I);
                }
            }
        }

        public readonly void FillACD(in VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddHexagonACD(cell.I, f.Position);
                if (!cell.B.Filled)
                {
                    _wall.AddBDAB(cell.I, f.Position);
                }
            }
            else
            {
                _surface.AddPentagonACD(cell.I);
                if (!cell.B.Filled)
                {
                    _wall.AddBDAB(cell.I);
                }
            }
        }

        public readonly void FillBCD(in VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddHexagonBCD(cell.I, f.Position);
                if (!cell.A.Filled)
                {
                    _wall.AddABAC(cell.I, f.Position);
                }
            }
            else
            {
                _surface.AddPentagonBCD(cell.I);
                if (!cell.A.Filled)
                {
                    _wall.AddABAC(cell.I);
                }
            }
        }

        public readonly void FillAB(in VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddPentagonAB(cell.I, f.Position);
                if (!cell.C.Filled)
                {
                    _wall.AddFromAC(cell.I, f.Position);
                }
                if (!cell.D.Filled)
                {
                    _wall.AddToBD(cell.I, f.Position);
                }
            }
            else
            {
                _surface.AddQuadAB(cell.I);
                if (!cell.C.Filled)
                {
                    _wall.AddACBD(cell.I);
                }
            }
        }

        public readonly void FillAC(in VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddPentagonAC(cell.I, f.Position);
                if (!cell.D.Filled)
                {
                    _wall.AddFromCD(cell.I, f.Position);
                }
                if (!cell.B.Filled)
                {
                    _wall.AddToAB(cell.I, f.Position);
                }
            }
            else
            {
                _surface.AddQuadAC(cell.I);
                if (!cell.B.Filled)
                {
                    _wall.AddCDAB(cell.I);
                }
            }
        }

        public readonly void FillBD(in VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddPentagonBD(cell.I, f.Position);
                if (!cell.A.Filled)
                {
                    _wall.AddFromAB(cell.I, f.Position);
                }
                if (!cell.C.Filled)
                {
                    _wall.AddToCD(cell.I, f.Position);
                }
            }
            else
            {
                _surface.AddQuadBD(cell.I);
                if (!cell.A.Filled)
                {
                    _wall.AddABCD(cell.I);
                }
            }
        }

        public readonly void FillCD(in VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddPentagonCD(cell.I, f.Position);
                if (!cell.B.Filled)
                {
                    _wall.AddFromBD(cell.I, f.Position);
                }
                if (!cell.A.Filled)
                {
                    _wall.AddToAC(cell.I, f.Position);
                }
            }
            else
            {
                _surface.AddQuadCD(cell.I);
                if (!cell.A.Filled)
                {
                    _wall.AddBDAC(cell.I);
                }
            }
        }

        public readonly void FillADToB(in VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddPentagonADToB(cell.I, f.Position);
                if (!cell.B.Filled)
                {
                    _wall.AddBDAB(cell.I, f.Position);
                }
            }
            else
            {
                _surface.AddQuadADToB(cell.I);
                if (!cell.B.Filled)
                {
                    _wall.AddBDAB(cell.I);
                }
            }
        }

        public readonly void FillADToC(in VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddPentagonADToC(cell.I, f.Position);
                if (!cell.C.Filled)
                {
                    _wall.AddACCD(cell.I, f.Position);
                }
            }
            else
            {
                _surface.AddQuadADToC(cell.I);
                if (!cell.C.Filled)
                {
                    _wall.AddACCD(cell.I);
                }
            }
        }

        public readonly void FillBCToA(in VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddPentagonBCToA(cell.I, f.Position);
                if (!cell.A.Filled)
                {
                    _wall.AddABAC(cell.I, f.Position);
                }
            }
            else
            {
                _surface.AddQuadBCToA(cell.I);
                if (!cell.A.Filled)
                {
                    _wall.AddABAC(cell.I);
                }
            }
        }

        public readonly void FillBCToD(in VoxelCell cell, FeaturePoint f)
        {
            if (f.Exists)
            {
                _surface.AddPentagonBCToD(cell.I, f.Position);
                if (!cell.D.Filled)
                {
                    _wall.AddCDBD(cell.I, f.Position);
                }
            }
            else
            {
                _surface.AddQuadBCToD(cell.I);
                if (!cell.D.Filled)
                {
                    _wall.AddCDBD(cell.I);
                }
            }
        }

        public readonly void FillABCD(VoxelCell cell)
        {
            _surface.AddQuadABCD(cell.I);
        }

        #endregion
    }
}