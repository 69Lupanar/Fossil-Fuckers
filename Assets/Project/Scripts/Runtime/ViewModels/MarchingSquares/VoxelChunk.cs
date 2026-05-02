using UnityEngine;

namespace Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares
{
    /// <summary>
    /// Chunk contenant une grille de voxels
    /// </summary>
    [SelectionBase]
    public class VoxelChunk : MonoBehaviour
    {
        #region Propriétés

        /// <summary>
        /// Voxels
        /// </summary>
        public Voxel[] Voxels { get; private set; }

        #endregion

        #region Variables Unity

        /// <summary>
        /// Prefab du mesh du chunk
        /// </summary>
        [field: SerializeField, Tooltip("Prefab du mesh du chunk")]
        private VoxelChunkSurface SurfacePrefab { get; set; }

        /// <summary>
        /// Prefab du mesh du chunk
        /// </summary>
        [field: SerializeField, Tooltip("Prefab du mesh du chunk")]
        private VoxelChunkWall WallPrefab { get; set; }

        /// <summary>
        /// Prefab du mesh du chunk
        /// </summary>
        [field: SerializeField, Tooltip("Liste de Materials pour les surfaces et les murs")]
        private VoxelMaterials[] Materials { get; set; }

        #endregion

        #region Variables d'instance

        /// <summary>
        /// Taille du chunk
        /// </summary>
        private int _resolution;

        /// <summary>
        /// Taille d'un voxel
        /// </summary>
        private float _voxelSize;

        /// <summary>
        /// Taille du chunk
        /// </summary>
        private float _chunkSize;

        /// <summary>
        /// Renderers pour les surfaces/murs
        /// </summary>
        private VoxelRenderer[] _renderers;

        /// <summary>
        /// Chunk voisin
        /// </summary>
        [HideInInspector]
        public VoxelChunk xNeighbor, yNeighbor, xyNeighbor;

        /// <summary>
        /// Faux voxel utilisé lors de la triangulation pour relier les chunks entre eux
        /// </summary>
        private Voxel _dummyX, _dummyY, _dummyT;

