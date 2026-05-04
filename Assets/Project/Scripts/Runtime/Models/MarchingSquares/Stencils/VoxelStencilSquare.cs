using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils
{
    /// <summary>
    /// Brosse carrée
    /// </summary>
    public sealed class VoxelStencilSquare : VoxelStencil
    {
        #region Méthodes publiques

        /// <inheritdoc/>
        public sealed override void Apply(ref Voxel voxel)
        {
            Vector2 p = voxel.Position;

            if (p.x >= XStart && p.x <= XEnd && p.y >= YStart && p.y <= YEnd)
                voxel.State = FillType;
        }

        #endregion

        #region Méthodes protégées

        /// <inheritdoc/>
        protected sealed override void FindHorizontalCrossing(ref Voxel xMin, in Voxel xMax)
        {
            if (xMin.Position.y < YStart || xMin.Position.y > YEnd)
                return;

            if (xMin.State == FillType)
            {
                if (xMin.Position.x <= XEnd && xMax.Position.x >= XEnd)
                {
                    if (xMin.XEdge == float.MinValue || xMin.XEdge < XEnd)
                    {
                        xMin.XEdge = XEnd;
                        xMin.XNormal = new Vector2(FillType > xMax.State ? 1f : -1f, 0f);
                    }
                    else
                    {
                        ValidateHorizontalNormal(ref xMin, in xMax);
                    }
                }
            }
            else if (xMax.State == FillType)
            {
                if (xMin.Position.x <= XStart && xMax.Position.x >= XStart)
                {
                    if (xMin.XEdge == float.MinValue || xMin.XEdge > XStart)
                    {
                        xMin.XEdge = XStart;
                        xMin.XNormal = new Vector2(FillType > xMin.State ? -1f : 1f, 0f);
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
            if (yMin.Position.x < XStart || yMin.Position.x > XEnd)
                return;

            if (yMin.State == FillType)
            {
                if (yMin.Position.y <= YEnd && yMax.Position.y >= YEnd)
                {
                    if (yMin.YEdge == float.MinValue || yMin.YEdge < YEnd)
                    {
                        yMin.YEdge = YEnd;
                        yMin.YNormal = new Vector2(0f, FillType > yMax.State ? 1f : -1f);
                    }
                    else
                    {
                        ValidateVerticalNormal(ref yMin, in yMax);
                    }
                }
            }
            else if (yMax.State == FillType)
            {
                if (yMin.Position.y <= YStart && yMax.Position.y >= YStart)
                {
                    if (yMin.YEdge == float.MinValue || yMin.YEdge > YStart)
                    {
                        yMin.YEdge = YStart;
                        yMin.YNormal = new Vector2(0f, FillType > yMin.State ? -1f : 1f);
                    }
                    else
                    {
                        ValidateVerticalNormal(ref yMin, in yMax);
                    }
                }
            }
        }

        #endregion
    }
}