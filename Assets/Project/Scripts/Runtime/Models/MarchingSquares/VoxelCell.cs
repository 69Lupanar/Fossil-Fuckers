using System;
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

        public readonly FeaturePoint FeatureSW => GetSharpFeature(A.XEdgePoint, A.XNormal, A.YEdgePoint, A.YNormal);

        public readonly FeaturePoint FeatureSE => GetSharpFeature(A.XEdgePoint, A.XNormal, B.YEdgePoint, B.YNormal);

        public readonly FeaturePoint FeatureNW => GetSharpFeature(A.YEdgePoint, A.YNormal, C.XEdgePoint, C.XNormal);

        public readonly FeaturePoint FeatureNE => GetSharpFeature(C.XEdgePoint, C.XNormal, B.YEdgePoint, B.YNormal);

        public readonly FeaturePoint FeatureNS => GetSharpFeature(A.XEdgePoint, A.XNormal, C.XEdgePoint, C.XNormal);

        public readonly FeaturePoint FeatureEW => GetSharpFeature(A.YEdgePoint, A.YNormal, B.YEdgePoint, B.YNormal);

        public FeaturePoint FeatureNEW
        {
            get
            {
                FeaturePoint f = FeaturePoint.Average(FeatureEW, FeatureNE, FeatureNW);

                if (!f.Exists)
                    f = new FeaturePoint((A.YEdgePoint + B.YEdgePoint + C.XEdgePoint) / 3f, true);

                return f;
            }
        }

        public FeaturePoint FeatureNSE
        {
            get
            {
                FeaturePoint f = FeaturePoint.Average(FeatureNS, FeatureSE, FeatureNE);

                if (!f.Exists)
                    f = new FeaturePoint((A.XEdgePoint + B.YEdgePoint + C.XEdgePoint) / 3f, true);

                return f;
            }
        }

        public FeaturePoint FeatureNSW
        {
            get
            {
                FeaturePoint f = FeaturePoint.Average(FeatureNS, FeatureNW, FeatureSW);

                if (!f.Exists)
                    f = new FeaturePoint((A.XEdgePoint + A.YEdgePoint + C.XEdgePoint) / 3f, true);

                return f;
            }
        }

        public FeaturePoint FeatureSEW
        {
            get
            {
                FeaturePoint f = FeaturePoint.Average(FeatureEW, FeatureSE, FeatureSW);

                if (!f.Exists)
                    f = new FeaturePoint((A.XEdgePoint + A.YEdgePoint + B.YEdgePoint) / 3f, true);

                return f;
            }
        }

        /// <summary>
        /// Cosinus de la limite autorisée pour un angle d'une section du mesh
        /// </summary>
        public float SharpFeatureLimit { get; set; }

        /// <summary>
        /// Cosinus de la limite autorisée pour un angle d'une section du mesh
        /// </summary>
        public float ParallelLimit { get; set; }

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
        public void SetData(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            I = i;
            A = a;
            B = b;
            C = c;
            D = d;
        }

        public readonly bool HasConnectionAD(FeaturePoint fA, FeaturePoint fD)
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

        public readonly bool HasConnectionBC(FeaturePoint fB, FeaturePoint fC)
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

        public readonly bool IsInsideABD(Vector2 point)
        {
            return IsBelowLine(point, A.Position, D.Position);
        }

        public readonly bool IsInsideACD(Vector2 point)
        {
            return IsBelowLine(point, D.Position, A.Position);
        }

        public readonly bool IsInsideABC(Vector2 point)
        {
            return IsBelowLine(point, C.Position, B.Position);
        }

        public readonly bool IsInsideBCD(Vector2 point)
        {
            return IsBelowLine(point, B.Position, C.Position);
        }

        #endregion

        #region Méthodes privées

        private readonly bool IsBelowLine(Vector2 p, Vector2 start, Vector2 end)
        {
            return (end.x - start.x) * (p.y - start.y) - (end.y - start.y) * (p.x - start.x) < 0f;
        }

        private readonly FeaturePoint GetSharpFeature(Vector2 p1, Vector2 n1, Vector2 p2, Vector2 n2)
        {
            if (IsSharpFeature(n1, n2))
            {
                Vector2 pos = GetIntersection(p1, n1, p2, n2);
                return new FeaturePoint(pos, IsInsideCell(pos));
            }
            else
            {
                return FeaturePoint.Empty;
            }
        }

        private readonly bool IsSharpFeature(Vector2 n1, Vector2 n2)
        {
            float dot = Vector2.Dot(n1, -n2);
            return dot >= SharpFeatureLimit && dot < 0.9999f;
        }

        private readonly bool IsParallel(Vector2 n1, Vector2 n2, bool flip)
        {
            return Vector2.Dot(n1, flip ? -n2 : n2) > ParallelLimit;
        }

        private readonly Vector2 GetIntersection(Vector2 p1, Vector2 n1, Vector2 p2, Vector2 n2)
        {
            Vector2 d2 = new(-n2.y, n2.x);
            float u2 = -Vector2.Dot(n1, p2 - p1) / Vector2.Dot(n1, d2);
            return p2 + d2 * u2;
        }

        private readonly bool IsInsideCell(Vector2 point)
        {
            return point.x > A.Position.x && point.y > A.Position.y &&
                   point.x < D.Position.x && point.y < D.Position.y;
        }

        #endregion
    }
}