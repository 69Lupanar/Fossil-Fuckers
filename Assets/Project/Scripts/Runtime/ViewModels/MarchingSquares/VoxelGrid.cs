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

        /// <summary>
        /// Prefab du mesh du chunk
        /// </summary>
        [Tooltip("Liste de Materials pour les surfaces et les murs")]
        public VoxelMaterials[] materials;

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
        /// Renderers pour les surfaces/murs
        /// </summary>
        private VoxelRenderer[] renderers;

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
        /// Celle factice pour faciliter le déplacement des valeurs
        /// </summary>
        private VoxelCell cell = new();

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// init
        /// </summary>
        /// <param name="resolution">Résolution des voxels pour ce chunk</param>
        /// <param name="size">Taille du chunk</param>
        /// <param name="maxFeatureAngle">Angle max d'une section du mesh qui peut apparaître</param>
        public void Initialize(int resolution, float size, float maxFeatureAngle, float maxParallelAngle)
        {
            cell.sharpFeatureLimit = Mathf.Cos(maxFeatureAngle * Mathf.Deg2Rad);
            cell.parallelLimit = Mathf.Cos(maxParallelAngle * Mathf.Deg2Rad);
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

            CreateRenderers();
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
        /// Crée les renderers pour les surfaces et murs
        /// </summary>
        private void CreateRenderers()
        {
            // On crée un Renderer de plus que nécessaire
            // pour pouvoir utiliser directement l'état de voxel comme index.
            // Ca nous éviter de soustraire 1 à chaque fois.

            renderers = new VoxelRenderer[materials.Length + 1];

            for (int i = 0; i < materials.Length; ++i)
            {
                VoxelGridSurface surface = Instantiate(surfacePrefab);
                surface.transform.parent = transform;
                surface.transform.localPosition = Vector3.zero;
                surface.Initialize(resolution, materials[i].surfaceMaterial);

                VoxelGridWall wall = Instantiate(wallPrefab);
                wall.transform.parent = transform;
                wall.transform.localPosition = Vector3.zero;
                wall.Initialize(resolution, materials[i].wallMaterial);

                renderers[i + 1] = new VoxelRenderer(surface, wall);
            }
        }

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
                voxelMaterials[i].color = voxels[i].Filled ? Color.black : Color.white;
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
            for (int i = 1; i < renderers.Length; ++i)
            {
                renderers[i].Clear();
            }

            FillFirstRowCache();
            TriangulateCellRows();

            if (yNeighbor != null)
            {
                TriangulateGapRow();
            }

            for (int i = 1; i < renderers.Length; ++i)
            {
                renderers[i].Apply();
            }
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
            if (voxel.Filled)
            {
                renderers[voxel.state].CacheFirstCorner(voxel);
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
                if (xMin.Filled)
                {
                    if (xMax.Filled)
                    {
                        renderers[xMin.state].CacheXEdge(i, xMin);
                        renderers[xMax.state].CacheXEdge(i, xMin);
                    }
                    else
                    {
                        renderers[xMin.state].CacheXEdgeWithWall(i, xMin);
                    }
                }
                else
                {
                    renderers[xMax.state].CacheXEdgeWithWall(i, xMin);
                }
            }
            if (xMax.Filled)
            {
                renderers[xMax.state].CacheNextCorner(i, xMax);
            }
        }

        /// <summary>
        /// Cache l'edge du milieu
        /// </summary>
        /// <param name="yMin">Voxel du milieu gauche</param>
        /// <param name="yMax">Voxel du milieu droit</param>
        private void CacheNextMiddleEdge(Voxel yMin, Voxel yMax)
        {
            for (int i = 1; i < renderers.Length; ++i)
            {
                renderers[i].PrepareCacheForNextCell();
            }

            if (yMin.state != yMax.state)
            {
                if (yMin.Filled)
                {
                    if (yMax.Filled)
                    {
                        renderers[yMin.state].CacheYEdge(yMin);
                        renderers[yMax.state].CacheYEdge(yMin);
                    }
                    else
                    {
                        renderers[yMin.state].CacheYEdgeWithWall(yMin);
                    }
                }
                else
                {
                    renderers[yMax.state].CacheYEdgeWithWall(yMin);
                }
            }
        }

        /// <summary>
        /// Echange les lignes de cache
        /// </summary>
        private void SwapRowCaches()
        {
            for (int i = 1; i < renderers.Length; ++i)
            {
                renderers[i].PrepareCacheForNextRow();
            }
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

        private void TriangulateCell(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            cell.i = i;
            cell.a = a;
            cell.b = b;
            cell.c = c;
            cell.d = d;

            if (a.state == b.state)
            {
                if (a.state == c.state)
                {
                    if (a.state == d.state)
                    {
                        Triangulate0000();
                    }
                    else
                    {
                        Triangulate0001();
                    }
                }
                else
                {
                    if (a.state == d.state)
                    {
                        Triangulate0010();
                    }
                    else if (c.state == d.state)
                    {
                        Triangulate0011();
                    }
                    else
                    {
                        Triangulate0012();
                    }
                }
            }
            else
            {
                if (a.state == c.state)
                {
                    if (a.state == d.state)
                    {
                        Triangulate0100();
                    }
                    else if (b.state == d.state)
                    {
                        Triangulate0101();
                    }
                    else
                    {
                        Triangulate0102();
                    }
                }
                else if (b.state == c.state)
                {
                    if (a.state == d.state)
                    {
                        Triangulate0110();
                    }
                    else if (b.state == d.state)
                    {
                        Triangulate0111();
                    }
                    else
                    {
                        Triangulate0112();
                    }
                }
                else
                {
                    if (a.state == d.state)
                    {
                        Triangulate0120();
                    }
                    else if (b.state == d.state)
                    {
                        Triangulate0121();
                    }
                    else if (c.state == d.state)
                    {
                        Triangulate0122();
                    }
                    else
                    {
                        Triangulate0123();
                    }
                }
            }
        }

        private void Triangulate0000()
        {
            FillABCD();
        }

        private void Triangulate0001()
        {
            FeaturePoint f = cell.FeatureNE;
            FillABC(f);
            FillD(f);
        }

        private void Triangulate0010()
        {
            FeaturePoint f = cell.FeatureNW;
            FillABD(f);
            FillC(f);
        }

        private void Triangulate0100()
        {
            FeaturePoint f = cell.FeatureSE;
            FillACD(f);
            FillB(f);
        }

        private void Triangulate0111()
        {
            FeaturePoint f = cell.FeatureSW;
            FillA(f);
            FillBCD(f);
        }

        private void Triangulate0011()
        {
            FeaturePoint f = cell.FeatureEW;
            FillAB(f);
            FillCD(f);
        }

        private void Triangulate0101()
        {
            FeaturePoint f = cell.FeatureNS;
            FillAC(f);
            FillBD(f);
        }

        private void Triangulate0012()
        {
            FeaturePoint f = cell.FeatureNEW;
            FillAB(f);
            FillC(f);
            FillD(f);
        }

        private void Triangulate0102()
        {
            FeaturePoint f = cell.FeatureNSE;
            FillAC(f);
            FillB(f);
            FillD(f);
        }

        private void Triangulate0121()
        {
            FeaturePoint f = cell.FeatureNSW;
            FillA(f);
            FillBD(f);
            FillC(f);
        }

        private void Triangulate0122()
        {
            FeaturePoint f = cell.FeatureSEW;
            FillA(f);
            FillB(f);
            FillCD(f);
        }

        private void Triangulate0110()
        {
            FeaturePoint
                fA = cell.FeatureSW, fB = cell.FeatureSE,
                fC = cell.FeatureNW, fD = cell.FeatureNE;

            if (cell.HasConnectionAD(fA, fD))
            {
                fB.exists &= cell.IsInsideABD(fB.position);
                fC.exists &= cell.IsInsideACD(fC.position);
                FillADToB(fB);
                FillADToC(fC);
                FillB(fB);
                FillC(fC);
            }
            else if (cell.HasConnectionBC(fB, fC))
            {
                fA.exists &= cell.IsInsideABC(fA.position);
                fD.exists &= cell.IsInsideBCD(fD.position);
                FillA(fA);
                FillD(fD);
                FillBCToA(fA);
                FillBCToD(fD);
            }
            else if (cell.a.Filled && cell.b.Filled)
            {
                FillJoinedCorners(fA, fB, fC, fD);
            }
            else
            {
                FillA(fA);
                FillB(fB);
                FillC(fC);
                FillD(fD);
            }
        }

        private void Triangulate0112()
        {
            FeaturePoint
                fA = cell.FeatureSW, fB = cell.FeatureSE,
                fC = cell.FeatureNW, fD = cell.FeatureNE;

            if (cell.HasConnectionBC(fB, fC))
            {
                fA.exists &= cell.IsInsideABC(fA.position);
                fD.exists &= cell.IsInsideBCD(fD.position);
                FillA(fA);
                FillD(fD);
                FillBCToA(fA);
                FillBCToD(fD);
            }
            else if (cell.b.Filled || cell.HasConnectionAD(fA, fD))
            {
                FillJoinedCorners(fA, fB, fC, fD);
            }
            else
            {
                FillA(fA);
                FillD(fD);
            }
        }

        private void Triangulate0120()
        {
            FeaturePoint
                fA = cell.FeatureSW, fB = cell.FeatureSE,
                fC = cell.FeatureNW, fD = cell.FeatureNE;

            if (cell.HasConnectionAD(fA, fD))
            {
                fB.exists &= cell.IsInsideABD(fB.position);
                fC.exists &= cell.IsInsideACD(fC.position);
                FillADToB(fB);
                FillADToC(fC);
                FillB(fB);
                FillC(fC);
            }
            else if (cell.a.Filled || cell.HasConnectionBC(fB, fC))
            {
                FillJoinedCorners(fA, fB, fC, fD);
            }
            else
            {
                FillB(fB);
                FillC(fC);
            }
        }

        private void Triangulate0123()
        {
            FillJoinedCorners(
                cell.FeatureSW, cell.FeatureSE,
                cell.FeatureNW, cell.FeatureNE);
        }

        private void FillA(FeaturePoint f)
        {
            if (cell.a.Filled)
            {
                renderers[cell.a.state].FillA(cell, f);
            }
        }

        private void FillB(FeaturePoint f)
        {
            if (cell.b.Filled)
            {
                renderers[cell.b.state].FillB(cell, f);
            }
        }

        private void FillC(FeaturePoint f)
        {
            if (cell.c.Filled)
            {
                renderers[cell.c.state].FillC(cell, f);
            }
        }

        private void FillD(FeaturePoint f)
        {
            if (cell.d.Filled)
            {
                renderers[cell.d.state].FillD(cell, f);
            }
        }

        private void FillABC(FeaturePoint f)
        {
            if (cell.a.Filled)
            {
                renderers[cell.a.state].FillABC(cell, f);
            }
        }

        private void FillABD(FeaturePoint f)
        {
            if (cell.a.Filled)
            {
                renderers[cell.a.state].FillABD(cell, f);
            }
        }

        private void FillACD(FeaturePoint f)
        {
            if (cell.a.Filled)
            {
                renderers[cell.a.state].FillACD(cell, f);
            }
        }

        private void FillBCD(FeaturePoint f)
        {
            if (cell.b.Filled)
            {
                renderers[cell.b.state].FillBCD(cell, f);
            }
        }

        private void FillAB(FeaturePoint f)
        {
            if (cell.a.Filled)
            {
                renderers[cell.a.state].FillAB(cell, f);
            }
        }

        private void FillAC(FeaturePoint f)
        {
            if (cell.a.Filled)
            {
                renderers[cell.a.state].FillAC(cell, f);
            }
        }

        private void FillBD(FeaturePoint f)
        {
            if (cell.b.Filled)
            {
                renderers[cell.b.state].FillBD(cell, f);
            }
        }

        private void FillCD(FeaturePoint f)
        {
            if (cell.c.Filled)
            {
                renderers[cell.c.state].FillCD(cell, f);
            }
        }

        private void FillADToB(FeaturePoint f)
        {
            if (cell.a.Filled)
            {
                renderers[cell.a.state].FillADToB(cell, f);
            }
        }

        private void FillADToC(FeaturePoint f)
        {
            if (cell.a.Filled)
            {
                renderers[cell.a.state].FillADToC(cell, f);
            }
        }

        private void FillBCToA(FeaturePoint f)
        {
            if (cell.b.Filled)
            {
                renderers[cell.b.state].FillBCToA(cell, f);
            }
        }

        private void FillBCToD(FeaturePoint f)
        {
            if (cell.b.Filled)
            {
                renderers[cell.b.state].FillBCToD(cell, f);
            }
        }

        private void FillABCD()
        {
            if (cell.a.Filled)
            {
                renderers[cell.a.state].FillABCD(cell);
            }
        }

        private void FillJoinedCorners(
            FeaturePoint fA, FeaturePoint fB, FeaturePoint fC, FeaturePoint fD)
        {

            FeaturePoint point = FeaturePoint.Average(fA, fB, fC, fD);
            if (!point.exists)
            {
                point.position = cell.AverageNESW;
                point.exists = true;
            }
            FillA(point);
            FillB(point);
            FillC(point);
            FillD(point);
        }

        #endregion
    }
}