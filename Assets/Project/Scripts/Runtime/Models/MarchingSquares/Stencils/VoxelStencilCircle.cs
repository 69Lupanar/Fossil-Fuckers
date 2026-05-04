using Unity.Mathematics;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils
{
    /// <summary>
    /// Brosse circulaire
    /// </summary>
    public sealed class VoxelStencilCircle : VoxelStencil
    {
        #region Variables d'instance

        /// <summary>
        /// Carré du rayon
        /// </summary>
        private float sqrRadius;

        #endregion

        #region Méthodes publiques

        /// <inheritdoc/>
        public override void Initialize(int fillType, float radius)
        {
            base.Initialize(fillType, radius);
            sqrRadius = radius * radius;
        }

        /// <inheritdoc/>
        public override void Apply(ref Voxel voxel)
        {
            float x = voxel.Position.x - centerX;
            float y = voxel.Position.y - centerY;

            if (x * x + y * y <= sqrRadius)
                voxel.State = fillType;
        }

        #endregion

        #region Méthodes protégées

        /// <summary>
        /// Calcule l'intersection entre deux voxels
        /// </summary>
        /// <param name="xMin">Voxel gauche</param>
        /// <param name="xMax">Voxel droit</param>
        protected override void FindHorizontalCrossing(ref Voxel xMin, in Voxel xMax)
        {
            float y2 = xMin.Position.y - centerY;
            y2 *= y2;
            if (xMin.State == fillType)
            {
                float x = xMin.Position.x - centerX;
                if (x * x + y2 <= sqrRadius)
                {
                    x = centerX + Mathf.Sqrt(sqrRadius - y2);
                    if (xMin.XEdge == float.MinValue || xMin.XEdge < x)
                    {
                        xMin.XEdge = x;
                        xMin.XNormal = ComputeNormal(x, xMin.Position.y, xMax.State);
                    }
                    else
                    {
                        ValidateHorizontalNormal(ref xMin, in xMax);
                    }
                }
            }
            else if (xMax.State == fillType)
            {
                float x = xMax.Position.x - centerX;
                if (x * x + y2 <= sqrRadius)
                {
                    x = centerX - Mathf.Sqrt(sqrRadius - y2);
                    if (xMin.XEdge == float.MinValue || xMin.XEdge > x)
                    {
                        xMin.XEdge = x;
                        xMin.XNormal = ComputeNormal(x, xMin.Position.y, xMin.State);
                    }
                    else
                    {
                        ValidateHorizontalNormal(ref xMin, in xMax);
                    }
                }
            }
        }

        /// <summary>
        /// Calcule l'intersection entre deux voxels
        /// </summary>
        /// <param name="yMin">Voxel bas</param>
        /// <param name="yMax">Voxel haut</param>
        protected override void FindVerticalCrossing(ref Voxel yMin, in Voxel yMax)
        {
            float x2 = yMin.Position.x - centerX;
            x2 *= x2;
            if (yMin.State == fillType)
            {
                float y = yMin.Position.y - centerY;
                if (y * y + x2 <= sqrRadius)
                {
                    y = centerY + Mathf.Sqrt(sqrRadius - x2);
                    if (yMin.YEdge == float.MinValue || yMin.YEdge < y)
                    {
                        yMin.YEdge = y;
                        yMin.YNormal = ComputeNormal(yMin.Position.x, y, yMax.State);
                    }
                    else
                    {
                        ValidateVerticalNormal(ref yMin, in yMax);
                    }
                }
            }
            else if (yMax.State == fillType)
            {
                float y = yMax.Position.y - centerY;
                if (y * y + x2 <= sqrRadius)
                {
                    y = centerY - Mathf.Sqrt(sqrRadius - x2);
                    if (yMin.YEdge == float.MinValue || yMin.YEdge > y)
                    {
                        yMin.YEdge = y;
                        yMin.YNormal = ComputeNormal(yMin.Position.x, y, yMin.State);
                    }
                    else
                    {
                        ValidateVerticalNormal(ref yMin, in yMax);
                    }
                }
            }
        }

        #endregion

        #region Méthodes privées

        /// <summary>
        /// Obtient la normale aux coordonnées renseignées
        /// </summary>
        private float2 ComputeNormal(float x, float y, int otherState)
        {
            return (fillType > otherState) ? math.normalize(new float2(x - centerX, y - centerY)) : math.normalize(new float2(centerX - x, centerY - y));
        }

        #endregion
    }
}