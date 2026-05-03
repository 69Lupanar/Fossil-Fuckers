using Assets.Project.Scripts.Runtime.Models.MarchingSquares;
using Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Views.MarchingSquares
{
    /// <summary>
    /// Affiche les voxels à l'écran
    /// </summary>
    public class VoxelGizmosView : MonoBehaviour
    {
        #region Variables Unity

        /// <summary>
        /// La grille de voxels
        /// </summary>
        [SerializeField]
        private VoxelGrid _grid;

        /// <summary>
        /// true pour afficher les gizmos
        /// </summary>
        [SerializeField]
        private bool _showGizmos;

        /// <summary>
        /// true pour utiliser voxel.state comme couleur, false pour utiliser voxel.Filled
        /// </summary>
        [SerializeField]
        [Tooltip("true pour utiliser voxel.state comme couleur, false pour utiliser voxel.Filled")]
        private bool _useVoxelStateAsColors;

        /// <summary>
        /// Couleur d'un voxel vide
        /// </summary>
        [SerializeField]
        [Tooltip("Couleur d'un voxel vide")]
        private Color _emptyVoxelColor = Color.white;

        /// <summary>
        /// Couleur d'un voxel non-vide
        /// </summary>
        [SerializeField]
        [Tooltip("Couleur d'un voxel non-vide")]
        private Color _filledVoxelColor = Color.black;

        /// <summary>
        /// Couleurs associées à chaque état possible de voxel (sauf vide)
        /// </summary>
        [SerializeField]
        [Tooltip("Couleurs associées à chaque état possible de voxel (sauf vide)")]
        private Color[] _voxelStateColors;

        #endregion

        #region Méthodes Unity

        /// <summary>
        /// Affiche les gizmos dans l'éditeur
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!_showGizmos)
                return;

            //float halfSize = _map.mapSize * 0.5f;
            float chunkSize = _grid.MapSize / _grid.ChunkResolution;
            float voxelSize = chunkSize / _grid.VoxelResolution;

            for (int chunkIndex = 0, y = 0; y < _grid.ChunkResolution; ++y)
            {
                for (int x = 0; x < _grid.ChunkResolution; ++x, ++chunkIndex)
                {
                    // Pour chaque chunk...

                    Vector3 chunkPos = new(x * chunkSize/* - halfSize*/, y * chunkSize/* - halfSize*/);

                    for (int voxelIndex = 0, y2 = 0; y2 < _grid.VoxelResolution; ++y2)
                    {
                        for (int x2 = 0; x2 < _grid.VoxelResolution; ++x2, ++voxelIndex)
                        {
                            // Pour chaque voxel...

                            Vector3 localPosition = new((x2 + 0.5f) * voxelSize, (y2 + 0.5f) * voxelSize);
                            Vector3 localScale = _grid.VoxelSpacing * voxelSize * Vector3.one;

                            if (!Application.isPlaying)
                            {
                                Gizmos.color = _emptyVoxelColor;
                            }
                            else
                            {
                                Voxel voxel = _grid.Chunks[chunkIndex].Voxels[voxelIndex];

                                if (_useVoxelStateAsColors)
                                {
                                    Gizmos.color = _voxelStateColors[voxel.state];
                                }
                                else
                                {
                                    Gizmos.color = voxel.Filled ? _filledVoxelColor : _emptyVoxelColor;
                                }
                            }

                            Gizmos.DrawWireCube(chunkPos + localPosition, localScale);
                        }
                    }
                }
            }
        }

        #endregion
    }
}