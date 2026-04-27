using Assets.Project.Scripts.Runtime.Models.QuadTree;
using UnityEngine;
using TerrainData = Assets.Project.Scripts.Runtime.Models.QuadTree.TerrainData;

namespace Assets.Project.Scripts.Runtime.ViewModels.QuadTreeGen
{
    /// <summary>
    /// Gère le terrain creusable
    /// </summary>
    public class DiggableTerrain : MonoBehaviour
    {
        #region Variables Unity

        /// <summary>
        /// La texture du terrain
        /// </summary>
        [SerializeField]
        private Texture2D _texture2D;

        /// <summary>
        /// Le MeshFilter
        /// </summary>
        [SerializeField]
        private MeshFilter _meshFilter;

        /// <summary>
        /// Dimensions d'un point du terrain dans l'epspace
        /// </summary>
        [SerializeField]
        private float _pixelsPerUnit = 10f;

        /// <summary>
        /// this will be the range of terrain destruction when clicking
        /// </summary>
        [SerializeField]
        private float _destructionRadius = 3f;

        /// <summary>
        /// true pour afficher les gizmos
        /// </summary>
        [SerializeField]
        private bool _showGizmos;

        #endregion

        #region Variables d'instance

        /// <summary>
        /// La caméra
        /// </summary>
        private Camera _camera;

        /// <summary>
        /// Les données du terrain
        /// </summary>
        private TerrainData _terrainData;

        /// <summary>
        /// L'arbre quaternaire
        /// </summary>
        private QuadTree _quadTree;

        /// <summary>
        /// a debug variable to draw a gizmo
        /// </summary>
        private Vector3 _mousePosition = Vector3.zero;

        #endregion

        #region Méthodes Unity

        /// <summary>
        /// Gizmos
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            //if (_showGizmos && !_terrainData.Empty)
            //    DrawGizmos(_pixelsPerUnit);

            if (_showGizmos && !_quadTree.Empty)
                DrawGizmos();
        }

