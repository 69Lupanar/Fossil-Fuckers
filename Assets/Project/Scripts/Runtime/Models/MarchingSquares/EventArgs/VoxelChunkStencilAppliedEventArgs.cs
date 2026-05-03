using Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils;

namespace Assets.Project.Scripts.Runtime.Models.MarchingSquares.EventArgs
{
    /// <summary>
    /// Infos sur la màj d'un chunk par un stencil
    /// </summary>
    public class VoxelChunkStencilAppliedEventArgs : System.EventArgs
    {
        #region Propriétés

        /// <summary>
        /// Brosse utilisée
        /// </summary>
        public VoxelStencil Stencil { get; }

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public int XStart { get; set; }

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public int XEnd { get; set; }

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public int YStart { get; set; }

        /// <summary>
        /// Limite de la zone rectangulaire affectée par la brosse
        /// </summary>
        public int YEnd { get; set; }

        #endregion

        #region Constructeur

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="xStart">Limite de la zone rectangulaire affectée par la brosse</param>
        /// <param name="xEnd">Limite de la zone rectangulaire affectée par la brosse</param>
        /// <param name="yStart">Limite de la zone rectangulaire affectée par la brosse</param>
        /// <param name="yEnd">Limite de la zone rectangulaire affectée par la brosse</param>
        public VoxelChunkStencilAppliedEventArgs(VoxelStencil stencil, int xStart, int xEnd, int yStart, int yEnd)
        {
            Stencil = stencil;
            XStart = xStart;
            XEnd = xEnd;
            YStart = yStart;
            YEnd = yEnd;
        }

        #endregion
    }
}