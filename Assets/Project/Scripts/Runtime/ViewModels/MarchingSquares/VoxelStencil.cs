using UnityEngine;

namespace Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares
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
            Vector2 p = voxel.position;

            if (p.x >= XStart && p.x <= XEnd && p.y >= YStart && p.y <= YEnd)
            {
                voxel.state = fillType;
            }
        }

        /// <summary>
        /// Calcule l'intersection entre deux voxels
        /// </summary>
        /// <param name="xMin">Voxel gauche</param>
        /// <param name="xMax">Voxel droit</param>
        public void SetHorizontalCrossing(Voxel xMin, Voxel xMax)
        {
            if (xMin.state != xMax.state)
            {
                FindHorizontalCrossing(xMin, xMax);
            }
            else
            {
                xMin.xEdge = float.MinValue;
            }
        }

        /// <summary>
        /// Calcule l'intersection entre deux voxels
        /// </summary>
        /// <param name="yMin">Voxel bas</param>
        /// <param name="yMax">Voxel haut</param>
        public void SetVerticalCrossing(Voxel yMin, Voxel yMax)
        {
            if (yMin.state != yMax.state)
            {
                FindVerticalCrossing(yMin, yMax);
            }
            else
            {
                yMin.yEdge = float.MinValue;
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
            if (xMin.position.y < YStart || xMin.position.y > YEnd)
            {
                return;
            }
            if (xMin.state == fillType)
            {
                if (xMin.position.x <= XEnd && xMax.position.x >= XEnd)
                {
                    if (xMin.xEdge == float.MinValue || xMin.xEdge < XEnd)
                    {
                        xMin.xEdge = XEnd;
                        xMin.xNormal = new Vector2(fillType > xMax.state ? 1f : -1f, 0f);
                    }
                    else
                    {
                        ValidateHorizontalNormal(xMin, xMax);
                    }
                }
            }
            else if (xMax.state == fillType)
            {
                if (xMin.position.x <= XStart && xMax.position.x >= XStart)
                {
                    if (xMin.xEdge == float.MinValue || xMin.xEdge > XStart)
                    {
                        xMin.xEdge = XStart;
                        xMin.xNormal = new Vector2(fillType > xMin.state ? -1f : 1f, 0f);
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
            if (xMin.state < xMax.state)
            {
                if (xMin.xNormal.x > 0f)
                {
                    xMin.xNormal = -xMin.xNormal;
                }
            }
            else if (xMin.xNormal.x < 0f)
            {
                xMin.xNormal = -xMin.xNormal;
            }
        }

        /// <summary>
        /// Calcule l'intersection entre deux voxels
        /// </summary>
        /// <param name="yMin">Voxel bas</param>
        /// <param name="yMax">Voxel haut</param>
        protected virtual void FindVerticalCrossing(Voxel yMin, Voxel yMax)
        {
            if (yMin.position.x < XStart || yMin.position.x > XEnd)
            {
                return;
            }
            if (yMin.state == fillType)
            {
                if (yMin.position.y <= YEnd && yMax.position.y >= YEnd)
                {
                    if (yMin.yEdge == float.MinValue || yMin.yEdge < YEnd)
                    {
                        yMin.yEdge = YEnd;
                        yMin.yNormal = new Vector2(0f, fillType > yMax.state ? 1f : -1f);
                    }
                    else
                    {
                        ValidateVerticalNormal(yMin, yMax);
                    }
                }
            }
            else if (yMax.state == fillType)
            {
                if (yMin.position.y <= YStart && yMax.position.y >= YStart)
                {
                    if (yMin.yEdge == float.MinValue || yMin.yEdge > YStart)
                    {
                        yMin.yEdge = YStart;
                        yMin.yNormal = new Vector2(0f, fillType > yMin.state ? -1f : 1f);
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
            if (yMin.state < yMax.state)
            {
                if (yMin.yNormal.y > 0f)
                {
                    yMin.yNormal = -yMin.yNormal;
                }
            }
            else if (yMin.yNormal.y < 0f)
            {
                yMin.yNormal = -yMin.yNormal;
            }
        }

        #endregion
    }
}