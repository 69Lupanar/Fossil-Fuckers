using UnityEngine;

namespace Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares
{
    /// <summary>
    /// Grille de voxels divisée en chunks
    /// </summary>
    public class VoxelMap : MonoBehaviour
    {
        #region Propriétés

        /// <summary>
        /// Le type de remplissage de la brosse active
        /// </summary>
        public int FillTypeIndex { get; set; }

        /// <summary>
        /// Le rayon de la brosse active
        /// </summary>
        public int RadiusIndex { get; set; }

        /// <summary>
        /// Le type de la brosse active
        /// </summary>
        public int StencilIndex { get; set; }

        #endregion

        #region Variables Unity

        /// <summary>
        /// Nombre de chunks par dimension de la carte
        /// </summary>
        [Tooltip("Nombres de chunks par dimensions de la carte")]
        public float size = 2f;

        /// <summary>
        /// Nombre de voxels par dimension de la carte
        /// </summary>
        [Tooltip("Nombre de voxels par dimension de la carte")]
        public int voxelResolution = 8;

        /// <summary>
        /// Taille d'un chunk
        /// </summary>
        [Tooltip("Taille d'un chunk")]
        public int chunkResolution = 2;

        /// <summary>
        /// Prefab de la grille de voxels
        /// </summary>
        [Tooltip("Prefab de la grille de voxels")]
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
        /// Moitié de la taille de la grille
        /// </summary>
        private float halfSize;

        /// <summary>
        /// Brosses
        /// </summary>
        private VoxelStencil[] stencils = { new VoxelStencil(), new VoxelStencilCircle() };

        #endregion

        #region Méthodes Unity

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
        /// màj à chaque frame
        /// </summary>
        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                // On récupère la position de la souris dans la grille

                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo))
                {
                    if (hitInfo.collider.gameObject == gameObject)
                        EditVoxels(transform.InverseTransformPoint(hitInfo.point));
                }
            }
        }

        #endregion

        /// <summary>
        /// Crée un chunk à partir des coordonnées renseignées
        /// </summary>
        /// <param name="i"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void CreateChunk(int i, int x, int y)
        {
            VoxelGrid chunk = Instantiate(voxelGridPrefab, transform);
            chunk.Initialize(voxelResolution, chunkSize);
            chunk.transform.localPosition = new Vector3(x * chunkSize/* - halfSize*/, y * chunkSize/* - halfSize*/);
            chunks[i] = chunk;

            if (x > 0)
            {
                chunks[i - 1].xNeighbor = chunk;
            }
            if (y > 0)
            {
                chunks[i - chunkResolution].yNeighbor = chunk;
                if (x > 0)
                {
                    chunks[i - chunkResolution - 1].xyNeighbor = chunk;
                }
            }
        }

        /// <summary>
        /// Màj l'état des voxels affectéspar la brosse active
        /// </summary>
        /// <param name="point">Position du curseur</param>
        private void EditVoxels(Vector3 point)
        {
            int centerX = (int)((point.x/* + halfSize*/) / voxelSize);
            int centerY = (int)((point.y/* + halfSize*/) / voxelSize);

            int xStart = Mathf.Max(0, (centerX - RadiusIndex - 1) / voxelResolution);
            int xEnd = Mathf.Min((centerX + RadiusIndex) / voxelResolution, chunkResolution - 1);
            int yStart = Mathf.Max(0, (centerY - RadiusIndex - 1) / voxelResolution);
            int yEnd = Mathf.Min((centerY + RadiusIndex) / voxelResolution, chunkResolution - 1);

            VoxelStencil activeStencil = stencils[StencilIndex];
            activeStencil.Initialize(FillTypeIndex == 0, RadiusIndex);

            int voxelYOffset = yEnd * voxelResolution;

            for (int y = yEnd; y >= yStart; --y)
            {
                int i = y * chunkResolution + xEnd;
                int voxelXOffset = xEnd * voxelResolution;

                for (int x = xEnd; x >= xStart; --x, --i)
                {
                    activeStencil.SetCenter(centerX - voxelXOffset, centerY - voxelYOffset);
                    chunks[i].Apply(activeStencil);
                    voxelXOffset -= voxelResolution;
                }

                voxelYOffset -= voxelResolution;
            }
        }
    }
}