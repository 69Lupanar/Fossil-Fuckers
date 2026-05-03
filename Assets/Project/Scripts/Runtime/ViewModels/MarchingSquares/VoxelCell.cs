using System;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares
{
    /// <summary>
    /// Cellule de la grille
    /// </summary>
    [Serializable]
    public class VoxelCell
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
                    a.XEdgePoint, a.xNormal, a.YEdgePoint, a.yNormal);
            }
        }

        public FeaturePoint FeatureSE
        {
            get
            {
                return GetSharpFeature(
                    a.XEdgePoint, a.xNormal, b.YEdgePoint, b.yNormal);
            }
        }

        public FeaturePoint FeatureNW
        {
            get
            {
                return GetSharpFeature(
                    a.YEdgePoint, a.yNormal, c.XEdgePoint, c.xNormal);
            }
        }

        public FeaturePoint FeatureNE
        {
            get
            {
                return GetSharpFeature(
                    c.XEdgePoint, c.xNormal, b.YEdgePoint, b.yNormal);
            }
        }

        public FeaturePoint FeatureNS
        {
            get
            {
                return GetSharpFeature(
                    a.XEdgePoint, a.xNormal, c.XEdgePoint, c.xNormal);
            }
        }

        public FeaturePoint FeatureEW
        {
            get
            {
                return GetSharpFeature(
                    a.YEdgePoint, a.yNormal, b.YEdgePoint, b.yNormal);
            }
        }

        public FeaturePoint FeatureNEW
        {
            get
            {
                FeaturePoint f = FeaturePoint.Average(
                    FeatureEW, FeatureNE, FeatureNW);
                if (!f.exists)
                {
                    f.position = (a.YEdgePoint + b.YEdgePoint + c.XEdgePoint) / 3f;
                    f.exists = true;
                }
                return f;
            }
        }

        public FeaturePoint FeatureNSE
        {
            get
            {
                FeaturePoint f = FeaturePoint.Average(
                    FeatureNS, FeatureSE, FeatureNE);
                if (!f.exists)
                {
                    f.position = (a.XEdgePoint + b.YEdgePoint + c.XEdgePoint) / 3f;
                    f.exists = true;
                }
                return f;
            }
        }

        public FeaturePoint FeatureNSW
        {
            get
            {
                FeaturePoint f = FeaturePoint.Average(
                    FeatureNS, FeatureNW, FeatureSW);
                if (!f.exists)
                {
                    f.position = (a.XEdgePoint + a.YEdgePoint + c.XEdgePoint) / 3f;
                    f.exists = true;
                }
                return f;
            }
        }

        public FeaturePoint FeatureSEW
        {
            get
            {
                FeaturePoint f = FeaturePoint.Average(
                    FeatureEW, FeatureSE, FeatureSW);
                if (!f.exists)
                {
                    f.position = (a.XEdgePoint + a.YEdgePoint + b.YEdgePoint) / 3f;
                    f.exists = true;
                }
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
            bool flip = (a.state < b.state) == (a.state < c.state);
            if (
                IsParallel(a.xNormal, a.yNormal, flip) ||
                IsParallel(c.xNormal, b.yNormal, flip))
            {
                return true;
            }
            if (fA.exists)
            {
                if (fD.exists)
                {
                    if (IsBelowLine(fA.position, b.YEdgePoint, fD.position))
                    {
                        if (IsBelowLine(fA.position, fD.position, c.XEdgePoint) ||
                            IsBelowLine(fD.position, fA.position, a.XEdgePoint))
                        {
                            return true;
                        }
                    }
                    else if (IsBelowLine(fA.position, fD.position, c.XEdgePoint) &&
                             IsBelowLine(fD.position, a.YEdgePoint, fA.position))
                    {
                        return true;
                    }
                    return false;
                }
                return IsBelowLine(fA.position, b.YEdgePoint, c.XEdgePoint);
            }
            return fD.exists &&
                IsBelowLine(fD.position, a.YEdgePoint, a.XEdgePoint);
        }

        public bool HasConnectionBC(FeaturePoint fB, FeaturePoint fC)
        {
            bool flip = (b.state < a.state) == (b.state < d.state);
            if (
                IsParallel(a.xNormal, b.yNormal, flip) ||
                IsParallel(c.xNormal, a.yNormal, flip))
            {
                return true;
            }
            if (fB.exists)
            {
                if (fC.exists)
                {
                    if (IsBelowLine(fC.position, a.XEdgePoint, fB.position))
                    {
                        if (IsBelowLine(fC.position, fB.position, b.YEdgePoint) ||
                            IsBelowLine(fB.position, fC.position, a.YEdgePoint))
                        {
                            return true;
                        }
                    }
                    else if (IsBelowLine(fC.position, fB.position, b.YEdgePoint) &&
                             IsBelowLine(fB.position, c.XEdgePoint, fC.position))
                    {
                        return true;
                    }
                    return false;
                }
                return IsBelowLine(fB.position, c.XEdgePoint, a.YEdgePoint);
            }
            return fC.exists &&
                IsBelowLine(fC.position, a.XEdgePoint, b.YEdgePoint);
        }

        public bool IsInsideABD(Vector2 point)
        {
            return IsBelowLine(point, a.position, d.position);
        }

        public bool IsInsideACD(Vector2 point)
        {
            return IsBelowLine(point, d.position, a.position);
        }

        public bool IsInsideABC(Vector2 point)
        {
            return IsBelowLine(point, c.position, b.position);
        }

        public bool IsInsideBCD(Vector2 point)
        {
            return IsBelowLine(point, b.position, c.position);
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

        private FeaturePoint GetSharpFeature(
            Vector2 p1, Vector2 n1, Vector2 p2, Vector2 n2)
        {

            FeaturePoint point;
            if (IsSharpFeature(n1, n2))
            {
                point.position = GetIntersection(p1, n1, p2, n2);
                point.exists = IsInsideCell(point.position);
            }
            else
            {
                point.position = Vector2.zero;
                point.exists = false;
            }
            return point;
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

        private static Vector2 GetIntersection(
            Vector2 p1, Vector2 n1, Vector2 p2, Vector2 n2)
        {

            Vector2 d2 = new Vector2(-n2.y, n2.x);
            float u2 = -Vector2.Dot(n1, p2 - p1) / Vector2.Dot(n1, d2);
            return p2 + d2 * u2;
        }

        private bool IsInsideCell(Vector2 point)
        {
            return
                point.x > a.position.x && point.y > a.position.y &&
                    point.x < d.position.x && point.y < d.position.y;
        }

        #endregion
    }
}