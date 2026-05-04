namespace Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils
{
    /// <summary>
    /// Brosse permettant de modifier plusieurs voxels à la fois
    /// </summary>
    public abstract class VoxelStencil
    {
        #region Propriétés

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public float XStart => CenterX - Radius;

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public float XEnd => CenterX + Radius;

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public float YStart => CenterY - Radius;

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public float YEnd => CenterY + Radius;

        /// <summary>
        /// Type de remplissage de la brosse
        /// </summary>
        protected int FillType { get; private set; }

        /// <summary>
        /// Coord X
        /// </summary>
        protected float CenterX { get; private set; }

        /// <summary>
        /// Coord Y
        /// </summary>
        protected float CenterY { get; private set; }

        /// <summary>
        /// Rayon de la brosse
        /// </summary>
        protected float Radius { get; private set; }

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// init
        /// </summary>
        /// <param name="fillType">Type de remplissage de la brosse</param>
        /// <param name="radius">Rayon de la brosse</param>
        public virtual void Initialize(int fillType, float radius)
        {
            FillType = fillType;
            Radius = radius;
        }

        /// <summary>
        /// Assigne le centre de la brosse
        /// </summary>
        public void SetCenter(float x, float y)
        {
            CenterX = x;
            CenterY = y;
        }

        /// <summary>
        /// Applique le type de remplissage au voxel renseigné
        /// </summary>
        /// <param name="voxel">L'état précédent du voxel</param>
        public abstract void Apply(ref Voxel voxel);

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
        protected abstract void FindHorizontalCrossing(ref Voxel xMin, in Voxel xMax);

        /// <summary>
        /// Calcule l'intersection entre deux voxels
        /// </summary>
        /// <param name="yMin">Voxel bas</param>
        /// <param name="yMax">Voxel haut</param>
        protected abstract void FindVerticalCrossing(ref Voxel yMin, in Voxel yMax);

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