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

        /// <summary>
        /// Prefab du mesh du chunk
        /// </summary>
        [Tooltip("Prefab du mesh du chunk")]
        public VoxelGridSurface surfacePrefab;

        /// <summary>
        /// Prefab du mesh du chunk
        /// </summary>
        [Tooltip("Prefab du mesh du chunk")]
        public VoxelGridWall wallPrefab;

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
        private VoxelGridSurface surface;

        /// <summary>
        /// Mesh généré
        /// </summary>
        private VoxelGridWall wall;

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
        /// Cosinus de la limite autorisée pour un angle d'une section du mesh
        /// </summary>
        private float sharpFeatureLimit;

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// init
        /// </summary>
        /// <param name="resolution">Résolution des voxels pour ce chunk</param>
        /// <param name="size">Taille du chunk</param>
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

            surface = Instantiate(surfacePrefab);
            surface.transform.parent = transform;
            surface.transform.localPosition = Vector3.zero;
            surface.Initialize(resolution);

            wall = Instantiate(wallPrefab);
            wall.transform.parent = transform;
            wall.transform.localPosition = Vector3.zero;
            wall.Initialize(resolution);

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
            surface.Clear();
            wall.Clear();

            FillFirstRowCache();
            TriangulateCellRows();

            if (yNeighbor != null)
            {
                TriangulateGapRow();
            }

            surface.Apply();
            wall.Apply();
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
                    CacheNextEdgeAndCorner(x, c, d);
                    CacheNextMiddleEdge(b, d);
                    TriangulateCell(x, a, b, c, d);
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
            int cacheIndex = resolution - 1;
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
                CacheNextEdgeAndCorner(x, dummyT, dummyY);
                CacheNextMiddleEdge(voxels[x + offset + 1], dummyY);
                TriangulateCell(x, voxels[x + offset], voxels[x + offset + 1], dummyT, dummyY);
            }

            if (xNeighbor != null)
            {
                dummyT.BecomeXYDummyOf(xyNeighbor.voxels[0], gridSize);
                CacheNextEdgeAndCorner(cells, dummyY, dummyT);
                CacheNextMiddleEdge(dummyX, dummyT);
                TriangulateCell(cells, voxels[^1], dummyX, dummyY, dummyT);
            }
        }

        private void TriangulateCase0(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
        }

        private void TriangulateCase15(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            surface.AddQuadABCD(i);
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
                    surface.AddQuadA(i, point);
                    wall.AddACAB(i, point);
                    return;
                }
            }

            surface.AddTriangleA(i);
            wall.AddACAB(i);
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
                    surface.AddQuadB(i, point);
                    wall.AddABBD(i, point);
                    return;
                }
            }

            surface.AddTriangleB(i);
            wall.AddABBD(i);
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
                    surface.AddQuadC(i, point);
                    wall.AddCDAC(i, point);
                    return;
                }
            }
            surface.AddTriangleC(i);
            wall.AddCDAC(i);
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
                    surface.AddQuadD(i, point);
                    wall.AddBDCD(i, point);
                    return;
                }
            }
            surface.AddTriangleD(i);
            wall.AddBDCD(i);
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
                    surface.AddHexagonABC(i, point);
                    wall.AddCDBD(i, point);
                    return;
                }
            }
            surface.AddPentagonABC(i);
            wall.AddCDBD(i);
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
                    surface.AddHexagonABD(i, point);
                    wall.AddACCD(i, point);
                    return;
                }
            }
            surface.AddPentagonABD(i);
            wall.AddACCD(i);
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
                    surface.AddHexagonACD(i, point);
                    wall.AddBDAB(i, point);
                    return;
                }
            }
            surface.AddPentagonACD(i);
            wall.AddBDAB(i);
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
                    surface.AddHexagonBCD(i, point);
                    wall.AddABAC(i, point);
                    return;
                }
            }
            surface.AddPentagonBCD(i);
            wall.AddABAC(i);
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
                    surface.AddPentagonAB(i, point);
                    wall.AddACBD(i, point);
                    return;
                }
            }
            surface.AddQuadAB(i);
            wall.AddACBD(i);
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
                    surface.AddPentagonAC(i, point);
                    wall.AddCDAB(i, point);
                    return;
                }
            }
            surface.AddQuadAC(i);
            wall.AddCDAB(i);
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
                    surface.AddPentagonBD(i, point);
                    wall.AddABCD(i, point);
                    return;
                }
            }
            surface.AddQuadBD(i);
            wall.AddABCD(i);
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
                    surface.AddPentagonCD(i, point);
                    wall.AddBDAC(i, point);
                    return;
                }
            }
            surface.AddQuadCD(i);
            wall.AddBDAC(i);
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
                    surface.AddQuadB(i, point1);
                    surface.AddQuadC(i, point2);
                    wall.AddABBD(i, point1);
                    wall.AddCDAC(i, point2);
                    return;
                }
                // First sharp.
                if (IsBelowLine(point1, c.XEdgePoint, a.YEdgePoint))
                {
                    TriangulateCase6Connected(i, a, b, c, d);
                    return;
                }
                surface.AddQuadB(i, point1);
                surface.AddTriangleC(i);
                wall.AddABBD(i, point1);
                wall.AddCDAC(i, point2);
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
                surface.AddTriangleB(i);
                surface.AddQuadC(i, point2);
                wall.AddABBD(i, point1);
                wall.AddCDAC(i, point2);
                return;
            }
            // Neither sharp.
            surface.AddTriangleB(i);
            surface.AddTriangleC(i);
            wall.AddABBD(i, point1);
            wall.AddCDAC(i, point2);
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
                    surface.AddPentagonBCToA(i, point);
                    wall.AddABAC(i, point);
                }
                else
                {
                    surface.AddQuadBCToA(i);
                    wall.AddABAC(i, point);
                }
            }
            else
            {
                surface.AddQuadBCToA(i);
                wall.AddABAC(i);
            }

            n1 = c.xNormal;
            n2 = -b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d) && IsBelowLine(point, b.position, c.position))
                {
                    surface.AddPentagonBCToD(i, point);
                    wall.AddCDBD(i, point);
                    return;
                }
            }
            surface.AddQuadBCToD(i);
            wall.AddCDBD(i);
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
                    surface.AddQuadA(i, point1);
                    surface.AddQuadD(i, point2);
                    wall.AddACAB(i, point1);
                    wall.AddBDCD(i, point2);
                    return;
                }
                if (IsBelowLine(point1, b.YEdgePoint, c.XEdgePoint))
                {
                    TriangulateCase9Connected(i, a, b, c, d);
                    return;
                }
                surface.AddQuadA(i, point1);
                surface.AddTriangleD(i);
                wall.AddACAB(i, point1);
                wall.AddBDCD(i, point2);
                return;
            }
            if (sharp2)
            {
                if (IsBelowLine(point2, a.YEdgePoint, a.XEdgePoint))
                {
                    TriangulateCase9Connected(i, a, b, c, d);
                    return;
                }
                surface.AddTriangleA(i);
                surface.AddQuadD(i, point2);
                wall.AddACAB(i, point1);
                wall.AddBDCD(i, point2);
                return;
            }
            surface.AddTriangleA(i);
            surface.AddTriangleD(i);
            wall.AddACAB(i, point1);
            wall.AddBDCD(i, point2);
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
                    surface.AddPentagonADToB(i, point);
                    wall.AddBDAB(i, point);
                }
                else
                {
                    surface.AddQuadADToB(i);
                    wall.AddBDAB(i, point);
                }
            }
            else
            {
                surface.AddQuadADToB(i);
                wall.AddBDAB(i);
            }

            n1 = c.xNormal;
            n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
                if (IsInsideCell(point, a, d) && IsBelowLine(point, d.position, a.position))
                {
                    surface.AddPentagonADToC(i, point);
                    wall.AddACCD(i, point);
                    return;
                }
            }
            surface.AddQuadADToC(i);
            wall.AddACCD(i);
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
                CacheNextEdgeAndCorner(i, voxels[i], voxels[i + 1]);
            }
            if (xNeighbor != null)
            {
                dummyX.BecomeXDummyOf(xNeighbor.voxels[0], gridSize);
                CacheNextEdgeAndCorner(i, voxels[i], dummyX);
            }
        }

        /// <summary>
        /// Cache le 1er voxel (bas gauche)
        /// </summary>
        private void CacheFirstCorner(Voxel voxel)
        {
            if (voxel.state)
            {
                surface.CacheFirstCorner(voxel);
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
                surface.CacheXEdge(i, xMin);
                wall.CacheXEdge(i, xMin);
            }
            if (xMax.state)
            {
                surface.CacheNextCorner(i, xMax);
            }
        }

        /// <summary>
        /// Cache l'edge du milieu
        /// </summary>
        /// <param name="yMin">Voxel du milieu gauche</param>
        /// <param name="yMax">Voxel du milieu droit</param>
        private void CacheNextMiddleEdge(Voxel yMin, Voxel yMax)
        {
            surface.PrepareCacheForNextCell();
            wall.PrepareCacheForNextCell();
            if (yMin.state != yMax.state)
            {
                surface.CacheYEdge(yMin);
                wall.CacheYEdge(yMin);
            }
        }

        /// <summary>
        /// Echange les lignes de cache
        /// </summary>
        private void SwapRowCaches()
        {
            surface.PrepareCacheForNextRow();
            wall.PrepareCacheForNextRow();
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