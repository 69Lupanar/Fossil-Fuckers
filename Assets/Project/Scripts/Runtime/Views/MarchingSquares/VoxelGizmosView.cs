using Assets.Project.Scripts.Runtime.Models.MarchingSquares;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Views.MarchingSquares
{
    /// <summary>
    /// Affiche les voxels à l'écran
    /// </summary>
    public sealed class VoxelGizmosView : MonoBehaviour
    {
        #region Variables Unity

        /// <summary>
        /// La grille de voxels
        /// </summary>
        [SerializeField]
        private VoxelGridView _gridView;

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
        /// Espacement entre les voxels
        /// </summary>
        [SerializeField, Tooltip("Espacement entre les voxels"), Range(0f, 1f)]
        private float _voxelSpacing = .1f;

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
        /// Couleur d'un voxel mort
        /// </summary>
        [SerializeField]
        [Tooltip("Couleur d'un voxel mort")]
        private Color _deadVoxelColor = new(255f, 0f, 255f);

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
        private void OnDrawGizmos()
        {
            if (!_showGizmos || _gridView == null)
                return;

            //float halfSize = _gridView.GridSize * 0.5f;
            float chunkSize = _gridView.GridSize / _gridView.ChunkResolution;
            float voxelSize = chunkSize / _gridView.VoxelResolution;

            for (int chunkIndex = 0, y = 0; y < _gridView.ChunkResolution; ++y)
            {
                for (int x = 0; x < _gridView.ChunkResolution; ++x, ++chunkIndex)
                {
                    // Pour chaque chunk...

                    Vector3 chunkPos = new(x * chunkSize/* - halfSize*/, y * chunkSize/* - halfSize*/);

                    for (int voxelIndex = 0, y2 = 0; y2 < _gridView.VoxelResolution; ++y2)
                    {
                        for (int x2 = 0; x2 < _gridView.VoxelResolution; ++x2, ++voxelIndex)
                        {
                            // Pour chaque voxel...

                            Vector3 localPosition = new((x2 + 0.5f) * voxelSize, (y2 + 0.5f) * voxelSize);
                            Vector3 localScale = _voxelSpacing * voxelSize * Vector3.one;

                            if (!Application.isPlaying)
                            {
                                Gizmos.color = _emptyVoxelColor;
                            }
                            else
                            {
                                VoxelChunk chunk = _gridView.Grid.Chunks[chunkIndex];
                                Voxel voxel = chunk.Voxels[voxelIndex];

                                if (chunk.DeadPositions.Contains(voxel.Position))
                                {
                                    Gizmos.color = _deadVoxelColor;
                                }
                                else
                                {
                                    if (_useVoxelStateAsColors)
                                    {
                                        Gizmos.color = _voxelStateColors[voxel.State];
                                    }
                                    else
                                    {
                                        Gizmos.color = voxel.Filled ? _filledVoxelColor : _emptyVoxelColor;
                                    }
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