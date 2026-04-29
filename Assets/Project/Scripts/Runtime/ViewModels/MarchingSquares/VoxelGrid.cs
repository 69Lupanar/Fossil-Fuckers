using System.Collections.Generic;
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
        private Voxel[] voxels;

        /// <summary>
        /// Materials de chaque voxel
        /// </summary>
        private Material[] voxelMaterials;

        /// <summary>
        /// Taille d'un voxel
        /// </summary>
        private float voxelSize;

        /// <summary>
        /// Taille du chunk
        /// </summary>
        private float gridSize;

        /// <summary>
        /// Mesh généré
        /// </summary>
        private Mesh mesh;

        /// <summary>
        /// Vertices du mesh
        /// </summary>
        private List<Vector3> vertices;

        /// <summary>
        /// Triangles du mesh
        /// </summary>
        private List<int> triangles;

        /// <summary>
        /// Chunk voisin
        /// </summary>
        [HideInInspector]
        public VoxelGrid xNeighbor, yNeighbor, xyNeighbor;

        /// <summary>
        /// Faux voxel utilisé lors de la triangulation pour relier les chunks entre eux
        /// </summary>
        private Voxel dummyX, dummyY, dummyT;
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
            gridSize = size;
            voxels = new Voxel[resolution * resolution];
            voxelMaterials = new Material[voxels.Length];

            dummyX = new Voxel();
            dummyY = new Voxel();
            dummyT = new Voxel();

            for (int i = 0, y = 0; y < resolution; ++y)
            {
                for (int x = 0; x < resolution; ++x, ++i)
                {
                    CreateVoxel(i, x, y);
                }
            }

            GetComponent<MeshFilter>().mesh = mesh = new Mesh();
            mesh.name = "VoxelGrid Mesh";
            vertices = new List<Vector3>();
            triangles = new List<int>();
            Refresh();
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
                    voxels[i].state = stencil.Apply(x, y, voxels[i].state);
                }
            }

            Refresh();
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
            voxels[i] = new Voxel(x, y, voxelSize);
        }

        /// <summary>
        /// Assigne les couleurs de chaque voxel en fonction de leur état
        /// </summary>
        private void SetVoxelColors()
        {
            for (int i = 0; i < voxels.Length; ++i)
            {
                voxelMaterials[i].color = voxels[i].state ? Color.black : Color.white;
            }
        }

        /// <summary>
        /// Màj le mesh
        /// </summary>
        private void Refresh()
        {
            SetVoxelColors();
            Triangulate();
        }

        /// <summary>
        /// Calcule les triangles du mesh
        /// </summary>
        private void Triangulate()
        {
            triangles.Clear();
            mesh.Clear();

            if (xNeighbor != null)
            {
                dummyX.BecomeXDummyOf(xNeighbor.voxels[0], gridSize);
            }

            TriangulateCellRows();

            if (yNeighbor != null)
            {
                TriangulateGapRow();
            }

            // TAF : Passer à des tableaux fixes au lieu de listes
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
        }

        /// <summary>
        /// Calcule les triangles de chaque rangée de cellules
        /// </summary>
        private void TriangulateCellRows()
        {
            int cells = resolution - 1;
            for (int i = 0, y = 0; y < cells; ++y, ++i)
            {
                for (int x = 0; x < cells; ++x, ++i)
                {
                    TriangulateCell(
                    voxels[i],
                    voxels[i + 1],
                    voxels[i + resolution],
                    voxels[i + resolution + 1]);
                }
                if (xNeighbor != null)
                {
                    TriangulateGapCell(i);
                }
            }
        }

        /// <summary>
        /// Calcule les triangles d'une cellule en fonction de ses voxels
        /// </summary>
        /// <param name="a">Voxel bas gauche</param>
        /// <param name="b">Voxel bas droit</param>
        /// <param name="c">Voxel haut gauche</param>
        /// <param name="d">Voxel haut droit</param>
        private void TriangulateCell(Voxel a, Voxel b, Voxel c, Voxel d)
        {
            // TAF : tester avec un byte au lieu d'un int
            int cellType = 0;
            if (a.state)
            {
                cellType |= 1;
            }
            if (b.state)
            {
                cellType |= 2;
            }
            if (c.state)
            {
                cellType |= 4;
            }
            if (d.state)
            {
                cellType |= 8;
            }

            // On exécute une triangulation différente pour chacun des 16 cas possibles de la cellule

            switch (cellType)
            {
                case 0:
                    return;
                case 1:
                    AddTriangle(a.position, a.yEdgePosition, a.xEdgePosition);
                    break;
                case 2:
                    AddTriangle(b.position, a.xEdgePosition, b.yEdgePosition);
                    break;
                case 4:
                    AddTriangle(c.position, c.xEdgePosition, a.yEdgePosition);
                    break;
                case 8:
                    AddTriangle(d.position, b.yEdgePosition, c.xEdgePosition);
                    break;
                case 3:
                    AddQuad(a.position, a.yEdgePosition, b.yEdgePosition, b.position);
                    break;
                case 5:
                    AddQuad(a.position, c.position, c.xEdgePosition, a.xEdgePosition);
                    break;
                case 10:
                    AddQuad(a.xEdgePosition, c.xEdgePosition, d.position, b.position);
                    break;
                case 12:
                    AddQuad(a.yEdgePosition, c.position, d.position, b.yEdgePosition);
                    break;
                case 15:
                    AddQuad(a.position, c.position, d.position, b.position);
                    break;
                case 7:
                    AddPentagon(a.position, c.position, c.xEdgePosition, b.yEdgePosition, b.position);
                    break;
                case 11:
                    AddPentagon(b.position, a.position, a.yEdgePosition, c.xEdgePosition, d.position);
                    break;
                case 13:
                    AddPentagon(c.position, d.position, b.yEdgePosition, a.xEdgePosition, a.position);
                    break;
                case 14:
                    AddPentagon(d.position, b.position, a.xEdgePosition, a.yEdgePosition, c.position);
                    break;
                case 6:
                    AddTriangle(b.position, a.xEdgePosition, b.yEdgePosition);
                    AddTriangle(c.position, c.xEdgePosition, a.yEdgePosition);
                    break;
                case 9:
                    AddTriangle(a.position, a.yEdgePosition, a.xEdgePosition);
                    AddTriangle(d.position, b.yEdgePosition, c.xEdgePosition);
                    break;
            }
        }

        /// <summary>
        /// Calcules les truangles d'une cellule séparant deux chunks
        /// </summary>
        /// <param name="i">La position du voxel dans la liste</param>
        private void TriangulateGapCell(int i)
        {
            Voxel dummySwap = dummyT;
            dummySwap.BecomeXDummyOf(xNeighbor.voxels[i + 1], gridSize);
            dummyT = dummyX;
            dummyX = dummySwap;
            TriangulateCell(voxels[i], dummyT, voxels[i + resolution], dummyX);
        }

        /// <summary>
        /// Calcules les truangles des cellules séparant deux chunks
        /// </summary>
        private void TriangulateGapRow()
        {
            dummyY.BecomeYDummyOf(yNeighbor.voxels[0], gridSize);
            int cells = resolution - 1;
            int offset = cells * resolution;

            for (int x = 0; x < cells; ++x)
            {
                Voxel dummySwap = dummyT;
                dummySwap.BecomeYDummyOf(yNeighbor.voxels[x + 1], gridSize);
                dummyT = dummyY;
                dummyY = dummySwap;
                TriangulateCell(voxels[x + offset], voxels[x + offset + 1], dummyT, dummyY);
            }

            if (xNeighbor != null)
            {
                dummyT.BecomeXYDummyOf(xyNeighbor.voxels[0], gridSize);
                TriangulateCell(voxels[^1], dummyX, dummyY, dummyT);
            }
        }

        /// <summary>
        /// Crée un triangle à partir des vertices renseignés
        /// </summary>
        private void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            int vertexIndex = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
        }

        /// <summary>
        /// Crée un quad à partir des vertices renseignés
        /// </summary>
        private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int vertexIndex = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);
        }

        /// <summary>
        /// Crée un pentagone à partir des vertices renseignés
        /// </summary>
        private void AddPentagon(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e)
        {
            int vertexIndex = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            vertices.Add(e);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 3);
            triangles.Add(vertexIndex + 4);
        }

        #endregion
    }
}