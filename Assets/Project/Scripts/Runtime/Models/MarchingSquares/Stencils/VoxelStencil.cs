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
        public float XStart => _centerX - _radius;

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public float XEnd => _centerX + _radius;

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public float YStart => _centerY - _radius;

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public float YEnd => _centerY + _radius;

        #endregion

        #region Variables d'instance

        /// <summary>
        /// Type de remplissage de la brosse
        /// </summary>
        protected int _fillType;

        /// <summary>
        /// Coord X
        /// </summary>
        protected float _centerX;

        /// <summary>
        /// Coord Y
        /// </summary>
        protected float _centerY;

        /// <summary>
        /// Rayon de la brosse
        /// </summary>
        protected float _radius;

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// init
        /// </summary>
        /// <param name="fillType">Type de remplissage de la brosse</param>
        /// <param name="radius">Rayon de la brosse</param>
        public virtual void Initialize(int fillType, float radius)
        {
            _fillType = fillType;
            _radius = radius;
        }

        /// <summary>
        /// Assigne le centre de la brosse
        /// </summary>
        public virtual void SetCenter(float x, float y)
        {
            _centerX = x;
            _centerY = y;
        }

        /// <summary>
        /// Applique le type de remplissage au voxel renseigné
        /// </summary>
        /// <param name="voxel">L'état précédent du voxel</param>
        public virtual void Apply(ref Voxel voxel)
        {
            Vector2 p = voxel.Position;

            if (p.x >= XStart && p.x <= XEnd && p.y >= YStart && p.y <= YEnd)
                voxel.State = _fillType;
        }

        /// <summary>
        /// Calcule l'intersection entre deux voxels
        /// </summary>
        /// <param name="xMin">Voxel gauche</param>
        /// <param name="xMax">Voxel droit</param>
        public void SetHorizontalCrossing(ref Voxel xMin, in Voxel xMax)
        {
            if (xMin.State != xMax.State)
                FindHorizontalCrossing(ref xMin, in xMax);
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
        public void SetVerticalCrossing(ref Voxel yMin, in Voxel yMax)
        {
            if (yMin.State != yMax.State)
                FindVerticalCrossing(ref yMin, in yMax);
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
        protected virtual void FindHorizontalCrossing(ref Voxel xMin, in Voxel xMax)
        {
            if (xMin.Position.y < YStart || xMin.Position.y > YEnd)
                return;
            if (xMin.State == _fillType)
            {
                if (xMin.Position.x <= XEnd && xMax.Position.x >= XEnd)
                {
                    if (xMin.XEdge == float.MinValue || xMin.XEdge < XEnd)
                    {
                        xMin.XEdge = XEnd;
                        xMin.XNormal = new Vector2(_fillType > xMax.State ? 1f : -1f, 0f);
                    }
                    else
                    {
                        ValidateHorizontalNormal(ref xMin, in xMax);
                    }
                }
            }
            else if (xMax.State == _fillType)
            {
                if (xMin.Position.x <= XStart && xMax.Position.x >= XStart)
                {
                    if (xMin.XEdge == float.MinValue || xMin.XEdge > XStart)
                    {
                        xMin.XEdge = XStart;
                        xMin.XNormal = new Vector2(_fillType > xMin.State ? -1f : 1f, 0f);
                    }
                    else
                    {
                        ValidateHorizontalNormal(ref xMin, in xMax);
                    }
                }
            }
        }

        /// <summary>
        /// Assure que la normale pointe dans la bonne direction
        /// </summary>
        protected static void ValidateHorizontalNormal(ref Voxel xMin, in Voxel xMax)
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
        protected virtual void FindVerticalCrossing(ref Voxel yMin, in Voxel yMax)
        {
            if (yMin.Position.x < XStart || yMin.Position.x > XEnd)
                return;
            if (yMin.State == _fillType)
            {
                if (yMin.Position.y <= YEnd && yMax.Position.y >= YEnd)
                {
                    if (yMin.YEdge == float.MinValue || yMin.YEdge < YEnd)
                    {
                        yMin.YEdge = YEnd;
                        yMin.YNormal = new Vector2(0f, _fillType > yMax.State ? 1f : -1f);
                    }
                    else
                    {
                        ValidateVerticalNormal(ref yMin, in yMax);
                    }
                }
            }
            else if (yMax.State == _fillType)
            {
                if (yMin.Position.y <= YStart && yMax.Position.y >= YStart)
                {
                    if (yMin.YEdge == float.MinValue || yMin.YEdge > YStart)
                    {
                        yMin.YEdge = YStart;
                        yMin.YNormal = new Vector2(0f, _fillType > yMin.State ? -1f : 1f);
                    }
                    else
                    {
                        ValidateVerticalNormal(ref yMin, in yMax);
                    }
                }
            }
        }

        /// <summary>
        /// Assure que la normale pointe dans la bonne direction
        /// </summary>
        protected static void ValidateVerticalNormal(ref Voxel yMin, in Voxel yMax)
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