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

        /// <summary>
        /// Cache des vertices
        /// </summary>
        private int[] rowCacheMax, rowCacheMin;

        /// <summary>
        /// Cache des vertices
        /// </summary>
        private int edgeCacheMin, edgeCacheMax;

        /// <summary>
        /// Cosinus de la limite autorisée pour un angle d'une section du mesh
        /// </summary>
        private float sharpFeatureLimit;

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// init
        /// </summary>
        /// <param name="resolution">Résolution des voxels pour ce chunk</param>
        /// <param name="resolution">Taille du chunk</param>
        /// <param name="maxFeatureAngle">Angle max d'une section du mesh qui peut apparaître</param>
        public void Initialize(int resolution, float size, float maxFeatureAngle)
        {
            sharpFeatureLimit = Mathf.Cos(maxFeatureAngle * Mathf.Deg2Rad);
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
            rowCacheMax = new int[resolution * 2 + 1];
            rowCacheMin = new int[resolution * 2 + 1];
            Refresh();
        }

        /// <summary>
        /// Modifie l'état d'un voxel
        /// </summary>
        /// <param name="stencil">Brosse utilisée</param>
        public void Apply(VoxelStencil stencil)
        {
            int xStart = Mathf.Max(0, (int)(stencil.XStart / voxelSize));
            int xEnd = Mathf.Min((int)(stencil.XEnd / voxelSize), resolution - 1);
            int yStart = Mathf.Max(0, (int)(stencil.YStart / voxelSize));
            int yEnd = Mathf.Min((int)(stencil.YEnd / voxelSize), resolution - 1);

            // On traverse toute la zone rectangulaire englobant la brosse
            // pour modifier les voxels concernés

            for (int y = yStart; y <= yEnd; ++y)
            {
                int i = y * resolution + xStart;

                for (int x = xStart; x <= xEnd; ++x, ++i)
                {
                    stencil.Apply(voxels[i]);
                }
            }

            SetCrossings(stencil, xStart, xEnd, yStart, yEnd);
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

            FillFirstRowCache();
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
                SwapRowCaches();
                CacheFirstCorner(voxels[i + resolution]);
                CacheNextMiddleEdge(voxels[i], voxels[i + resolution]);

                for (int x = 0; x < cells; ++x, ++i)
                {
                    Voxel
                     a = voxels[i],
                     b = voxels[i + 1],
                     c = voxels[i + resolution],
                     d = voxels[i + resolution + 1];
                    int cacheIndex = x * 2;
                    CacheNextEdgeAndCorner(cacheIndex, c, d);
                    CacheNextMiddleEdge(b, d);
                    TriangulateCell(cacheIndex, a, b, c, d);
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
        /// <param name="i">Indice du cache</param>
        /// <param name="a">Voxel bas gauche</param>
        /// <param name="b">Voxel bas droit</param>
        /// <param name="c">Voxel haut gauche</param>
        /// <param name="d">Voxel haut droit</param>
        private void TriangulateCell(int i, Voxel a, Voxel b, Voxel c, Voxel d)
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
                    TriangulateCase0(i, a, b, c, d);
                    break;
                case 1:
                    TriangulateCase1(i, a, b, c, d);
                    break;
                case 2:
                    TriangulateCase2(i, a, b, c, d);
                    break;
                case 3:
                    TriangulateCase3(i, a, b, c, d);
                    break;
                case 4:
                    TriangulateCase4(i, a, b, c, d);
                    break;
                case 5:
                    TriangulateCase5(i, a, b, c, d);
                    break;
                case 6:
                    TriangulateCase6(i, a, b, c, d);
                    break;
                case 7:
                    TriangulateCase7(i, a, b, c, d);
                    break;
                case 8:
                    TriangulateCase8(i, a, b, c, d);
                    break;
                case 9:
                    TriangulateCase9(i, a, b, c, d);
                    break;
                case 10:
                    TriangulateCase10(i, a, b, c, d);
                    break;
                case 11:
                    TriangulateCase11(i, a, b, c, d);
                    break;
                case 12:
                    TriangulateCase12(i, a, b, c, d);
                    break;
                case 13:
                    TriangulateCase13(i, a, b, c, d);
                    break;
                case 14:
                    TriangulateCase14(i, a, b, c, d);
                    break;
                case 15:
                    TriangulateCase15(i, a, b, c, d);
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
            int cacheIndex = (resolution - 1) * 2;
            CacheNextEdgeAndCorner(cacheIndex, voxels[i + resolution], dummyX);
            CacheNextMiddleEdge(dummyT, dummyX);
            TriangulateCell(cacheIndex, voxels[i], dummyT, voxels[i + resolution], dummyX);
        }

        /// <summary>
        /// Calcules les truangles des cellules séparant deux chunks
        /// </summary>
        private void TriangulateGapRow()
        {
            dummyY.BecomeYDummyOf(yNeighbor.voxels[0], gridSize);
            int cells = resolution - 1;
            int offset = cells * resolution;
            SwapRowCaches();
            CacheFirstCorner(dummyY);
            CacheNextMiddleEdge(voxels[cells * resolution], dummyY);

            for (int x = 0; x < cells; ++x)
            {
                Voxel dummySwap = dummyT;
                dummySwap.BecomeYDummyOf(yNeighbor.voxels[x + 1], gridSize);
                dummyT = dummyY;
                dummyY = dummySwap;
                int cacheIndex = x * 2;
                CacheNextEdgeAndCorner(cacheIndex, dummyT, dummyY);
                CacheNextMiddleEdge(voxels[x + offset + 1], dummyY);
                TriangulateCell(cacheIndex, voxels[x + offset], voxels[x + offset + 1], dummyT, dummyY);
            }

            if (xNeighbor != null)
            {
                dummyT.BecomeXYDummyOf(xyNeighbor.voxels[0], gridSize);
                int cacheIndex = cells * 2;
                CacheNextEdgeAndCorner(cacheIndex, dummyY, dummyT);
                CacheNextMiddleEdge(dummyX, dummyT);
                TriangulateCell(cacheIndex, voxels[^1], dummyX, dummyY, dummyT);
            }
        }

        /// <summary>
        /// Crée un triangle à partir des vertices renseignés
        /// </summary>
        private void AddTriangle(int a, int b, int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        /// <summary>
        /// Crée un quad à partir des vertices renseignés
        /// </summary>
        private void AddQuad(int a, int b, int c, int d)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
        }

        /// <summary>
        /// Crée un pentagone à partir des vertices renseignés
        /// </summary>
        private void AddPentagon(int a, int b, int c, int d, int e)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
            triangles.Add(a);
            triangles.Add(d);
            triangles.Add(e);
        }

        private void TriangulateCase0(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
        }

        private void TriangulateCase15(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            AddQuadABCD(i);
        }

        private void TriangulateCase1(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
                if (ClampToCellMaxMax(ref point, a, d))
                {
                    AddQuadA(i, point);
                    return;
                }
            }

            AddTriangleA(i);
        }

        private void TriangulateCase2(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
                if (ClampToCellMinMax(ref point, a, d))
                {
                    AddQuadB(i, point);
                    return;
                }
            }

            AddTriangleB(i);
        }

        private void TriangulateCase4(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = c.xNormal;
            Vector2 n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
                if (ClampToCellMaxMin(ref point, a, d))
                {
                    AddQuadC(i, point);
                    return;
                }
            }
            AddTriangleC(i);
        }

        private void TriangulateCase8(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = c.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
                if (ClampToCellMinMin(ref point, a, d))
                {
                    AddQuadD(i, point);
                    return;
                }
            }
            AddTriangleD(i);
        }

        private void TriangulateCase7(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = c.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddHexagonABC(i, point);
                    return;
                }
            }
            AddPentagonABC(i);
        }

        private void TriangulateCase11(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = c.xNormal;
            Vector2 n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddHexagonABD(i, point);
                    return;
                }
            }
            AddPentagonABD(i);
        }

        private void TriangulateCase13(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddHexagonACD(i, point);
                    return;
                }
            }
            AddPentagonACD(i);
        }

        private void TriangulateCase14(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddHexagonBCD(i, point);
                    return;
                }
            }
            AddPentagonBCD(i);
        }

        private void TriangulateCase3(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.yNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.YEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddPentagonAB(i, point);
                    return;
                }
            }
            AddQuadAB(i);
        }

        private void TriangulateCase5(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = c.xNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, c.XEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddPentagonAC(i, point);
                    return;
                }
            }
            AddQuadAC(i);
        }

        private void TriangulateCase10(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = c.xNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, c.XEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddPentagonBD(i, point);
                    return;
                }
            }
            AddQuadBD(i);
        }

        private void TriangulateCase12(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.yNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.YEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddPentagonCD(i, point);
                    return;
                }
            }
            AddQuadCD(i);
        }

        private void TriangulateCase6(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            bool sharp1, sharp2;
            Vector2 point1, point2;

            Vector2 n1 = a.xNormal;
            Vector2 n2 = -b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                point1 = ComputeIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
                sharp1 = ClampToCellMinMax(ref point1, a, d);
            }
            else
            {
                point1.x = point1.y = 0f;
                sharp1 = false;
            }

            n1 = c.xNormal;
            n2 = -a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                point2 = ComputeIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
                sharp2 = ClampToCellMaxMin(ref point2, a, d);
            }
            else
            {
                point2.x = point2.y = 0f;
                sharp2 = false;
            }

            if (sharp1)
            {
                if (sharp2)
                {
                    // Both sharp.
                    if (IsBelowLine(point2, a.XEdgePoint, point1))
                    {
                        if (
                            IsBelowLine(point2, point1, b.YEdgePoint) ||
                            IsBelowLine(point1, point2, a.YEdgePoint))
                        {
                            TriangulateCase6Connected(i, a, b, c, d);
                            return;
                        }
                    }
                    else if (
                        IsBelowLine(point2, point1, b.YEdgePoint) &&
                        IsBelowLine(point1, c.XEdgePoint, point2))
                    {
                        TriangulateCase6Connected(i, a, b, c, d);
                        return;
                    }
                    AddQuadB(i, point1);
                    AddQuadC(i, point2);
                    return;
                }
                // First sharp.
                if (IsBelowLine(point1, c.XEdgePoint, a.YEdgePoint))
                {
                    TriangulateCase6Connected(i, a, b, c, d);
                    return;
                }
                AddQuadB(i, point1);
                AddTriangleC(i);
                return;
            }
            if (sharp2)
            {
                // Second sharp.
                if (IsBelowLine(point2, a.XEdgePoint, b.YEdgePoint))
                {
                    TriangulateCase6Connected(i, a, b, c, d);
                    return;
                }
                AddTriangleB(i);
                AddQuadC(i, point2);
                return;
            }
            // Neither sharp.
            AddTriangleB(i);
            AddTriangleC(i);
        }

        private void TriangulateCase6Connected(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = -a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
                if (IsInsideCell(point, a, d) && IsBelowLine(point, c.position, b.position))
                {
                    AddPentagonBCToA(i, point);
                }
                else
                {
                    AddQuadBCToA(i);
                }
            }
            else
            {
                AddQuadBCToA(i);
            }

            n1 = c.xNormal;
            n2 = -b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d) && IsBelowLine(point, b.position, c.position))
                {
                    AddPentagonBCToD(i, point);
                    return;
                }
            }
            AddQuadBCToD(i);
        }

        private void TriangulateCase9(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            bool sharp1, sharp2;
            Vector2 point1, point2;
            Vector2 n1 = a.xNormal;
            Vector2 n2 = a.yNormal;

            if (IsSharpFeature(n1, n2))
            {
                point1 = ComputeIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
                sharp1 = ClampToCellMaxMax(ref point1, a, d);
            }
            else
            {
                point1.x = point1.y = 0f;
                sharp1 = false;
            }

            n1 = c.xNormal;
            n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                point2 = ComputeIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
                sharp2 = ClampToCellMinMin(ref point2, a, d);
            }
            else
            {
                point2.x = point2.y = 0f;
                sharp2 = false;
            }

            if (sharp1)
            {
                if (sharp2)
                {
                    if (IsBelowLine(point1, b.YEdgePoint, point2))
                    {
                        if (
                            IsBelowLine(point1, point2, c.XEdgePoint) ||
                            IsBelowLine(point2, point1, a.XEdgePoint))
                        {
                            TriangulateCase9Connected(i, a, b, c, d);
                            return;
                        }
                    }
                    else if (
                        IsBelowLine(point1, point2, c.XEdgePoint) &&
                        IsBelowLine(point2, a.YEdgePoint, point1))
                    {
                        TriangulateCase9Connected(i, a, b, c, d);
                        return;
                    }
                    AddQuadA(i, point1);
                    AddQuadD(i, point2);
                    return;
                }
                if (IsBelowLine(point1, b.YEdgePoint, c.XEdgePoint))
                {
                    TriangulateCase9Connected(i, a, b, c, d);
                    return;
                }
                AddQuadA(i, point1);
                AddTriangleD(i);
                return;
            }
            if (sharp2)
            {
                if (IsBelowLine(point2, a.YEdgePoint, a.XEdgePoint))
                {
                    TriangulateCase9Connected(i, a, b, c, d);
                    return;
                }
                AddTriangleA(i);
                AddQuadD(i, point2);
                return;
            }
            AddTriangleA(i);
            AddTriangleD(i);
        }

        private void TriangulateCase9Connected(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d) && IsBelowLine(point, a.position, d.position))
                {
                    AddPentagonADToB(i, point);
                }
                else
                {
                    AddQuadADToB(i);
                }
            }
            else
            {
                AddQuadADToB(i);
            }

            n1 = c.xNormal;
            n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
                if (IsInsideCell(point, a, d) && IsBelowLine(point, d.position, a.position))
                {
                    AddPentagonADToC(i, point);
                    return;
                }
            }
            AddQuadADToC(i);
        }

        private void AddQuadABCD(int i)
        {
            AddQuad(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 2], rowCacheMin[i + 2]);
        }

        private void AddTriangleA(int i)
        {
            AddTriangle(rowCacheMin[i], edgeCacheMin, rowCacheMin[i + 1]);
        }

        private void AddTriangleB(int i)
        {
            AddTriangle(rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMax);
        }

        private void AddTriangleC(int i)
        {
            AddTriangle(rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMin);
        }

        private void AddTriangleD(int i)
        {
            AddTriangle(rowCacheMax[i + 2], edgeCacheMax, rowCacheMax[i + 1]);
        }

        private void AddPentagonABC(int i)
        {
            AddPentagon(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMax, rowCacheMin[i + 2]);
        }

        private void AddPentagonABD(int i)
        {
            AddPentagon(rowCacheMin[i + 2], rowCacheMin[i], edgeCacheMin, rowCacheMax[i + 1], rowCacheMax[i + 2]);
        }

        private void AddPentagonACD(int i)
        {
            AddPentagon(rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax, rowCacheMin[i + 1], rowCacheMin[i]);
        }

        private void AddPentagonBCD(int i)
        {
            AddPentagon(rowCacheMax[i + 2], rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMin, rowCacheMax[i]);
        }

        private void AddPentagonAB(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, edgeCacheMax, rowCacheMin[i + 2], rowCacheMin[i], edgeCacheMin);
            vertices.Add(extraVertex);
        }

        private void AddPentagonAC(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, rowCacheMin[i + 1], rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1]);
            vertices.Add(extraVertex);
        }

        private void AddPentagonBD(int i, Vector2 extraVertex)
        {
            AddPentagon(
                vertices.Count, rowCacheMax[i + 1], rowCacheMax[i + 2], rowCacheMin[i + 2], rowCacheMin[i + 1]);
            vertices.Add(extraVertex);
        }

        private void AddPentagonCD(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, edgeCacheMin, rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax);
            vertices.Add(extraVertex);
        }

        private void AddPentagonBCToA(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, edgeCacheMin, rowCacheMax[i], rowCacheMin[i + 2], rowCacheMin[i + 1]);
            vertices.Add(extraVertex);
        }

        private void AddPentagonBCToD(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, edgeCacheMax, rowCacheMin[i + 2], rowCacheMax[i], rowCacheMax[i + 1]);
            vertices.Add(extraVertex);
        }

        private void AddPentagonADToB(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, rowCacheMin[i + 1], rowCacheMin[i], rowCacheMax[i + 2], edgeCacheMax);
            vertices.Add(extraVertex);
        }

        private void AddPentagonADToC(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, rowCacheMax[i + 1], rowCacheMax[i + 2], rowCacheMin[i], edgeCacheMin);
            vertices.Add(extraVertex);
        }

        private void AddQuadAB(int i)
        {
            AddQuad(rowCacheMin[i], edgeCacheMin, edgeCacheMax, rowCacheMin[i + 2]);
        }

        private void AddQuadAC(int i)
        {
            AddQuad(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1], rowCacheMin[i + 1]);
        }

        private void AddQuadBD(int i)
        {
            AddQuad(rowCacheMin[i + 1], rowCacheMax[i + 1], rowCacheMax[i + 2], rowCacheMin[i + 2]);
        }

        private void AddQuadCD(int i)
        {
            AddQuad(edgeCacheMin, rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax);
        }

        private void AddQuadA(int i, Vector2 extraVertex)
        {
            AddQuad(vertices.Count, rowCacheMin[i + 1], rowCacheMin[i], edgeCacheMin);
            vertices.Add(extraVertex);
        }

        private void AddQuadB(int i, Vector2 extraVertex)
        {
            AddQuad(vertices.Count, edgeCacheMax, rowCacheMin[i + 2], rowCacheMin[i + 1]);
            vertices.Add(extraVertex);
        }

        private void AddQuadC(int i, Vector2 extraVertex)
        {
            AddQuad(vertices.Count, edgeCacheMin, rowCacheMax[i], rowCacheMax[i + 1]);
            vertices.Add(extraVertex);
        }

        private void AddQuadD(int i, Vector2 extraVertex)
        {
            AddQuad(vertices.Count, rowCacheMax[i + 1], rowCacheMax[i + 2], edgeCacheMax);
            vertices.Add(extraVertex);
        }

        private void AddQuadBCToA(int i)
        {
            AddQuad(edgeCacheMin, rowCacheMax[i], rowCacheMin[i + 2], rowCacheMin[i + 1]);
        }

        private void AddQuadBCToD(int i)
        {
            AddQuad(edgeCacheMax, rowCacheMin[i + 2], rowCacheMax[i], rowCacheMax[i + 1]);
        }

        private void AddQuadADToB(int i)
        {
            AddQuad(rowCacheMin[i + 1], rowCacheMin[i], rowCacheMax[i + 2], edgeCacheMax);
        }

        private void AddQuadADToC(int i)
        {
            AddQuad(rowCacheMax[i + 1], rowCacheMax[i + 2], rowCacheMin[i], edgeCacheMin);
        }

        private void AddHexagon(int a, int b, int c, int d, int e, int f)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
            triangles.Add(a);
            triangles.Add(d);
            triangles.Add(e);
            triangles.Add(a);
            triangles.Add(e);
            triangles.Add(f);
        }

        private void AddHexagonABC(int i, Vector2 extraVertex)
        {
            AddHexagon(
                vertices.Count, edgeCacheMax, rowCacheMin[i + 2],
                rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1]);
            vertices.Add(extraVertex);
        }

        private void AddHexagonABD(int i, Vector2 extraVertex)
        {
            AddHexagon(
                vertices.Count, rowCacheMax[i + 1], rowCacheMax[i + 2],
                rowCacheMin[i + 2], rowCacheMin[i], edgeCacheMin);
            vertices.Add(extraVertex);
        }

        private void AddHexagonACD(int i, Vector2 extraVertex)
        {
            AddHexagon(
                vertices.Count, rowCacheMin[i + 1], rowCacheMin[i],
                rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax);
            vertices.Add(extraVertex);
        }

        private void AddHexagonBCD(int i, Vector2 extraVertex)
        {
            AddHexagon(
                vertices.Count, edgeCacheMin, rowCacheMax[i],
                rowCacheMax[i + 2], rowCacheMin[i + 2], rowCacheMin[i + 1]);
            vertices.Add(extraVertex);
        }

        /// <summary>
        /// Indique si les normales forment un angle dépassant l'angle limite
        /// </summary>
        private bool IsSharpFeature(Vector2 normal1, Vector2 normal2)
        {
            float dot = Vector2.Dot(normal1, -normal2);
            return dot >= sharpFeatureLimit && dot < 0.9999f;
        }

        /// <summary>
        /// Indique si le point renseigné est contenu dans la cellule
        /// représentée par les voxels renseignés
        /// </summary>
        private static bool IsInsideCell(Vector2 point, Voxel min, Voxel max)
        {
            return
                point.x > min.position.x && point.y > min.position.y &&
                point.x < max.position.x && point.y < max.position.y;
        }

        /// <summary>
        /// Indique si le point renseigné est en dessous d'une ligne
        /// </summary>
        private static bool IsBelowLine(Vector2 p, Vector2 start, Vector2 end)
        {
            float determinant = (end.x - start.x) * (p.y - start.y) - (end.y - start.y) * (p.x - start.x);
            return determinant < 0f;
        }

        /// <summary>
        /// Calcule l'intersection entre deux points
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="normal1"></param>
        /// <param name="point2"></param>
        /// <param name="normal2"></param>
        private static Vector2 ComputeIntersection(Vector2 point1, Vector2 normal1, Vector2 point2, Vector2 normal2)
        {
            Vector2 d2 = new(normal2.y, -normal2.x);
            float u2 = -Vector2.Dot(normal1, point2 - point1) / Vector2.Dot(normal1, d2);
            return point2 + d2 * u2;
        }

        /// <summary>
        /// Garde les points d'une surface à l'intérieur de sa cellule
        /// </summary>
        private static bool ClampToCellMaxMax(ref Vector2 point, Voxel min, Voxel max)
        {
            if (point.x < min.position.x || point.y < min.position.y)
            {
                return false;
            }
            if (point.x > max.position.x)
            {
                point.x = max.position.x;
            }
            if (point.y > max.position.y)
            {
                point.y = max.position.y;
            }
            return true;
        }

        /// <summary>
        /// Garde les points d'une surface à l'intérieur de sa cellule
        /// </summary>
        private static bool ClampToCellMinMin(ref Vector2 point, Voxel min, Voxel max)
        {
            if (point.x > max.position.x || point.y > max.position.y)
            {
                return false;
            }
            if (point.x < min.position.x)
            {
                point.x = min.position.x;
            }
            if (point.y < min.position.y)
            {
                point.y = min.position.y;
            }
            return true;
        }

        /// <summary>
        /// Garde les points d'une surface à l'intérieur de sa cellule
        /// </summary>
        private static bool ClampToCellMinMax(ref Vector2 point, Voxel min, Voxel max)
        {
            if (point.x > max.position.x || point.y < min.position.y)
            {
                return false;
            }
            if (point.x < min.position.x)
            {
                point.x = min.position.x;
            }
            if (point.y > max.position.y)
            {
                point.y = max.position.y;
            }
            return true;
        }

        /// <summary>
        /// Garde les points d'une surface à l'intérieur de sa cellule
        /// </summary>
        private static bool ClampToCellMaxMin(ref Vector2 point, Voxel min, Voxel max)
        {
            if (point.x < min.position.x || point.y > max.position.y)
            {
                return false;
            }
            if (point.x > max.position.x)
            {
                point.x = max.position.x;
            }
            if (point.y < min.position.y)
            {
                point.y = min.position.y;
            }
            return true;
        }

        /// <summary>
        /// Remplit la 1è ligne de cache
        /// </summary>
        private void FillFirstRowCache()
        {
            CacheFirstCorner(voxels[0]);
            int i;

            for (i = 0; i < resolution - 1; ++i)
            {
                CacheNextEdgeAndCorner(i * 2, voxels[i], voxels[i + 1]);
            }
            if (xNeighbor != null)
            {
                dummyX.BecomeXDummyOf(xNeighbor.voxels[0], gridSize);
                CacheNextEdgeAndCorner(i * 2, voxels[i], dummyX);
            }
        }

        /// <summary>
        /// Cache le 1er voxel (bas gauche)
        /// </summary>
        private void CacheFirstCorner(Voxel voxel)
        {
            if (voxel.state)
            {
                rowCacheMax[0] = vertices.Count;
                vertices.Add(voxel.position);
            }
        }

        /// <summary>
        /// Cache l'edge et le voxel suivant
        /// </summary>
        /// <param name="i">Position du voxel dans le cache</param>
        /// <param name="xMin">Voxel de gauche</param>
        /// <param name="xMax">Voxel de droite</param>
        private void CacheNextEdgeAndCorner(int i, Voxel xMin, Voxel xMax)
        {
            if (xMin.state != xMax.state)
            {
                rowCacheMax[i + 1] = vertices.Count;
                Vector3 p = new(xMin.xEdge, xMin.position.y, 0f);
                vertices.Add(p);
            }
            if (xMax.state)
            {
                rowCacheMax[i + 2] = vertices.Count;
                vertices.Add(xMax.position);
            }
        }

        /// <summary>
        /// Cache l'edge du milieu
        /// </summary>
        /// <param name="yMin">Voxel du milieu gauche</param>
        /// <param name="yMax">Voxel du milieu droit</param>
        private void CacheNextMiddleEdge(Voxel yMin, Voxel yMax)
        {
            edgeCacheMin = edgeCacheMax;
            if (yMin.state != yMax.state)
            {
                edgeCacheMax = vertices.Count;
                Vector3 p = new(yMin.position.x, yMin.yEdge, 0f);
                vertices.Add(p);
            }
        }

        /// <summary>
        /// Echange les lignes de cache
        /// </summary>
        private void SwapRowCaches()
        {
            (rowCacheMax, rowCacheMin) = (rowCacheMin, rowCacheMax);
        }

        /// <summary>
        /// Calcule les intersections
        /// </summary>
        /// <param name="stencil">La brosse</param>
        /// <param name="xStart">Limite de la zone rectangulaire affectée par la brosse</param>
        /// <param name="xEnd">Limite de la zone rectangulaire affectée par la brosse</param>
        /// <param name="yStart">Limite de la zone rectangulaire affectée par la brosse</param>
        /// <param name="yEnd">Limite de la zone rectangulaire affectée par la brosse</param>
        private void SetCrossings(VoxelStencil stencil, int xStart, int xEnd, int yStart, int yEnd)
        {
            bool crossHorizontalGap = false;
            bool includeLastVerticalRow = false;
            bool crossVerticalGap = false;

            if (xStart > 0)
            {
                xStart -= 1;
            }
            if (xEnd == resolution - 1)
            {
                xEnd -= 1;
                crossHorizontalGap = xNeighbor != null;
            }
            if (yStart > 0)
            {
                yStart -= 1;
            }
            if (yEnd == resolution - 1)
            {
                yEnd -= 1;
                includeLastVerticalRow = true;
                crossVerticalGap = yNeighbor != null;
            }

            Voxel a, b;
            for (int y = yStart; y <= yEnd; y++)
            {
                int i = y * resolution + xStart;
                b = voxels[i];
                for (int x = xStart; x <= xEnd; x++, i++)
                {
                    a = b;
                    b = voxels[i + 1];
                    stencil.SetHorizontalCrossing(a, b);
                    stencil.SetVerticalCrossing(a, voxels[i + resolution]);
                }
                stencil.SetVerticalCrossing(b, voxels[i + resolution]);
                if (crossHorizontalGap)
                {
                    dummyX.BecomeXDummyOf(xNeighbor.voxels[y * resolution], gridSize);
                    stencil.SetHorizontalCrossing(b, dummyX);
                }
            }

            if (includeLastVerticalRow)
            {
                int i = voxels.Length - resolution + xStart;
                b = voxels[i];
                for (int x = xStart; x <= xEnd; x++, i++)
                {
                    a = b;
                    b = voxels[i + 1];
                    stencil.SetHorizontalCrossing(a, b);
                    if (crossVerticalGap)
                    {
                        dummyY.BecomeYDummyOf(yNeighbor.voxels[x], gridSize);
                        stencil.SetVerticalCrossing(a, dummyY);
                    }
                }
                if (crossVerticalGap)
                {
                    dummyY.BecomeYDummyOf(yNeighbor.voxels[xEnd + 1], gridSize);
                    stencil.SetVerticalCrossing(b, dummyY);
                }
                if (crossHorizontalGap)
                {
                    dummyX.BecomeXDummyOf(xNeighbor.voxels[voxels.Length - resolution], gridSize);
                    stencil.SetHorizontalCrossing(b, dummyX);
                }
            }
        }

        #endregion
    }
}