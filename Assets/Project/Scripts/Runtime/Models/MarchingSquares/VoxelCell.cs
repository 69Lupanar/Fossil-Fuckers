using System;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Models.MarchingSquares
{
    /// <summary>
    /// Cellule de la grille
    /// </summary>
    [Serializable]
    public sealed class VoxelCell
    {
        #region Propriétés

        public Vector2 AverageNESW
        {
            get
            {
                return (a.XEdgePoint + a.YEdgePoint +
                        b.YEdgePoint + c.XEdgePoint) * 0.25f;
            }
        }

        public FeaturePoint FeatureSW
        {
            get
            {
                return GetSharpFeature(
                    a.XEdgePoint, a.XNormal, a.YEdgePoint, a.YNormal);
            }
        }

        public FeaturePoint FeatureSE
        {
            get
            {
                return GetSharpFeature(
                    a.XEdgePoint, a.XNormal, b.YEdgePoint, b.YNormal);
            }
        }

        public FeaturePoint FeatureNW
        {
            get
            {
                return GetSharpFeature(
                    a.YEdgePoint, a.YNormal, c.XEdgePoint, c.XNormal);
            }
        }

        public FeaturePoint FeatureNE
        {
            get
            {
                return GetSharpFeature(
                    c.XEdgePoint, c.XNormal, b.YEdgePoint, b.YNormal);
            }
        }

        public FeaturePoint FeatureNS
        {
            get
            {
                return GetSharpFeature(
                    a.XEdgePoint, a.XNormal, c.XEdgePoint, c.XNormal);
            }
        }

        public FeaturePoint FeatureEW
        {
            get
            {
                return GetSharpFeature(
                    a.YEdgePoint, a.YNormal, b.YEdgePoint, b.YNormal);
            }
        }

        public FeaturePoint FeatureNEW
        {
            get
            {
                FeaturePoint f = FeaturePoint.Average(FeatureEW, FeatureNE, FeatureNW);

                if (!f.Exists)
                    f = new FeaturePoint((a.YEdgePoint + b.YEdgePoint + c.XEdgePoint) / 3f, true);

                return f;
            }
        }

        public FeaturePoint FeatureNSE
        {
            get
            {
                FeaturePoint f = FeaturePoint.Average(
                    FeatureNS, FeatureSE, FeatureNE);
                if (!f.Exists)
                    f = new FeaturePoint((a.XEdgePoint + b.YEdgePoint + c.XEdgePoint) / 3f, true);
                return f;
            }
        }

        public FeaturePoint FeatureNSW
        {
            get
            {
                FeaturePoint f = FeaturePoint.Average(
                    FeatureNS, FeatureNW, FeatureSW);
                if (!f.Exists)
                    f = new FeaturePoint((a.XEdgePoint + a.YEdgePoint + c.XEdgePoint) / 3f, true);
                return f;
            }
        }

        public FeaturePoint FeatureSEW
        {
            get
            {
                FeaturePoint f = FeaturePoint.Average(
                    FeatureEW, FeatureSE, FeatureSW);
                if (!f.Exists)
                    f = new FeaturePoint((a.XEdgePoint + a.YEdgePoint + b.YEdgePoint) / 3f, true);
                return f;
            }
        }

        #endregion

        #region Variables d'instance

        /// <summary>
        /// Voxels des 4 coins de la cellule
        /// </summary>
        [NonSerialized]
        public Voxel a, b, c, d;

        /// <summary>
        /// Index
        /// </summary>
        [NonSerialized]
        public int i;

        /// <summary>
        /// Cosinus de la limite autorisée pour un angle d'une section du mesh
        /// </summary>
        public float sharpFeatureLimit, parallelLimit;

        #endregion

        #region Méthodes publiques

        public bool HasConnectionAD(FeaturePoint fA, FeaturePoint fD)
        {
            bool flip = a.State < b.State == a.State < c.State;
            if (
                IsParallel(a.XNormal, a.YNormal, flip) ||
                IsParallel(c.XNormal, b.YNormal, flip))
            {
                return true;
            }
            if (fA.Exists)
            {
                if (fD.Exists)
                {
                    if (IsBelowLine(fA.Position, b.YEdgePoint, fD.Position))
                    {
                        if (IsBelowLine(fA.Position, fD.Position, c.XEdgePoint) ||
                            IsBelowLine(fD.Position, fA.Position, a.XEdgePoint))
                        {
                            return true;
                        }
                    }
                    else if (IsBelowLine(fA.Position, fD.Position, c.XEdgePoint) &&
                             IsBelowLine(fD.Position, a.YEdgePoint, fA.Position))
                    {
                        return true;
                    }
                    return false;
                }
                return IsBelowLine(fA.Position, b.YEdgePoint, c.XEdgePoint);
            }
            return fD.Exists &&
                IsBelowLine(fD.Position, a.YEdgePoint, a.XEdgePoint);
        }

        public bool HasConnectionBC(FeaturePoint fB, FeaturePoint fC)
        {
            bool flip = b.State < a.State == b.State < d.State;
            if (
                IsParallel(a.XNormal, b.YNormal, flip) ||
                IsParallel(c.XNormal, a.YNormal, flip))
            {
                return true;
            }
            if (fB.Exists)
            {
                if (fC.Exists)
                {
                    if (IsBelowLine(fC.Position, a.XEdgePoint, fB.Position))
                    {
                        if (IsBelowLine(fC.Position, fB.Position, b.YEdgePoint) ||
                            IsBelowLine(fB.Position, fC.Position, a.YEdgePoint))
                        {
                            return true;
                        }
                    }
                    else if (IsBelowLine(fC.Position, fB.Position, b.YEdgePoint) &&
                             IsBelowLine(fB.Position, c.XEdgePoint, fC.Position))
                    {
                        return true;
                    }
                    return false;
                }
                return IsBelowLine(fB.Position, c.XEdgePoint, a.YEdgePoint);
            }
            return fC.Exists &&
                IsBelowLine(fC.Position, a.XEdgePoint, b.YEdgePoint);
        }

        public bool IsInsideABD(Vector2 point)
        {
            return IsBelowLine(point, a.Position, d.Position);
        }

        public bool IsInsideACD(Vector2 point)
        {
            return IsBelowLine(point, d.Position, a.Position);
        }

        public bool IsInsideABC(Vector2 point)
        {
            return IsBelowLine(point, c.Position, b.Position);
        }

        public bool IsInsideBCD(Vector2 point)
        {
            return IsBelowLine(point, b.Position, c.Position);
        }

        #endregion

        #region Méthodes privées

        private static bool IsBelowLine(Vector2 p, Vector2 start, Vector2 end)
        {
            float determinant =
                (end.x - start.x) * (p.y - start.y) -
                    (end.y - start.y) * (p.x - start.x);
            return determinant < 0f;
        }

        private FeaturePoint GetSharpFeature(Vector2 p1, Vector2 n1, Vector2 p2, Vector2 n2)
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

        private bool IsSharpFeature(Vector2 n1, Vector2 n2)
        {
            float dot = Vector2.Dot(n1, -n2);
            return dot >= sharpFeatureLimit && dot < 0.9999f;
        }

        private bool IsParallel(Vector2 n1, Vector2 n2, bool flip)
        {
            return Vector2.Dot(n1, flip ? -n2 : n2) > parallelLimit;
        }

        private static Vector2 GetIntersection(Vector2 p1, Vector2 n1, Vector2 p2, Vector2 n2)
        {
            Vector2 d2 = new(-n2.y, n2.x);
            float u2 = -Vector2.Dot(n1, p2 - p1) / Vector2.Dot(n1, d2);
            return p2 + d2 * u2;
        }

        private bool IsInsideCell(Vector2 point)
        {
            return point.x > a.Position.x && point.y > a.Position.y &&
                   point.x < d.Position.x && point.y < d.Position.y;
        }

        #endregion
    }
}