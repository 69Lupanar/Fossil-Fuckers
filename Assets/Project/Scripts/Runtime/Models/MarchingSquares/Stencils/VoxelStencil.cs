using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils
{
    /// <summary>
    /// Brosse permettant de modifier plusieurs voxels à la fois
    /// </summary>
    public class VoxelStencil
    {
        #region Propriétés

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public float XStart => centerX - radius;

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public float XEnd => centerX + radius;

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public float YStart => centerY - radius;

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public float YEnd => centerY + radius;

        #endregion

        #region Variables d'instance

        /// <summary>
        /// Type de remplissage de la brosse
        /// </summary>
        protected int fillType;

        /// <summary>
        /// Coord X
        /// </summary>
        protected float centerX;

        /// <summary>
        /// Coord Y
        /// </summary>
        protected float centerY;

        /// <summary>
        /// Rayon de la brosse
        /// </summary>
        protected float radius;

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// init
        /// </summary>
        /// <param name="fillType">Type de remplissage de la brosse</param>
        /// <param name="radius">Rayon de la brosse</param>
        public virtual void Initialize(int fillType, float radius)
        {
            this.fillType = fillType;
            this.radius = radius;
        }

        /// <summary>
        /// Assigne le centre de la brosse
        /// </summary>
        public virtual void SetCenter(float x, float y)
        {
            centerX = x;
            centerY = y;
        }

        /// <summary>
        /// Applique le type de remplissage au voxel renseigné
        /// </summary>
        /// <param name="voxel">L'état précédent du voxel</param>
        public virtual void Apply(Voxel voxel)
        {
            Vector2 p = voxel.Position;

            if (p.x >= XStart && p.x <= XEnd && p.y >= YStart && p.y <= YEnd)
                voxel.State = fillType;
        }

        /// <summary>
        /// Calcule l'intersection entre deux voxels
        /// </summary>
        /// <param name="xMin">Voxel gauche</param>
        /// <param name="xMax">Voxel droit</param>
        public void SetHorizontalCrossing(Voxel xMin, Voxel xMax)
        {
            if (xMin.State != xMax.State)
                FindHorizontalCrossing(xMin, xMax);
            else
            {
                xMin.XEdge = float.MinValue;
            }
        }

        /// <summary>
        /// Calcule l'intersection entre deux voxels
        /// </summary>
        /// <param name="yMin">Voxel bas</param>
        /// <param name="yMax">Voxel haut</param>
        public void SetVerticalCrossing(Voxel yMin, Voxel yMax)
        {
            if (yMin.State != yMax.State)
                FindVerticalCrossing(yMin, yMax);
            else
            {
                yMin.YEdge = float.MinValue;
            }
        }

        #endregion

        #region Méthodes protégées

        /// <summary>
        /// Calcule l'intersection entre deux voxels
        /// </summary>
        /// <param name="xMin">Voxel gauche</param>
        /// <param name="xMax">Voxel droit</param>
        protected virtual void FindHorizontalCrossing(Voxel xMin, Voxel xMax)
        {
            if (xMin.Position.y < YStart || xMin.Position.y > YEnd)
                return;
            if (xMin.State == fillType)
            {
                if (xMin.Position.x <= XEnd && xMax.Position.x >= XEnd)
                {
                    if (xMin.XEdge == float.MinValue || xMin.XEdge < XEnd)
                    {
                        xMin.XEdge = XEnd;
                        xMin.XNormal = new Vector2(fillType > xMax.State ? 1f : -1f, 0f);
                    }
                    else
                    {
                        ValidateHorizontalNormal(xMin, xMax);
                    }
                }
            }
            else if (xMax.State == fillType)
            {
                if (xMin.Position.x <= XStart && xMax.Position.x >= XStart)
                {
                    if (xMin.XEdge == float.MinValue || xMin.XEdge > XStart)
                    {
                        xMin.XEdge = XStart;
                        xMin.XNormal = new Vector2(fillType > xMin.State ? -1f : 1f, 0f);
                    }
                    else
                    {
                        ValidateHorizontalNormal(xMin, xMax);
                    }
                }
            }
        }

        /// <summary>
        /// Assure que la normale pointe dans la bonne direction
        /// </summary>
        protected static void ValidateHorizontalNormal(Voxel xMin, Voxel xMax)
        {
            if (xMin.State < xMax.State)
            {
                if (xMin.XNormal.x > 0f)
                    xMin.XNormal = -xMin.XNormal;
            }
            else if (xMin.XNormal.x < 0f)
            {
                xMin.XNormal = -xMin.XNormal;
            }
        }

        /// <summary>
        /// Calcule l'intersection entre deux voxels
        /// </summary>
        /// <param name="yMin">Voxel bas</param>
        /// <param name="yMax">Voxel haut</param>
        protected virtual void FindVerticalCrossing(Voxel yMin, Voxel yMax)
        {
            if (yMin.Position.x < XStart || yMin.Position.x > XEnd)
                return;
            if (yMin.State == fillType)
            {
                if (yMin.Position.y <= YEnd && yMax.Position.y >= YEnd)
                {
                    if (yMin.YEdge == float.MinValue || yMin.YEdge < YEnd)
                    {
                        yMin.YEdge = YEnd;
                        yMin.YNormal = new Vector2(0f, fillType > yMax.State ? 1f : -1f);
                    }
                    else
                    {
                        ValidateVerticalNormal(yMin, yMax);
                    }
                }
            }
            else if (yMax.State == fillType)
            {
                if (yMin.Position.y <= YStart && yMax.Position.y >= YStart)
                {
                    if (yMin.YEdge == float.MinValue || yMin.YEdge > YStart)
                    {
                        yMin.YEdge = YStart;
                        yMin.YNormal = new Vector2(0f, fillType > yMin.State ? -1f : 1f);
                    }
                    else
                    {
                        ValidateVerticalNormal(yMin, yMax);
                    }
                }
            }
        }

        /// <summary>
        /// Assure que la normale pointe dans la bonne direction
        /// </summary>
        protected static void ValidateVerticalNormal(Voxel yMin, Voxel yMax)
        {
            if (yMin.State < yMax.State)
            {
                if (yMin.YNormal.y > 0f)
                    yMin.YNormal = -yMin.YNormal;
            }
            else if (yMin.YNormal.y < 0f)
            {
                yMin.YNormal = -yMin.YNormal;
            }
        }

        #endregion
    }
}