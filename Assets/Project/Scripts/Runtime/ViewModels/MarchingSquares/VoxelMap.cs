using UnityEngine;

namespace Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares
{
    /// <summary>
    /// Grille contenant les chunks de voxels
    /// </summary>
    public class VoxelMap : MonoBehaviour
    {
        #region Propriétés

        /// <summary>
        /// Etat que doit prendre un voxel au passage de la brosse
        /// </summary>
        public int FillTypeIndex { get; set; }

        /// <summary>
        /// Taille de la brosse
        /// </summary>
        public int RadiusIndex { get; set; }

        /// <summary>
        /// Type de la brosse
        /// </summary>
        public int StencilIndex { get; set; }

        #endregion

        #region Variables Unity

        /// <summary>
        /// Dimensions de la carte en nb de chunks
        /// </summary>
        [Tooltip("Dimensions de la carte en nb de chunks")]
        public float size = 2f;

        /// <summary>
        /// Résolution des voxels par chunk
        /// </summary>
        [Tooltip("Résolution des voxels par chunk")]
        public int voxelResolution = 8;

        /// <summary>
        /// Taille d'un chunk
        /// </summary>
        [Tooltip("Taille d'un chunk")]
        public int chunkResolution = 2;

        /// <summary>
        /// Prefab des chunks
        /// </summary>
        [Tooltip("Prefab des chunks")]
        public VoxelGrid voxelGridPrefab;

        #endregion

        #region Variables d'instance

        /// <summary>
        /// Chunks
        /// </summary>
        private VoxelGrid[] chunks;

        /// <summary>
        /// Taille d'un chunk
        /// </summary>
        private float chunkSize;

        /// <summary>
        /// Taille d'un voxel
        /// </summary>
        private float voxelSize;

        /// <summary>
        /// Moitié de la taille de la carte
        /// </summary>
        private float halfSize;

        /// <summary>
        /// Les brosses
        /// </summary>
        private VoxelStencil[] stencils = { new VoxelStencil(), new VoxelStencilCircle() };

        #endregion

        #region Variables Unity

        /// <summary>
        /// init
        /// </summary>
        private void Awake()
        {
            halfSize = size * 0.5f;
            chunkSize = size / chunkResolution;
            voxelSize = chunkSize / voxelResolution;
            chunks = new VoxelGrid[chunkResolution * chunkResolution];
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(size, size);
            box.center = new Vector3(halfSize, halfSize);

            for (int i = 0, y = 0; y < chunkResolution; ++y)
            {
                for (int x = 0; x < chunkResolution; ++x, ++i)
                {
                    CreateChunk(i, x, y);
                }
            }
        }

        /// <summary>
        /// Màj à chaque frame
        /// </summary>
        private void Update()
        {
            // On détecte où se trouve la souris pour éditer le voxel à son emplacement

            if (Input.GetMouseButton(0))
            {
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo))
                {
                    if (hitInfo.collider.gameObject == gameObject)
                    {
                        EditVoxels(transform.InverseTransformPoint(hitInfo.point));
                    }
                }
            }
        }

        #endregion

        #region Méthodes privées

        /// <summary>
        /// Crée un chunk aux coordonnées renseignées
        /// </summary>
        private void CreateChunk(int i, int x, int y)
        {
            VoxelGrid chunk = Instantiate(voxelGridPrefab, transform);
            chunk.transform.localPosition = new Vector3(x * chunkSize/* - halfSize*/, y * chunkSize/* - halfSize*/);    //halfSize permet de garder la grille centrée sur la caméra
            chunk.Initialize(voxelResolution, chunkSize);
            chunks[i] = chunk;
        }

        /// <summary>
        /// Modifie le voxel aux coordonnées renseignées
        /// </summary>
        /// <param name="point">Position du voxel</param>
        private void EditVoxels(Vector3 point)
        {
            //halfsize remet l'origine à 0 si on a décalé la carte dans l'Awake

            int centerX = (int)((point.x/* + halfSize*/) / voxelSize);
            int centerY = (int)((point.y/* + halfSize*/) / voxelSize);

            // On doit permettre à la brosse d'affecter les voxels
            // à travers tous les chunks

            int xStart = Mathf.Max(0, (centerX - RadiusIndex) / voxelResolution);
            int xEnd = Mathf.Min((centerX + RadiusIndex) / voxelResolution, chunkResolution - 1);
            int yStart = Mathf.Max(0, (centerY - RadiusIndex) / voxelResolution);
            int yEnd = Mathf.Min((centerY + RadiusIndex) / voxelResolution, chunkResolution - 1);


            VoxelStencil activeStencil = stencils[StencilIndex];
            activeStencil.Initialize(FillTypeIndex == 0, RadiusIndex);
            int voxelYOffset = yStart * voxelResolution;

            for (int y = yStart; y <= yEnd; ++y)
            {
                int i = y * chunkResolution + xStart;
                int voxelXOffset = xStart * voxelResolution;

                for (int x = xStart; x <= xEnd; ++x, ++i)
                {
                    activeStencil.SetCenter(centerX - voxelXOffset, centerY - voxelYOffset);
                    chunks[i].Apply(activeStencil);
                    voxelXOffset += voxelResolution;
                }

                voxelYOffset += voxelResolution;
            }
        }

        #endregion
    }
}