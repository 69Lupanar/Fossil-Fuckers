namespace Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares
{
    /// <summary>
    /// Brosse permettant de changer l'état de plusieurs
    /// voxels à la fois
    /// </summary>
    public class VoxelStencil
    {
        #region Propriétés

        /// <summary>
        /// Limite de la zone rectangulaire englobant la brosse
        /// </summary>
        public int XStart => centerX - radius;

        /// <summary>
        /// Limite de la zone rectangulaire englobant la brosse
        /// </summary>
        public int XEnd => centerX + radius;

        /// <summary>
        /// Limite de la zone rectangulaire englobant la brosse
        /// </summary>
        public int YStart => centerY - radius;

        /// <summary>
        /// Limite de la zone rectangulaire englobant la brosse
        /// </summary>
        public int YEnd => centerY + radius;

        #endregion

        #region Variables d'instance

        /// <summary>
        /// Etat que doit prendre un voxel au passage de la brosse
        /// </summary>
        protected bool fillType;

        /// <summary>
        /// Coord X de la brosse
        /// </summary>
        protected int centerX;

        /// <summary>
        /// Coord Y de la brosse
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
        /// <param name="fillType">Etat que doit prendre un voxel au passage de la brosse</param>
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
        /// Applique un nouvel état à un voxel
        /// </summary>
        /// <param name="x">Coord X</param>
        /// <param name="y">Coord Y</param>
        /// <param name="voxel">Valeur du voxel avant modification</param>
        /// <returns>Le nouvel état du voxel</returns>
        public virtual bool Apply(int x, int y, bool voxel)
        {
            return fillType;
        }

        #endregion
    }
}