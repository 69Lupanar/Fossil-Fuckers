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
        private int sqrRadius;

        #endregion

        #region Méthodes publiques

        /// <inheritdoc/>
        public override void Initialize(bool fillType, int radius)
        {
            base.Initialize(fillType, radius);
            sqrRadius = radius * radius;
        }

        /// <inheritdoc/>
        public override bool Apply(int x, int y, bool voxel)
        {
            x -= centerX;
            y -= centerY;

            if (x * x + y * y <= sqrRadius)
                return fillType;

            return voxel;
        }

        #endregion
    }
}