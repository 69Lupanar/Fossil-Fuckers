using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Models.MarchingSquares
{
    /// <summary>
    /// Cellule de la grille
    /// </summary>
    [Serializable]
    public struct VoxelCell
    {
        #region Propriétés

        public readonly Vector2 AverageNESW => (A.XEdgePoint + A.YEdgePoint + B.YEdgePoint + C.XEdgePoint) * 0.25f;

        public readonly FeaturePoint FeatureSW
        {
            get
            {
                GetSharpFeature(A.XEdgePoint, A.XNormal, A.YEdgePoint, A.YNormal, out FeaturePoint f);
                return f;
            }
        }

        public readonly FeaturePoint FeatureSE
        {
            get
            {
                GetSharpFeature(A.XEdgePoint, A.XNormal, B.YEdgePoint, B.YNormal, out FeaturePoint f);
                return f;
            }
        }

        public readonly FeaturePoint FeatureNW
        {
            get
            {
                GetSharpFeature(A.YEdgePoint, A.YNormal, C.XEdgePoint, C.XNormal, out FeaturePoint f);
                return f;
            }
        }

        public readonly FeaturePoint FeatureNE
        {
            get
            {
                GetSharpFeature(C.XEdgePoint, C.XNormal, B.YEdgePoint, B.YNormal, out FeaturePoint f);
                return f;
            }
        }

        public readonly FeaturePoint FeatureNS
        {
            get
            {
                GetSharpFeature(A.XEdgePoint, A.XNormal, C.XEdgePoint, C.XNormal, out FeaturePoint f);
                return f;
            }
        }

        public readonly FeaturePoint FeatureEW
        {
            get
            {
                GetSharpFeature(A.YEdgePoint, A.YNormal, B.YEdgePoint, B.YNormal, out FeaturePoint f);
                return f;
            }
        }

        public readonly FeaturePoint FeatureNEW
        {
            get
            {
                FeaturePoint.Average(FeatureEW, FeatureNE, FeatureNW, out FeaturePoint f);

                if (!f.Exists)
                    f = new FeaturePoint((A.YEdgePoint + B.YEdgePoint + C.XEdgePoint) / 3f, true);

                return f;
            }
        }

        public readonly FeaturePoint FeatureNSE
        {
            get
            {
                FeaturePoint.Average(FeatureNS, FeatureSE, FeatureNE, out FeaturePoint f);

                if (!f.Exists)
                    f = new FeaturePoint((A.XEdgePoint + B.YEdgePoint + C.XEdgePoint) / 3f, true);

                return f;
            }
        }

        public readonly FeaturePoint FeatureNSW
        {
            get
            {
                FeaturePoint.Average(FeatureNS, FeatureNW, FeatureSW, out FeaturePoint f);

                if (!f.Exists)
                    f = new FeaturePoint((A.XEdgePoint + A.YEdgePoint + C.XEdgePoint) / 3f, true);

                return f;
            }
        }

        public readonly FeaturePoint FeatureSEW
        {
            get
            {
                FeaturePoint.Average(FeatureEW, FeatureSE, FeatureSW, out FeaturePoint f);

                if (!f.Exists)
                    f = new FeaturePoint((A.XEdgePoint + A.YEdgePoint + B.YEdgePoint) / 3f, true);

                return f;
            }
        }

        /// <summary>
        /// Cosinus de la limite autorisée pour un angle d'une section du mesh
        /// </summary>
        public readonly float SharpFeatureLimit { get; }

        /// <summary>
        /// Cosinus de la limite autorisée pour un angle d'une section du mesh
        /// </summary>
        public readonly float ParallelLimit { get; }

        /// <summary>
        /// Index
        /// </summary>
        [field: NonSerialized]
        public int I { get; private set; }

        /// <summary>
        /// Voxel d'un des 4 coins de la cellule
        /// </summary>
        [field: NonSerialized]
        public Voxel A { get; private set; }

        /// <summary>
        /// Voxel d'un des 4 coins de la cellule
        /// </summary>
        [field: NonSerialized]
        public Voxel B { get; private set; }

        /// <summary>
        /// Voxel d'un des 4 coins de la cellule
        /// </summary>
        [field: NonSerialized]
        public Voxel C { get; private set; }

        /// <summary>
        /// Voxel d'un des 4 coins de la cellule
        /// </summary>
        [field: NonSerialized]
        public Voxel D { get; private set; }

        #endregion

        #region Constructeur

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="sharpFeatureLimit">Cosinus de la limite autorisée pour un angle d'une section du mesh</param>
        /// <param name="parallelLimit">Cosinus de la limite autorisée pour un angle d'une section du mesh</param>
        public VoxelCell(float sharpFeatureLimit, float parallelLimit) : this()
        {
            SharpFeatureLimit = sharpFeatureLimit;
            ParallelLimit = parallelLimit;
        }

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Assigne les voxels de la cellule
        /// </summary>
        /// <param name="i">Index de la cellule</param>
        /// <param name="a">Voxel d'un des 4 coins de la cellule</param>
        /// <param name="b">Voxel d'un des 4 coins de la cellule</param>
        /// <param name="c">Voxel d'un des 4 coins de la cellule</param>
        /// <param name="d">Voxel d'un des 4 coins de la cellule</param>
        [BurstCompile]
        public void SetData(int i, in Voxel a, in Voxel b, in Voxel c, in Voxel d)
        {
            I = i;
            A = a;
            B = b;
            C = c;
            D = d;
        }

        [BurstCompile]
        public readonly bool HasConnectionAD(in FeaturePoint fA, in FeaturePoint fD)
        {
            bool flip = A.State < B.State == A.State < C.State;
            if (
                IsParallel(A.XNormal, A.YNormal, flip) ||
                IsParallel(C.XNormal, B.YNormal, flip))
            {
                return true;
            }
            if (fA.Exists)
            {
                if (fD.Exists)
                {
                    if (IsBelowLine(fA.Position, B.YEdgePoint, fD.Position))
                    {
                        if (IsBelowLine(fA.Position, fD.Position, C.XEdgePoint) ||
                            IsBelowLine(fD.Position, fA.Position, A.XEdgePoint))
                        {
                            return true;
                        }
                    }
                    else if (IsBelowLine(fA.Position, fD.Position, C.XEdgePoint) &&
                             IsBelowLine(fD.Position, A.YEdgePoint, fA.Position))
                    {
                        return true;
                    }
                    return false;
                }
                return IsBelowLine(fA.Position, B.YEdgePoint, C.XEdgePoint);
            }
            return fD.Exists &&
                IsBelowLine(fD.Position, A.YEdgePoint, A.XEdgePoint);
        }

        [BurstCompile]
        public readonly bool HasConnectionBC(in FeaturePoint fB, in FeaturePoint fC)
        {
            bool flip = B.State < A.State == B.State < D.State;
            if (
                IsParallel(A.XNormal, B.YNormal, flip) ||
                IsParallel(C.XNormal, A.YNormal, flip))
            {
                return true;
            }
            if (fB.Exists)
            {
                if (fC.Exists)
                {
                    if (IsBelowLine(fC.Position, A.XEdgePoint, fB.Position))
                    {
                        if (IsBelowLine(fC.Position, fB.Position, B.YEdgePoint) ||
                            IsBelowLine(fB.Position, fC.Position, A.YEdgePoint))
                        {
                            return true;
                        }
                    }
                    else if (IsBelowLine(fC.Position, fB.Position, B.YEdgePoint) &&
                             IsBelowLine(fB.Position, C.XEdgePoint, fC.Position))
                    {
                        return true;
                    }
                    return false;
                }
                return IsBelowLine(fB.Position, C.XEdgePoint, A.YEdgePoint);
            }
            return fC.Exists &&
                IsBelowLine(fC.Position, A.XEdgePoint, B.YEdgePoint);
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsInsideABD(float2 point)
        {
            return IsBelowLine(point, A.Position, D.Position);
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsInsideACD(float2 point)
        {
            return IsBelowLine(point, D.Position, A.Position);
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsInsideABC(float2 point)
        {
            return IsBelowLine(point, C.Position, B.Position);
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsInsideBCD(float2 point)
        {
            return IsBelowLine(point, B.Position, C.Position);
        }

        #endregion

        #region Méthodes privées

        [BurstCompile]
        private readonly void GetSharpFeature(float2 p1, float2 n1, float2 p2, float2 n2, out FeaturePoint sharpFeature)
        {
            if (IsSharpFeature(n1, n2))
            {
                float2 pos = GetIntersection(p1, n1, p2, n2);
                sharpFeature = new FeaturePoint(pos, IsInsideCell(pos));
            }
            else
            {
                sharpFeature = FeaturePoint.Empty;
            }
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly bool IsBelowLine(float2 p, float2 start, float2 end)
        {
            return (end.x - start.x) * (p.y - start.y) - (end.y - start.y) * (p.x - start.x) < 0f;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly bool IsSharpFeature(float2 n1, float2 n2)
        {
            float dot = math.dot(n1, -n2);
            return dot >= SharpFeatureLimit && dot < 0.9999f;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly bool IsParallel(float2 n1, float2 n2, bool flip)
        {
            return math.dot(n1, flip ? -n2 : n2) > ParallelLimit;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly float2 GetIntersection(float2 p1, float2 n1, float2 p2, float2 n2)
        {
            float2 d2 = new(-n2.y, n2.x);
            float u2 = -math.dot(n1, p2 - p1) / math.dot(n1, d2);
            return p2 + d2 * u2;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly bool IsInsideCell(float2 point)
        {
            return point.x > A.Position.x && point.y > A.Position.y &&
                   point.x < D.Position.x && point.y < D.Position.y;
        }

        #endregion
    }
}