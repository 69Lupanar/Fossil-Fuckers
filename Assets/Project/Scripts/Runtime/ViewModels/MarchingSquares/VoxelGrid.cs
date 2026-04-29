using UnityEngine;

namespace Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares
{
    /// <summary>
    /// Chunk contenant une grille de voxels
    /// </summary>
    [SelectionBase]
    public class VoxelGrid : MonoBehaviour
    {
        #region Variables Unity

        /// <summary>
        /// Taille du chunk
        /// </summary>
        [Tooltip("Taille du chunk")]
        public int resolution = 8;

        /// <summary>
        /// Espacement entre les voxels
        /// </summary>
        [Tooltip("Espacement entre les voxels")]
        public float voxelSpacing = .9f;

        /// <summary>
        /// Prefab d'un voxel
        /// </summary>
        [Tooltip("Prefab d'un voxel")]
        public GameObject voxelPrefab;

        #endregion

        #region Variables d'instance

        /// <summary>
        /// Grille de voxels
        /// </summary>
        private bool[] voxels;

        /// <summary>
        /// Materials de chaque voxel
        /// </summary>
        private Material[] voxelMaterials;

        /// <summary>
        /// Taille d'un voxel
        /// </summary>
        private float voxelSize;

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// init
        /// </summary>
        /// <param name="resolution">Résolution des voxels pour ce chunk</param>
        /// <param name="resolution">Taille du chunk</param>
        public void Initialize(int resolution, float size)
        {
            this.resolution = resolution;
            voxelSize = size / resolution;
            voxels = new bool[resolution * resolution];
            voxelMaterials = new Material[voxels.Length];

            for (int i = 0, y = 0; y < resolution; ++y)
            {
                for (int x = 0; x < resolution; ++x, ++i)
                {
                    CreateVoxel(i, x, y);
                }
            }

            SetVoxelColors();
        }

        /// <summary>
        /// Modifie l'état d'un voxel
        /// </summary>
        /// <param name="stencil">Brosse utilisée</param>
        public void Apply(VoxelStencil stencil)
        {
            int xStart = Mathf.Max(0, stencil.XStart);
            int xEnd = Mathf.Min(stencil.XEnd, resolution - 1);
            int yStart = Mathf.Max(0, stencil.YStart);
            int yEnd = Mathf.Min(stencil.YEnd, resolution - 1);

            // On traverse toute la zone rectangulaire englobant la brosse
            // pour modifier les voxels concernés

            for (int y = yStart; y <= yEnd; ++y)
            {
                int i = y * resolution + xStart;

                for (int x = xStart; x <= xEnd; ++x, ++i)
                {
                    voxels[i] = stencil.Apply(x, y, voxels[i]);
                }
            }

            SetVoxelColors();
        }

        #endregion

        #region Méthodes privées

        /// <summary>
        /// Crée un voxel aux coordonnées renseignées
        /// </summary>
        private void CreateVoxel(int i, int x, int y)
        {
            GameObject o = Instantiate(voxelPrefab, transform);
            o.transform.localPosition = new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize);
            o.transform.localScale = (1f - voxelSpacing) * voxelSize * Vector3.one;
            voxelMaterials[i] = o.GetComponent<MeshRenderer>().material;
        }

        /// <summary>
        /// Assigne les couleurs de chaque voxel en fonction de leur état
        /// </summary>
        private void SetVoxelColors()
        {
            for (int i = 0; i < voxels.Length; ++i)
            {
                voxelMaterials[i].color = voxels[i] ? Color.black : Color.white;
            }
        }

        #endregion
    }
}