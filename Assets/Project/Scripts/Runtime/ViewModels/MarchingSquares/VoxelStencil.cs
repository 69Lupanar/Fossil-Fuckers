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
        public int XStart => centerX - radius;

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public int XEnd => centerX + radius;

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public int YStart => centerY - radius;

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public int YEnd => centerY + radius;

        #endregion

        #region Variables d'instance

        /// <summary>
        /// Type de remplissage de la brosse
        /// </summary>
        protected bool fillType;

        /// <summary>
        /// Coord X
        /// </summary>
        protected int centerX;

        /// <summary>
        /// Coord Y
        /// </summary>
        protected int centerY;

        /// <summary>
        /// Rayon de la brosse
        /// </summary>
        protected int radius;

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// init
        /// </summary>
        /// <param name="fillType">Type de remplissage de la brosse</param>
        /// <param name="radius">Rayon de la brosse</param>
        public virtual void Initialize(bool fillType, int radius)
        {
            this.fillType = fillType;
            this.radius = radius;
        }

        /// <summary>
        /// Assigne le centre de la brosse
        /// </summary>
        public virtual void SetCenter(int x, int y)
        {
            centerX = x;
            centerY = y;
        }

        /// <summary>
        /// Applique le type de remplissage au voxel renseigné
        /// </summary>
        /// <param name="voxel">L'état précédent du voxel</param>
        public virtual bool Apply(int x, int y, bool voxel)
        {
            return fillType;
        }

        #endregion
    }
}