        private void Awake()
        {
            _camera = Camera.main;
            GenerateTerrain();
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            //changing gizmo position after clikcikng
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mousePosition = _camera.ScreenToWorldPoint(Input.mousePosition + new Vector3(0, 0, 5f));
                if (Trigonometry.PointIntersectsAPlane(_camera.transform.position,
                        mousePosition,
                        transform.position,
                        Vector3.forward,
                        out Vector3 result))
                {
                    _mousePosition = result;
                    DestroyArea(result);
                }
            }
        }
        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Génère un nouveau terrain
        /// </summary>
        [ContextMenu("Generate New Terrain")]
        public void GenerateTerrain()
        {
            _terrainData = new TerrainData(_texture2D);
            _quadTree = new QuadTree(_terrainData);
            ConstructMeshes();
        }

        /// <summary>
        /// Efface le terrain actuel
        /// </summary>
        [ContextMenu("Clear Terrain")]
        public void Clear()
        {
            _terrainData = default;
            _quadTree = default;
            DestroyImmediate(_meshFilter.sharedMesh);
        }

        #endregion

        #region Méthodes privées

        /// <summary>
        /// Gén_re un mesh à partir de l'arbre quaternaire
        /// </summary>
        private void ConstructMeshes()
        {
            MeshContructionHelper meshContructionHelper = new();

            foreach (Quad quad in _quadTree)
            {
                if (!quad.HasChildren)
                {
                    bool isQuadSolid = _terrainData.IsSolid(quad);

                    if (isQuadSolid)
                        ConstructQuad(quad, meshContructionHelper);
                }
            }

            _meshFilter.mesh = meshContructionHelper.ConstructMesh();
        }

        /// <summary>
        /// Convertit les coordonnées de l'image en vertices et UVs
        /// </summary>
        /// <param name="x">position du pixel en X</param>
        /// <param name="y">position du pixel en Y</param>
        /// <returns>Les données du vertice</returns>
        private VertexData ImageCoordinatesToVertex(int x, int y)
        {
            //we convert texture coordinates to uv's 
            //by dividing them by texture dimensions

            float uvX = x / (float)_texture2D.width;
            float uvY = y / (float)_texture2D.height;

            //we center our position by substraction half of texture dimension 
            //from texture coordinate and scale it

            Vector3 position = new Vector3(x - _texture2D.width / 2f, y - _texture2D.height / 2f, 0) / _pixelsPerUnit;

            return new VertexData()
            {
                Postion = position,
                Uv = new Vector2(uvX, uvY),
                Normal = transform.forward
            };
        }

        /// <summary>
        /// Convertit les extrémités du quad en données de vertice
        /// </summary>
        /// <param name="quad">Le quad</param>
        private VertexData[] GetQuadCorners(Quad quad)
        {
            VertexData[] vertices = new[]
            {
                //lower left corner
                ImageCoordinatesToVertex(quad.Pos.x, quad.Pos.y),
                //lower right corner
                ImageCoordinatesToVertex(quad.Pos.x + quad.Dimensions.x, quad.Pos.y),
                //upper right corner
                ImageCoordinatesToVertex(quad.Pos.x + quad.Dimensions.x, quad.Pos.y + quad.Dimensions.y),
                //upper left corner
                ImageCoordinatesToVertex(quad.Pos.x, quad.Pos.y + quad.Dimensions.y),
            };

            return vertices;
        }

        /// <summary>
        /// Ajoute le quad au mesh à construire
        /// </summary>
        /// <param name="quad">Le quad à générer</param>
        /// <param name="meshConstructionHelper">Le constructeur</param>
        private void ConstructQuad(Quad quad, MeshContructionHelper meshConstructionHelper)
        {
            VertexData[] vertices = GetQuadCorners(quad);

            //right triagnle
            meshConstructionHelper.AddMeshSection(vertices[0], vertices[2], vertices[1]);

            //left triangle
            meshConstructionHelper.AddMeshSection(vertices[0], vertices[3], vertices[2]);
        }

        /// <summary>
        /// Détruit le terrain aux coordonnées renseignées
        /// </summary>
        /// <param name="position">Le centre</param>
        private void DestroyArea(Vector3 position)
        {
            int x = (int)(position.x * _pixelsPerUnit) + _texture2D.width / 2;
            int y = (int)(position.y * _pixelsPerUnit) + _texture2D.height / 2;

            _terrainData.DestroyTerrain(x, y, (int)(_destructionRadius * _pixelsPerUnit));
            _quadTree = new QuadTree(_terrainData);
            ConstructMeshes();
        }

#if UNITY_EDITOR

        /// <summary>
        /// Gizmos
        /// </summary>
        /// <param name="pixelsPerUnit">Dimensions d'un point du terrain dans l'epspace</param>
        private void DrawGizmos(float pixelsPerUnit)
        {
            for (int y = 0; y < _terrainData.Height; ++y)
            {
                for (int x = 0; x < _terrainData.Width; ++x)
                {
                    Vector3 position = new Vector3(x, y, 0) / pixelsPerUnit;
                    Gizmos.color = _terrainData.Points[x + y * _terrainData.Width] ? Color.red : Color.blue;
                    Gizmos.DrawCube(position, Vector3.one / pixelsPerUnit);
                }
            }
        }

        /// <summary>
        /// Gizmos
        /// </summary>
        public void DrawGizmos()
        {
            DrawQuadGizmos(_quadTree.Root, Color.green);

            foreach (Quad quad in _quadTree)
            {
                Color color = _terrainData.IsSolid(quad) ? Color.red : Color.blue;
                DrawQuadGizmos(quad, color);
            }
        }

        /// <summary>
        /// Gizmos
        /// </summary>
        /// <param name="quad">Le quad à afficher</param>
        /// <param name="color">Couleur du quad</param>
        private void DrawQuadGizmos(Quad quad, Color color)
        {
            float x = quad.Pos.x + quad.Dimensions.x / 2f;
            float y = quad.Pos.y + quad.Dimensions.y / 2f;

            Vector3 center = new(x, y, 0);
            Vector3 size = new(quad.Dimensions.x, quad.Dimensions.y);
            Gizmos.color = color;
            Gizmos.DrawWireCube(center, size);
            color.a = 0.4f;
            Gizmos.color = color;
            Gizmos.DrawCube(center, size);
        }

#endif
        #endregion
    }
}