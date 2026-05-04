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
        private float _sqrRadius;

        #endregion

        #region Méthodes publiques

        /// <inheritdoc/>
        public sealed override void Initialize(int fillType, float radius)
        {
            base.Initialize(fillType, radius);
            _sqrRadius = radius * radius;
        }

        /// <inheritdoc/>
        public sealed override void Apply(ref Voxel voxel)
        {
            float x = voxel.Position.x - CenterX;
            float y = voxel.Position.y - CenterY;

            if (x * x + y * y <= _sqrRadius)
                voxel.State = FillType;
        }

        #endregion

        #region Méthodes protégées

        /// <inheritdoc/>
        protected sealed override void FindHorizontalCrossing(ref Voxel xMin, in Voxel xMax)
        {
            float y2 = xMin.Position.y - CenterY;
            y2 *= y2;
            if (xMin.State == FillType)
            {
                float x = xMin.Position.x - CenterX;
                if (x * x + y2 <= _sqrRadius)
                {
                    x = CenterX + Mathf.Sqrt(_sqrRadius - y2);
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
            else if (xMax.State == FillType)
            {
                float x = xMax.Position.x - CenterX;
                if (x * x + y2 <= _sqrRadius)
                {
                    x = CenterX - Mathf.Sqrt(_sqrRadius - y2);
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

        /// <inheritdoc/>
        protected sealed override void FindVerticalCrossing(ref Voxel yMin, in Voxel yMax)
        {
            float x2 = yMin.Position.x - CenterX;
            x2 *= x2;
            if (yMin.State == FillType)
            {
                float y = yMin.Position.y - CenterY;
                if (y * y + x2 <= _sqrRadius)
                {
                    y = CenterY + Mathf.Sqrt(_sqrRadius - x2);
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
            else if (yMax.State == FillType)
            {
                float y = yMax.Position.y - CenterY;
                if (y * y + x2 <= _sqrRadius)
                {
                    y = CenterY - Mathf.Sqrt(_sqrRadius - x2);
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
            return (FillType > otherState) ? math.normalize(new float2(x - CenterX, y - CenterY)) : math.normalize(new float2(CenterX - x, CenterY - y));
        }

        #endregion
    }
}