        /// <summary>
        /// Celle factice pour faciliter le déplacement des valeurs
        /// </summary>
        private readonly VoxelCell _cell = new();

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// init
        /// </summary>
        /// <param name="voxelResolution">Résolution des voxels pour ce chunk</param>
        /// <param name="chunkSize">Taille du chunk</param>
        /// <param name="maxFeatureAngle">Angle max d'une section du mesh qui peut apparaître</param>
        /// <param name="maxParallelAngle">Angle max d'une section du mesh qui peut apparaître</param>
        public void Initialize(int voxelResolution, float chunkSize, float maxFeatureAngle, float maxParallelAngle)
        {
            _cell.sharpFeatureLimit = Mathf.Cos(maxFeatureAngle * Mathf.Deg2Rad);
            _cell.parallelLimit = Mathf.Cos(maxParallelAngle * Mathf.Deg2Rad);
            _resolution = voxelResolution;
            _voxelSize = chunkSize / voxelResolution;
            _chunkSize = chunkSize;
            Voxels = new Voxel[voxelResolution * voxelResolution];

            _dummyX = new Voxel();
            _dummyY = new Voxel();
            _dummyT = new Voxel();

            for (int i = 0, y = 0; y < voxelResolution; ++y)
            {
                for (int x = 0; x < voxelResolution; ++x, ++i)
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
            int xStart = Mathf.Max(0, (int)(stencil.XStart / _voxelSize));
            int xEnd = Mathf.Min((int)(stencil.XEnd / _voxelSize), _resolution - 1);
            int yStart = Mathf.Max(0, (int)(stencil.YStart / _voxelSize));
            int yEnd = Mathf.Min((int)(stencil.YEnd / _voxelSize), _resolution - 1);

            // On traverse toute la zone rectangulaire englobant la brosse
            // pour modifier les voxels concernés

            for (int y = yStart; y <= yEnd; ++y)
            {
                int i = y * _resolution + xStart;

                for (int x = xStart; x <= xEnd; ++x, ++i)
                {
                    stencil.Apply(Voxels[i]);
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

            _renderers = new VoxelRenderer[Materials.Length + 1];

            for (int i = 0; i < Materials.Length; ++i)
            {
                VoxelChunkSurface surface = Instantiate(SurfacePrefab);
                surface.transform.parent = transform;
                surface.transform.localPosition = Vector3.zero;
                surface.Initialize(_resolution, Materials[i].surfaceMaterial);

                VoxelChunkWall wall = Instantiate(WallPrefab);
                wall.transform.parent = transform;
                wall.transform.localPosition = Vector3.zero;
                wall.Initialize(_resolution, Materials[i].wallMaterial);

                _renderers[i + 1] = new VoxelRenderer(surface, wall);
            }
        }

        /// <summary>
        /// Crée un voxel aux coordonnées renseignées
        /// </summary>
        /// <param name="voxelSpacing">Espacement entre les voxels</param>
        private void CreateVoxel(int i, int x, int y)
        {
            Voxels[i] = new Voxel(x, y, _voxelSize);
        }

        /// <summary>
        /// Màj le mesh
        /// </summary>
        private void Refresh()
        {
            Triangulate();
        }

        /// <summary>
        /// Calcule les triangles du mesh
        /// </summary>
        private void Triangulate()
        {
            for (int i = 1; i < _renderers.Length; ++i)
            {
                _renderers[i].Clear();
            }

            FillFirstRowCache();
            TriangulateCellRows();

            if (yNeighbor != null)
            {
                TriangulateGapRow();
            }

            for (int i = 1; i < _renderers.Length; ++i)
            {
                _renderers[i].Apply();
            }
        }

        /// <summary>
        /// Calcule les triangles de chaque rangée de cellules
        /// </summary>
        private void TriangulateCellRows()
        {
            int cells = _resolution - 1;
            for (int i = 0, y = 0; y < cells; ++y, ++i)
            {
                SwapRowCaches();
                CacheFirstCorner(Voxels[i + _resolution]);
                CacheNextMiddleEdge(Voxels[i], Voxels[i + _resolution]);

                for (int x = 0; x < cells; ++x, ++i)
                {
                    Voxel
                     a = Voxels[i],
                     b = Voxels[i + 1],
                     c = Voxels[i + _resolution],
                     d = Voxels[i + _resolution + 1];
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
            Voxel dummySwap = _dummyT;
            dummySwap.BecomeXDummyOf(xNeighbor.Voxels[i + 1], _chunkSize);
            _dummyT = _dummyX;
            _dummyX = dummySwap;
            int cacheIndex = _resolution - 1;
            CacheNextEdgeAndCorner(cacheIndex, Voxels[i + _resolution], _dummyX);
            CacheNextMiddleEdge(_dummyT, _dummyX);
            TriangulateCell(cacheIndex, Voxels[i], _dummyT, Voxels[i + _resolution], _dummyX);
        }

        /// <summary>
        /// Calcules les truangles des cellules séparant deux chunks
        /// </summary>
        private void TriangulateGapRow()
        {
            _dummyY.BecomeYDummyOf(yNeighbor.Voxels[0], _chunkSize);
            int cells = _resolution - 1;
            int offset = cells * _resolution;
            SwapRowCaches();
            CacheFirstCorner(_dummyY);
            CacheNextMiddleEdge(Voxels[cells * _resolution], _dummyY);

            for (int x = 0; x < cells; ++x)
            {
                Voxel dummySwap = _dummyT;
                dummySwap.BecomeYDummyOf(yNeighbor.Voxels[x + 1], _chunkSize);
                _dummyT = _dummyY;
                _dummyY = dummySwap;
                CacheNextEdgeAndCorner(x, _dummyT, _dummyY);
                CacheNextMiddleEdge(Voxels[x + offset + 1], _dummyY);
                TriangulateCell(x, Voxels[x + offset], Voxels[x + offset + 1], _dummyT, _dummyY);
            }

            if (xNeighbor != null)
            {
                _dummyT.BecomeXYDummyOf(xyNeighbor.Voxels[0], _chunkSize);
                CacheNextEdgeAndCorner(cells, _dummyY, _dummyT);
                CacheNextMiddleEdge(_dummyX, _dummyT);
                TriangulateCell(cells, Voxels[^1], _dummyX, _dummyY, _dummyT);
            }
        }

        /// <summary>
        /// Remplit la 1è ligne de cache
        /// </summary>
        private void FillFirstRowCache()
        {
            CacheFirstCorner(Voxels[0]);
            int i;

            for (i = 0; i < _resolution - 1; ++i)
            {
                CacheNextEdgeAndCorner(i, Voxels[i], Voxels[i + 1]);
            }
            if (xNeighbor != null)
            {
                _dummyX.BecomeXDummyOf(xNeighbor.Voxels[0], _chunkSize);
                CacheNextEdgeAndCorner(i, Voxels[i], _dummyX);
            }
        }

        /// <summary>
        /// Cache le 1er voxel (bas gauche)
        /// </summary>
        private void CacheFirstCorner(Voxel voxel)
        {
            if (voxel.Filled)
            {
                _renderers[voxel.state].CacheFirstCorner(voxel);
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
                        _renderers[xMin.state].CacheXEdge(i, xMin);
                        _renderers[xMax.state].CacheXEdge(i, xMin);
                    }
                    else
                    {
                        _renderers[xMin.state].CacheXEdgeWithWall(i, xMin);
                    }
                }
                else
                {
                    _renderers[xMax.state].CacheXEdgeWithWall(i, xMin);
                }
            }
            if (xMax.Filled)
            {
                _renderers[xMax.state].CacheNextCorner(i, xMax);
            }
        }

        /// <summary>
        /// Cache l'edge du milieu
        /// </summary>
        /// <param name="yMin">Voxel du milieu gauche</param>
        /// <param name="yMax">Voxel du milieu droit</param>
        private void CacheNextMiddleEdge(Voxel yMin, Voxel yMax)
        {
            for (int i = 1; i < _renderers.Length; ++i)
            {
                _renderers[i].PrepareCacheForNextCell();
            }

            if (yMin.state != yMax.state)
            {
                if (yMin.Filled)
                {
                    if (yMax.Filled)
                    {
                        _renderers[yMin.state].CacheYEdge(yMin);
                        _renderers[yMax.state].CacheYEdge(yMin);
                    }
                    else
                    {
                        _renderers[yMin.state].CacheYEdgeWithWall(yMin);
                    }
                }
                else
                {
                    _renderers[yMax.state].CacheYEdgeWithWall(yMin);
                }
            }
        }

        /// <summary>
        /// Echange les lignes de cache
        /// </summary>
        private void SwapRowCaches()
        {
            for (int i = 1; i < _renderers.Length; ++i)
            {
                _renderers[i].PrepareCacheForNextRow();
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
            if (xEnd == _resolution - 1)
            {
                xEnd -= 1;
                crossHorizontalGap = xNeighbor != null;
            }
            if (yStart > 0)
            {
                yStart -= 1;
            }
            if (yEnd == _resolution - 1)
            {
                yEnd -= 1;
                includeLastVerticalRow = true;
                crossVerticalGap = yNeighbor != null;
            }

            Voxel a, b;
            for (int y = yStart; y <= yEnd; y++)
            {
                int i = y * _resolution + xStart;
                b = Voxels[i];
                for (int x = xStart; x <= xEnd; x++, i++)
                {
                    a = b;
                    b = Voxels[i + 1];
                    stencil.SetHorizontalCrossing(a, b);
                    stencil.SetVerticalCrossing(a, Voxels[i + _resolution]);
                }
                stencil.SetVerticalCrossing(b, Voxels[i + _resolution]);
                if (crossHorizontalGap)
                {
                    _dummyX.BecomeXDummyOf(xNeighbor.Voxels[y * _resolution], _chunkSize);
                    stencil.SetHorizontalCrossing(b, _dummyX);
                }
            }

            if (includeLastVerticalRow)
            {
                int i = Voxels.Length - _resolution + xStart;
                b = Voxels[i];
                for (int x = xStart; x <= xEnd; x++, i++)
                {
                    a = b;
                    b = Voxels[i + 1];
                    stencil.SetHorizontalCrossing(a, b);
                    if (crossVerticalGap)
                    {
                        _dummyY.BecomeYDummyOf(yNeighbor.Voxels[x], _chunkSize);
                        stencil.SetVerticalCrossing(a, _dummyY);
                    }
                }
                if (crossVerticalGap)
                {
                    _dummyY.BecomeYDummyOf(yNeighbor.Voxels[xEnd + 1], _chunkSize);
                    stencil.SetVerticalCrossing(b, _dummyY);
                }
                if (crossHorizontalGap)
                {
                    _dummyX.BecomeXDummyOf(xNeighbor.Voxels[Voxels.Length - _resolution], _chunkSize);
                    stencil.SetHorizontalCrossing(b, _dummyX);
                }
            }
        }

        private void TriangulateCell(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            _cell.i = i;
            _cell.a = a;
            _cell.b = b;
            _cell.c = c;
            _cell.d = d;

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
            FeaturePoint f = _cell.FeatureNE;
            FillABC(f);
            FillD(f);
        }

        private void Triangulate0010()
        {
            FeaturePoint f = _cell.FeatureNW;
            FillABD(f);
            FillC(f);
        }

        private void Triangulate0100()
        {
            FeaturePoint f = _cell.FeatureSE;
            FillACD(f);
            FillB(f);
        }

        private void Triangulate0111()
        {
            FeaturePoint f = _cell.FeatureSW;
            FillA(f);
            FillBCD(f);
        }

        private void Triangulate0011()
        {
            FeaturePoint f = _cell.FeatureEW;
            FillAB(f);
            FillCD(f);
        }

        private void Triangulate0101()
        {
            FeaturePoint f = _cell.FeatureNS;
            FillAC(f);
            FillBD(f);
        }

        private void Triangulate0012()
        {
            FeaturePoint f = _cell.FeatureNEW;
            FillAB(f);
            FillC(f);
            FillD(f);
        }

        private void Triangulate0102()
        {
            FeaturePoint f = _cell.FeatureNSE;
            FillAC(f);
            FillB(f);
            FillD(f);
        }

        private void Triangulate0121()
        {
            FeaturePoint f = _cell.FeatureNSW;
            FillA(f);
            FillBD(f);
            FillC(f);
        }

        private void Triangulate0122()
        {
            FeaturePoint f = _cell.FeatureSEW;
            FillA(f);
            FillB(f);
            FillCD(f);
        }

        private void Triangulate0110()
        {
            FeaturePoint
                fA = _cell.FeatureSW, fB = _cell.FeatureSE,
                fC = _cell.FeatureNW, fD = _cell.FeatureNE;

            if (_cell.HasConnectionAD(fA, fD))
            {
                fB.exists &= _cell.IsInsideABD(fB.position);
                fC.exists &= _cell.IsInsideACD(fC.position);
                FillADToB(fB);
                FillADToC(fC);
                FillB(fB);
                FillC(fC);
            }
            else if (_cell.HasConnectionBC(fB, fC))
            {
                fA.exists &= _cell.IsInsideABC(fA.position);
                fD.exists &= _cell.IsInsideBCD(fD.position);
                FillA(fA);
                FillD(fD);
                FillBCToA(fA);
                FillBCToD(fD);
            }
            else if (_cell.a.Filled && _cell.b.Filled)
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
                fA = _cell.FeatureSW, fB = _cell.FeatureSE,
                fC = _cell.FeatureNW, fD = _cell.FeatureNE;

            if (_cell.HasConnectionBC(fB, fC))
            {
                fA.exists &= _cell.IsInsideABC(fA.position);
                fD.exists &= _cell.IsInsideBCD(fD.position);
                FillA(fA);
                FillD(fD);
                FillBCToA(fA);
                FillBCToD(fD);
            }
            else if (_cell.b.Filled || _cell.HasConnectionAD(fA, fD))
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
                fA = _cell.FeatureSW, fB = _cell.FeatureSE,
                fC = _cell.FeatureNW, fD = _cell.FeatureNE;

            if (_cell.HasConnectionAD(fA, fD))
            {
                fB.exists &= _cell.IsInsideABD(fB.position);
                fC.exists &= _cell.IsInsideACD(fC.position);
                FillADToB(fB);
                FillADToC(fC);
                FillB(fB);
                FillC(fC);
            }
            else if (_cell.a.Filled || _cell.HasConnectionBC(fB, fC))
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
                _cell.FeatureSW, _cell.FeatureSE,
                _cell.FeatureNW, _cell.FeatureNE);
        }

        private void FillA(FeaturePoint f)
        {
            if (_cell.a.Filled)
            {
                _renderers[_cell.a.state].FillA(_cell, f);
            }
        }

        private void FillB(FeaturePoint f)
        {
            if (_cell.b.Filled)
            {
                _renderers[_cell.b.state].FillB(_cell, f);
            }
        }

        private void FillC(FeaturePoint f)
        {
            if (_cell.c.Filled)
            {
                _renderers[_cell.c.state].FillC(_cell, f);
            }
        }

        private void FillD(FeaturePoint f)
        {
            if (_cell.d.Filled)
            {
                _renderers[_cell.d.state].FillD(_cell, f);
            }
        }

        private void FillABC(FeaturePoint f)
        {
            if (_cell.a.Filled)
            {
                _renderers[_cell.a.state].FillABC(_cell, f);
            }
        }

        private void FillABD(FeaturePoint f)
        {
            if (_cell.a.Filled)
            {
                _renderers[_cell.a.state].FillABD(_cell, f);
            }
        }

        private void FillACD(FeaturePoint f)
        {
            if (_cell.a.Filled)
            {
                _renderers[_cell.a.state].FillACD(_cell, f);
            }
        }

        private void FillBCD(FeaturePoint f)
        {
            if (_cell.b.Filled)
            {
                _renderers[_cell.b.state].FillBCD(_cell, f);
            }
        }

        private void FillAB(FeaturePoint f)
        {
            if (_cell.a.Filled)
            {
                _renderers[_cell.a.state].FillAB(_cell, f);
            }
        }

        private void FillAC(FeaturePoint f)
        {
            if (_cell.a.Filled)
            {
                _renderers[_cell.a.state].FillAC(_cell, f);
            }
        }

        private void FillBD(FeaturePoint f)
        {
            if (_cell.b.Filled)
            {
                _renderers[_cell.b.state].FillBD(_cell, f);
            }
        }

        private void FillCD(FeaturePoint f)
        {
            if (_cell.c.Filled)
            {
                _renderers[_cell.c.state].FillCD(_cell, f);
            }
        }

        private void FillADToB(FeaturePoint f)
        {
            if (_cell.a.Filled)
            {
                _renderers[_cell.a.state].FillADToB(_cell, f);
            }
        }

        private void FillADToC(FeaturePoint f)
        {
            if (_cell.a.Filled)
            {
                _renderers[_cell.a.state].FillADToC(_cell, f);
            }
        }

        private void FillBCToA(FeaturePoint f)
        {
            if (_cell.b.Filled)
            {
                _renderers[_cell.b.state].FillBCToA(_cell, f);
            }
        }

        private void FillBCToD(FeaturePoint f)
        {
            if (_cell.b.Filled)
            {
                _renderers[_cell.b.state].FillBCToD(_cell, f);
            }
        }

        private void FillABCD()
        {
            if (_cell.a.Filled)
            {
                _renderers[_cell.a.state].FillABCD(_cell);
            }
        }

        private void FillJoinedCorners(
            FeaturePoint fA, FeaturePoint fB, FeaturePoint fC, FeaturePoint fD)
        {

            FeaturePoint point = FeaturePoint.Average(fA, fB, fC, fD);
            if (!point.exists)
            {
                point.position = _cell.AverageNESW;
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