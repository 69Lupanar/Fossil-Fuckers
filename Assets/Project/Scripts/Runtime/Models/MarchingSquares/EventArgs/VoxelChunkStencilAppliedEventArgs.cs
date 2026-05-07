using Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils;
using Unity.Mathematics;

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
        /// Index du chunk dans la grille
        /// </summary>
        public int ChunkIndex { get; set; }

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
        /// <param name="stencil">La brosse</param>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        /// <param name="bounds">Limite de la zone rectangulaire affectée par la brosse</param>
        public VoxelChunkStencilAppliedEventArgs(VoxelStencil stencil, int chunkIndex, int4 bounds)
        {
            Stencil = stencil;
            ChunkIndex = chunkIndex;
            XStart = bounds.x;
            XEnd = bounds.y;
            YStart = bounds.z;
            YEnd = bounds.w;
        }

        #endregion
    }
}