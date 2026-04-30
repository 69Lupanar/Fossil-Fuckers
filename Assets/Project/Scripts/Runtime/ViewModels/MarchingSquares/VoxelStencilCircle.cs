using UnityEngine;

namespace Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares
{
    /// <summary>
    /// Brosse circulaire
    /// </summary>
    public class VoxelStencilCircle : VoxelStencil
    {
        #region Variables d'instance

        /// <summary>
        /// Carré du rayon
        /// </summary>
        private float sqrRadius;

        #endregion

        #region Méthodes publiques

        /// <inheritdoc/>
        public override void Initialize(bool fillType, float radius)
        {
            base.Initialize(fillType, radius);
            sqrRadius = radius * radius;
        }

        /// <inheritdoc/>
        public override void Apply(Voxel voxel)
        {
            float x = voxel.position.x - centerX;
            float y = voxel.position.y - centerY;

            if (x * x + y * y <= sqrRadius)
            {
                voxel.state = fillType;
            }
        }

        #endregion

        #region Méthodes protégées

        /// <summary>
        /// Calcule l'intersection entre deux voxels
        /// </summary>
        /// <param name="xMin">Voxel gauche</param>
        /// <param name="xMax">Voxel droit</param>
        protected override void FindHorizontalCrossing(Voxel xMin, Voxel xMax)
        {
            float y2 = xMin.position.y - centerY;
            y2 *= y2;
            if (xMin.state == fillType)
            {
                float x = xMin.position.x - centerX;
                if (x * x + y2 <= sqrRadius)
                {
                    x = centerX + Mathf.Sqrt(sqrRadius - y2);
                    if (xMin.xEdge == float.MinValue || xMin.xEdge < x)
                    {
                        xMin.xEdge = x;
                        xMin.xNormal = ComputeNormal(x, xMin.position.y);
                    }
                }
            }
            else if (xMax.state == fillType)
            {
                float x = xMax.position.x - centerX;
                if (x * x + y2 <= sqrRadius)
                {
                    x = centerX - Mathf.Sqrt(sqrRadius - y2);
                    if (xMin.xEdge == float.MinValue || xMin.xEdge > x)
                    {
                        xMin.xEdge = x;
                        xMin.xNormal = ComputeNormal(x, xMin.position.y);
                    }
                }
            }
        }

        /// <summary>
        /// Calcule l'intersection entre deux voxels
        /// </summary>
        /// <param name="yMin">Voxel bas</param>
        /// <param name="yMax">Voxel haut</param>
        protected override void FindVerticalCrossing(Voxel yMin, Voxel yMax)
        {
            float x2 = yMin.position.x - centerX;
            x2 *= x2;
            if (yMin.state == fillType)
            {
                float y = yMin.position.y - centerY;
                if (y * y + x2 <= sqrRadius)
                {
                    y = centerY + Mathf.Sqrt(sqrRadius - x2);
                    if (yMin.yEdge == float.MinValue || yMin.yEdge < y)
                    {
                        yMin.yEdge = y;
                        yMin.yNormal = ComputeNormal(yMin.position.x, y);
                    }
                }
            }
            else if (yMax.state == fillType)
            {
                float y = yMax.position.y - centerY;
                if (y * y + x2 <= sqrRadius)
                {
                    y = centerY - Mathf.Sqrt(sqrRadius - x2);
                    if (yMin.yEdge == float.MinValue || yMin.yEdge > y)
                    {
                        yMin.yEdge = y;
                        yMin.yNormal = ComputeNormal(yMin.position.x, y);
                    }
                }
            }
        }

        #endregion

        #region Méthodes privées

        /// <summary>
        /// Obtient la normale aux coordonnées renseignées
        /// </summary>
        private Vector3 ComputeNormal(float x, float y)
        {
            if (fillType)
            {
                return new Vector2(x - centerX, y - centerY).normalized;
            }
            else
            {
                return new Vector2(centerX - x, centerY - y).normalized;
            }
        }

        #endregion
    }
}