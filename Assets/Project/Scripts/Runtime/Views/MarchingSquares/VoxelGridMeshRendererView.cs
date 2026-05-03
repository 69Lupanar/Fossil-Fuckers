using Assets.Project.Scripts.Runtime.Models.MarchingSquares;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares.EventArgs;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils;
using Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Views.MarchingSquares
{
    /// <summary>
    /// Charger de trianguler les voxels de la grille
    /// </summary>
    public class VoxelGridMeshRendererView : MonoBehaviour
    {
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
        /// Résolution des voxels pour ce chunk
        /// </summary>
        private int _voxelResolution;

        /// <summary>
        /// Taille du chunk
        /// </summary>
        private float _chunkSize;

        /// <summary>
        /// Grille de voxels
        /// </summary>
        private VoxelGrid _grid;

        /// <summary>
        /// Renderers pour les surfaces/murs
        /// </summary>
        private VoxelRenderer[][] _renderers;

        /// <summary>
        /// Faux voxel utilisé lors de la triangulation pour relier les chunks entre eux
        /// </summary>
        private Voxel[] _dummyXs, _dummyYs, _dummyTs;

        /// <summary>
        /// Celle factice pour faciliter le déplacement des valeurs
        /// </summary>
        private VoxelCell[] _cells;

        #endregion

        #region Méthodes Unity

        /// <summary>
        /// init
        /// </summary>
        private void Awake()
        {
            _grid = FindAnyObjectByType<VoxelGrid>();
            _grid.OnChunksCreated += Initialize;
            _grid.OnStencilApplied += Apply;
        }

        /// <summary>
        /// nettoyage
        /// </summary>
        private void OnDestroy()
        {
            _grid.OnChunksCreated -= Initialize;
            _grid.OnStencilApplied -= Apply;
        }

        #endregion

        #region Méthodes privées

        /// <summary>
        /// Crée les renderers pour les surfaces et murs
        /// </summary>
        /// <param name="e">Données de l'événement</param>
        private void Initialize(object _, VoxelChunkInitializedEventArgs e)
        {
            _voxelResolution = e.VoxelResolution;
            _chunkSize = e.ChunkSize;
            _cells = new VoxelCell[e.ChunkPositions.Length];
            _dummyXs = new Voxel[e.ChunkPositions.Length];
            _dummyYs = new Voxel[e.ChunkPositions.Length];
            _dummyTs = new Voxel[e.ChunkPositions.Length];
            _renderers = new VoxelRenderer[e.ChunkPositions.Length][];

            for (int i = 0; i < e.ChunkPositions.Length; ++i)
            {
                _cells[i] = new VoxelCell();
                _dummyXs[i] = new Voxel();
                _dummyYs[i] = new Voxel();
                _dummyTs[i] = new Voxel();
                _cells[i].sharpFeatureLimit = Mathf.Cos(e.MaxFeatureAngle * Mathf.Deg2Rad);
                _cells[i].parallelLimit = Mathf.Cos(e.MaxParallelAngle * Mathf.Deg2Rad);

                CreateRenderers(e.ChunkPositions[i], i);
                //Refresh(_grid.Chunks[i], i);
            }
        }

        /// <summary>
        /// Applique le stencil au mesh
        /// </summary>
        /// <param name="e">Données de l'événement</param>
        private void Apply(object _, VoxelChunkStencilAppliedEventArgs e)
        {
            VoxelChunk chunk = _grid.Chunks[e.ChunkIndex];
            SetCrossings(chunk, e.Stencil, e.ChunkIndex, e.XStart, e.XEnd, e.YStart, e.YEnd);
            Refresh(chunk, e.ChunkIndex);
        }

        /// <summary>
        /// Crée les renderers pour les surfaces et murs
        /// </summary>
        /// <param name="chunkPos">Position du chunk</param>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        private void CreateRenderers(Vector3 chunkPos, int chunkIndex)
        {
            // On crée un Renderer de plus que nécessaire
            // pour pouvoir utiliser directement l'état de voxel comme index.
            // Ca nous éviter de soustraire 1 à chaque fois.

            _renderers[chunkIndex] = new VoxelRenderer[Materials.Length + 1];

            for (int i = 0; i < Materials.Length; ++i)
            {
                VoxelChunkSurface surface = Instantiate(SurfacePrefab, chunkPos, Quaternion.identity, transform);
                surface.Initialize(_voxelResolution, Materials[i].surfaceMaterial);

                VoxelChunkWall wall = Instantiate(WallPrefab, chunkPos, Quaternion.identity, transform);
                wall.Initialize(_voxelResolution, Materials[i].wallMaterial);

                _renderers[chunkIndex][i + 1] = new VoxelRenderer(surface, wall);
            }
        }

        /// <summary>
        /// Calcule les intersections
        /// </summary>
        /// <param name="chunk">Le chunk affecté</param>
        /// <param name="stencil">La brosse</param>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        /// <param name="xStart">Limite de la zone rectangulaire affectée par la brosse</param>
        /// <param name="xEnd">Limite de la zone rectangulaire affectée par la brosse</param>
        /// <param name="yStart">Limite de la zone rectangulaire affectée par la brosse</param>
        /// <param name="yEnd">Limite de la zone rectangulaire affectée par la brosse</param>
        private void SetCrossings(VoxelChunk chunk, VoxelStencil stencil, int chunkIndex, int xStart, int xEnd, int yStart, int yEnd)
        {
            bool crossHorizontalGap = false;
            bool includeLastVerticalRow = false;
            bool crossVerticalGap = false;

            if (xStart > 0)
            {
                xStart -= 1;
            }
            if (xEnd == _voxelResolution - 1)
            {
                xEnd -= 1;
                crossHorizontalGap = chunk.XNeighbor != null;
            }
            if (yStart > 0)
            {
                yStart -= 1;
            }
            if (yEnd == _voxelResolution - 1)
            {
                yEnd -= 1;
                includeLastVerticalRow = true;
                crossVerticalGap = chunk.YNeighbor != null;
            }

            Voxel a, b;

            for (int y = yStart; y <= yEnd; y++)
            {
                int i = y * _voxelResolution + xStart;
                b = chunk.Voxels[i];

                for (int x = xStart; x <= xEnd; x++, i++)
                {
                    a = b;
                    b = chunk.Voxels[i + 1];
                    stencil.SetHorizontalCrossing(a, b);
                    stencil.SetVerticalCrossing(a, chunk.Voxels[i + _voxelResolution]);
                }

                stencil.SetVerticalCrossing(b, chunk.Voxels[i + _voxelResolution]);

                if (crossHorizontalGap)
                {
                    _dummyXs[chunkIndex].BecomeXDummyOf(chunk.XNeighbor.Voxels[y * _voxelResolution], _chunkSize);
                    stencil.SetHorizontalCrossing(b, _dummyXs[chunkIndex]);
                }
            }

            if (includeLastVerticalRow)
            {
                int i = chunk.Voxels.Length - _voxelResolution + xStart;
                b = chunk.Voxels[i];

                for (int x = xStart; x <= xEnd; x++, i++)
                {
                    a = b;
                    b = chunk.Voxels[i + 1];
                    stencil.SetHorizontalCrossing(a, b);

                    if (crossVerticalGap)
                    {
                        _dummyYs[chunkIndex].BecomeYDummyOf(chunk.YNeighbor.Voxels[x], _chunkSize);
                        stencil.SetVerticalCrossing(a, _dummyYs[chunkIndex]);
                    }
                }

                if (crossVerticalGap)
                {
                    _dummyYs[chunkIndex].BecomeYDummyOf(chunk.YNeighbor.Voxels[xEnd + 1], _chunkSize);
                    stencil.SetVerticalCrossing(b, _dummyYs[chunkIndex]);
                }
                if (crossHorizontalGap)
                {
                    _dummyXs[chunkIndex].BecomeXDummyOf(chunk.XNeighbor.Voxels[chunk.Voxels.Length - _voxelResolution], _chunkSize);
                    stencil.SetHorizontalCrossing(b, _dummyXs[chunkIndex]);
                }
            }
        }

        /// <summary>
        /// Màj le mesh
        /// </summary>
        /// <param name="chunk">Le chunk</param>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        private void Refresh(VoxelChunk chunk, int chunkIndex)
        {
            Triangulate(chunk, chunkIndex);
        }

        #region Triangulation

        /// <summary>
        /// Calcule les triangles du mesh
        /// </summary>
        /// <param name="chunk">Le chunk</param>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        private void Triangulate(VoxelChunk chunk, int chunkIndex)
        {
            for (int i = 1; i < _renderers[chunkIndex].Length; ++i)
            {
                _renderers[chunkIndex][i].Clear();
            }

            FillFirstRowCache(chunk, chunkIndex);
            TriangulateCellRows(chunk, chunkIndex);

            if (chunk.YNeighbor != null)
            {
                TriangulateGapRow(chunk, chunkIndex);
            }

            for (int i = 1; i < _renderers[chunkIndex].Length; ++i)
            {
                _renderers[chunkIndex][i].Apply();
            }
        }

        /// <summary>
        /// Remplit la 1è ligne de cache
        /// </summary>
        /// <param name="chunk">Le chunk</param>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        private void FillFirstRowCache(VoxelChunk chunk, int chunkIndex)
        {
            CacheFirstCorner(chunkIndex, chunk.Voxels[0]);
            int voxelIndex;

            for (voxelIndex = 0; voxelIndex < _voxelResolution - 1; ++voxelIndex)
            {
                CacheNextEdgeAndCorner(chunkIndex, voxelIndex, chunk.Voxels[voxelIndex], chunk.Voxels[voxelIndex + 1]);
            }

            if (chunk.XNeighbor != null)
            {
                _dummyXs[chunkIndex].BecomeXDummyOf(chunk.XNeighbor.Voxels[0], _chunkSize);
                CacheNextEdgeAndCorner(chunkIndex, voxelIndex, chunk.Voxels[voxelIndex], _dummyXs[chunkIndex]);
            }
        }

        /// <summary>
        /// Calcule les triangles de chaque rangée de cellules
        /// </summary>
        /// <param name="chunk">Le chunk</param>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        private void TriangulateCellRows(VoxelChunk chunk, int chunkIndex)
        {
            int cells = _voxelResolution - 1;

            for (int i = 0, y = 0; y < cells; ++y, ++i)
            {
                SwapRowCaches(chunkIndex);
                CacheFirstCorner(chunkIndex, chunk.Voxels[i + _voxelResolution]);
                CacheNextMiddleEdge(chunkIndex, chunk.Voxels[i], chunk.Voxels[i + _voxelResolution]);

                for (int x = 0; x < cells; ++x, ++i)
                {
                    Voxel
                     a = chunk.Voxels[i],
                     b = chunk.Voxels[i + 1],
                     c = chunk.Voxels[i + _voxelResolution],
                     d = chunk.Voxels[i + _voxelResolution + 1];
                    CacheNextEdgeAndCorner(chunkIndex, x, c, d);
                    CacheNextMiddleEdge(chunkIndex, b, d);
                    TriangulateCell(chunkIndex, x, a, b, c, d);
                }
                if (chunk.XNeighbor != null)
                {
                    TriangulateGapCell(chunk, chunkIndex, i);
                }
            }
        }

        /// <summary>
        /// Calcule les truangles d'une cellule séparant deux chunks
        /// </summary>
        /// <param name="chunk">Le chunk</param>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        /// <param name="i">La position du voxel dans la liste</param>
        private void TriangulateGapCell(VoxelChunk chunk, int chunkIndex, int i)
        {
            Voxel dummySwap = _dummyTs[chunkIndex];
            dummySwap.BecomeXDummyOf(chunk.XNeighbor.Voxels[i + 1], _chunkSize);
            _dummyTs[chunkIndex] = _dummyXs[chunkIndex];
            _dummyXs[chunkIndex] = dummySwap;
            int cacheIndex = _voxelResolution - 1;
            CacheNextEdgeAndCorner(chunkIndex, cacheIndex, chunk.Voxels[i + _voxelResolution], _dummyXs[chunkIndex]);
            CacheNextMiddleEdge(chunkIndex, _dummyTs[chunkIndex], _dummyXs[chunkIndex]);
            TriangulateCell(chunkIndex, cacheIndex, chunk.Voxels[i], _dummyTs[chunkIndex], chunk.Voxels[i + _voxelResolution], _dummyXs[chunkIndex]);
        }

        /// <summary>
        /// Calcule les truangles des cellules séparant deux chunks
        /// </summary>
        /// <param name="chunk">Le chunk</param>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        private void TriangulateGapRow(VoxelChunk chunk, int chunkIndex)
        {
            _dummyYs[chunkIndex].BecomeYDummyOf(chunk.YNeighbor.Voxels[0], _chunkSize);
            int cells = _voxelResolution - 1;
            int offset = cells * _voxelResolution;
            SwapRowCaches(chunkIndex);
            CacheFirstCorner(chunkIndex, _dummyYs[chunkIndex]);
            CacheNextMiddleEdge(chunkIndex, chunk.Voxels[cells * _voxelResolution], _dummyYs[chunkIndex]);

            for (int cellIndex = 0; cellIndex < cells; ++cellIndex)
            {
                Voxel dummySwap = _dummyTs[chunkIndex];
                dummySwap.BecomeYDummyOf(chunk.YNeighbor.Voxels[cellIndex + 1], _chunkSize);
                _dummyTs[chunkIndex] = _dummyYs[chunkIndex];
                _dummyYs[chunkIndex] = dummySwap;
                CacheNextEdgeAndCorner(chunkIndex, cellIndex, _dummyTs[chunkIndex], _dummyYs[chunkIndex]);
                CacheNextMiddleEdge(chunkIndex, chunk.Voxels[cellIndex + offset + 1], _dummyYs[chunkIndex]);
                TriangulateCell(chunkIndex, cellIndex, chunk.Voxels[cellIndex + offset], chunk.Voxels[cellIndex + offset + 1], _dummyTs[chunkIndex], _dummyYs[chunkIndex]);
            }

            if (chunk.XNeighbor != null)
            {
                _dummyTs[chunkIndex].BecomeXYDummyOf(chunk.XYNeighbor.Voxels[0], _chunkSize);
                CacheNextEdgeAndCorner(chunkIndex, cells, _dummyYs[chunkIndex], _dummyTs[chunkIndex]);
                CacheNextMiddleEdge(chunkIndex, _dummyXs[chunkIndex], _dummyTs[chunkIndex]);
                TriangulateCell(chunkIndex, cells, chunk.Voxels[^1], _dummyXs[chunkIndex], _dummyYs[chunkIndex], _dummyTs[chunkIndex]);
            }
        }

        /// <summary>
        /// Cache le 1er voxel (bas gauche)
        /// </summary>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        private void CacheFirstCorner(int chunkIndex, Voxel voxel)
        {
            if (voxel.Filled)
            {
                _renderers[chunkIndex][voxel.state].CacheFirstCorner(voxel);
            }
        }

        /// <summary>
        /// Cache l'edge et le voxel suivant
        /// </summary>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        /// <param name="cacheIndex">Position du voxel dans le cache</param>
        /// <param name="xMin">Voxel de gauche</param>
        /// <param name="xMax">Voxel de droite</param>
        private void CacheNextEdgeAndCorner(int chunkIndex, int cacheIndex, Voxel xMin, Voxel xMax)
        {
            if (xMin.state != xMax.state)
            {
                if (xMin.Filled)
                {
                    if (xMax.Filled)
                    {
                        _renderers[chunkIndex][xMin.state].CacheXEdge(cacheIndex, xMin);
                        _renderers[chunkIndex][xMax.state].CacheXEdge(cacheIndex, xMin);
                    }
                    else
                    {
                        _renderers[chunkIndex][xMin.state].CacheXEdgeWithWall(cacheIndex, xMin);
                    }
                }
                else
                {
                    _renderers[chunkIndex][xMax.state].CacheXEdgeWithWall(cacheIndex, xMin);
                }
            }
            if (xMax.Filled)
            {
                _renderers[chunkIndex][xMax.state].CacheNextCorner(cacheIndex, xMax);
            }
        }

        /// <summary>
        /// Cache l'edge du milieu
        /// </summary>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        /// <param name="yMin">Voxel du milieu gauche</param>
        /// <param name="yMax">Voxel du milieu droit</param>
        private void CacheNextMiddleEdge(int chunkIndex, Voxel yMin, Voxel yMax)
        {
            for (int i = 1; i < _renderers[chunkIndex].Length; ++i)
            {
                _renderers[chunkIndex][i].PrepareCacheForNextCell();
            }

            if (yMin.state != yMax.state)
            {
                if (yMin.Filled)
                {
                    if (yMax.Filled)
                    {
                        _renderers[chunkIndex][yMin.state].CacheYEdge(yMin);
                        _renderers[chunkIndex][yMax.state].CacheYEdge(yMin);
                    }
                    else
                    {
                        _renderers[chunkIndex][yMin.state].CacheYEdgeWithWall(yMin);
                    }
                }
                else
                {
                    _renderers[chunkIndex][yMax.state].CacheYEdgeWithWall(yMin);
                }
            }
        }

        /// <summary>
        /// Echange les lignes de cache
        /// </summary>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        private void SwapRowCaches(int chunkIndex)
        {
            for (int i = 1; i < _renderers[chunkIndex].Length; ++i)
            {
                _renderers[chunkIndex][i].PrepareCacheForNextRow();
            }
        }

        private void TriangulateCell(int chunkIndex, int cellIndex, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            _cells[chunkIndex].i = cellIndex;
            _cells[chunkIndex].a = a;
            _cells[chunkIndex].b = b;
            _cells[chunkIndex].c = c;
            _cells[chunkIndex].d = d;

            if (a.state == b.state)
            {
                if (a.state == c.state)
                {
                    if (a.state == d.state)
                    {
                        Triangulate0000(chunkIndex);
                    }
                    else
                    {
                        Triangulate0001(chunkIndex);
                    }
                }
                else
                {
                    if (a.state == d.state)
                    {
                        Triangulate0010(chunkIndex);
                    }
                    else if (c.state == d.state)
                    {
                        Triangulate0011(chunkIndex);
                    }
                    else
                    {
                        Triangulate0012(chunkIndex);
                    }
                }
            }
            else
            {
                if (a.state == c.state)
                {
                    if (a.state == d.state)
                    {
                        Triangulate0100(chunkIndex);
                    }
                    else if (b.state == d.state)
                    {
                        Triangulate0101(chunkIndex);
                    }
                    else
                    {
                        Triangulate0102(chunkIndex);
                    }
                }
                else if (b.state == c.state)
                {
                    if (a.state == d.state)
                    {
                        Triangulate0110(chunkIndex);
                    }
                    else if (b.state == d.state)
                    {
                        Triangulate0111(chunkIndex);
                    }
                    else
                    {
                        Triangulate0112(chunkIndex);
                    }
                }
                else
                {
                    if (a.state == d.state)
                    {
                        Triangulate0120(chunkIndex);
                    }
                    else if (b.state == d.state)
                    {
                        Triangulate0121(chunkIndex);
                    }
                    else if (c.state == d.state)
                    {
                        Triangulate0122(chunkIndex);
                    }
                    else
                    {
                        Triangulate0123(chunkIndex);
                    }
                }
            }
        }

        private void Triangulate0000(int chunkIndex)
        {
            FillABCD(chunkIndex);
        }

        private void Triangulate0001(int chunkIndex)
        {
            FeaturePoint f = _cells[chunkIndex].FeatureNE;
            FillABC(chunkIndex, f);
            FillD(chunkIndex, f);
        }

        private void Triangulate0010(int chunkIndex)
        {
            FeaturePoint f = _cells[chunkIndex].FeatureNW;
            FillABD(chunkIndex, f);
            FillC(chunkIndex, f);
        }

        private void Triangulate0100(int chunkIndex)
        {
            FeaturePoint f = _cells[chunkIndex].FeatureSE;
            FillACD(chunkIndex, f);
            FillB(chunkIndex, f);
        }

        private void Triangulate0111(int chunkIndex)
        {
            FeaturePoint f = _cells[chunkIndex].FeatureSW;
            FillA(chunkIndex, f);
            FillBCD(chunkIndex, f);
        }

        private void Triangulate0011(int chunkIndex)
        {
            FeaturePoint f = _cells[chunkIndex].FeatureEW;
            FillAB(chunkIndex, f);
            FillCD(chunkIndex, f);
        }

        private void Triangulate0101(int chunkIndex)
        {
            FeaturePoint f = _cells[chunkIndex].FeatureNS;
            FillAC(chunkIndex, f);
            FillBD(chunkIndex, f);
        }

        private void Triangulate0012(int chunkIndex)
        {
            FeaturePoint f = _cells[chunkIndex].FeatureNEW;
            FillAB(chunkIndex, f);
            FillC(chunkIndex, f);
            FillD(chunkIndex, f);
        }

        private void Triangulate0102(int chunkIndex)
        {
            FeaturePoint f = _cells[chunkIndex].FeatureNSE;
            FillAC(chunkIndex, f);
            FillB(chunkIndex, f);
            FillD(chunkIndex, f);
        }

        private void Triangulate0121(int chunkIndex)
        {
            FeaturePoint f = _cells[chunkIndex].FeatureNSW;
            FillA(chunkIndex, f);
            FillBD(chunkIndex, f);
            FillC(chunkIndex, f);
        }

        private void Triangulate0122(int chunkIndex)
        {
            FeaturePoint f = _cells[chunkIndex].FeatureSEW;
            FillA(chunkIndex, f);
            FillB(chunkIndex, f);
            FillCD(chunkIndex, f);
        }

        private void Triangulate0110(int chunkIndex)
        {
            FeaturePoint
                fA = _cells[chunkIndex].FeatureSW, fB = _cells[chunkIndex].FeatureSE,
                fC = _cells[chunkIndex].FeatureNW, fD = _cells[chunkIndex].FeatureNE;

            if (_cells[chunkIndex].HasConnectionAD(fA, fD))
            {
                fB.exists &= _cells[chunkIndex].IsInsideABD(fB.position);
                fC.exists &= _cells[chunkIndex].IsInsideACD(fC.position);
                FillADToB(chunkIndex, fB);
                FillADToC(chunkIndex, fC);
                FillB(chunkIndex, fB);
                FillC(chunkIndex, fC);
            }
            else if (_cells[chunkIndex].HasConnectionBC(fB, fC))
            {
                fA.exists &= _cells[chunkIndex].IsInsideABC(fA.position);
                fD.exists &= _cells[chunkIndex].IsInsideBCD(fD.position);
                FillA(chunkIndex, fA);
                FillD(chunkIndex, fD);
                FillBCToA(chunkIndex, fA);
                FillBCToD(chunkIndex, fD);
            }
            else if (_cells[chunkIndex].a.Filled && _cells[chunkIndex].b.Filled)
            {
                FillJoinedCorners(chunkIndex, fA, fB, fC, fD);
            }
            else
            {
                FillA(chunkIndex, fA);
                FillB(chunkIndex, fB);
                FillC(chunkIndex, fC);
                FillD(chunkIndex, fD);
            }
        }

        private void Triangulate0112(int chunkIndex)
        {
            FeaturePoint
                fA = _cells[chunkIndex].FeatureSW, fB = _cells[chunkIndex].FeatureSE,
                fC = _cells[chunkIndex].FeatureNW, fD = _cells[chunkIndex].FeatureNE;

            if (_cells[chunkIndex].HasConnectionBC(fB, fC))
            {
                fA.exists &= _cells[chunkIndex].IsInsideABC(fA.position);
                fD.exists &= _cells[chunkIndex].IsInsideBCD(fD.position);
                FillA(chunkIndex, fA);
                FillD(chunkIndex, fD);
                FillBCToA(chunkIndex, fA);
                FillBCToD(chunkIndex, fD);
            }
            else if (_cells[chunkIndex].b.Filled || _cells[chunkIndex].HasConnectionAD(fA, fD))
            {
                FillJoinedCorners(chunkIndex, fA, fB, fC, fD);
            }
            else
            {
                FillA(chunkIndex, fA);
                FillD(chunkIndex, fD);
            }
        }

        private void Triangulate0120(int chunkIndex)
        {
            FeaturePoint
                fA = _cells[chunkIndex].FeatureSW, fB = _cells[chunkIndex].FeatureSE,
                fC = _cells[chunkIndex].FeatureNW, fD = _cells[chunkIndex].FeatureNE;

            if (_cells[chunkIndex].HasConnectionAD(fA, fD))
            {
                fB.exists &= _cells[chunkIndex].IsInsideABD(fB.position);
                fC.exists &= _cells[chunkIndex].IsInsideACD(fC.position);
                FillADToB(chunkIndex, fB);
                FillADToC(chunkIndex, fC);
                FillB(chunkIndex, fB);
                FillC(chunkIndex, fC);
            }
            else if (_cells[chunkIndex].a.Filled || _cells[chunkIndex].HasConnectionBC(fB, fC))
            {
                FillJoinedCorners(chunkIndex, fA, fB, fC, fD);
            }
            else
            {
                FillB(chunkIndex, fB);
                FillC(chunkIndex, fC);
            }
        }

        private void Triangulate0123(int chunkIndex)
        {
            FillJoinedCorners(chunkIndex,
                _cells[chunkIndex].FeatureSW, _cells[chunkIndex].FeatureSE,
                _cells[chunkIndex].FeatureNW, _cells[chunkIndex].FeatureNE);
        }

        private void FillA(int chunkIndex, FeaturePoint f)
        {
            if (_cells[chunkIndex].a.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].a.state].FillA(_cells[chunkIndex], f);
            }
        }

        private void FillB(int chunkIndex, FeaturePoint f)
        {
            if (_cells[chunkIndex].b.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].b.state].FillB(_cells[chunkIndex], f);
            }
        }

        private void FillC(int chunkIndex, FeaturePoint f)
        {
            if (_cells[chunkIndex].c.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].c.state].FillC(_cells[chunkIndex], f);
            }
        }

        private void FillD(int chunkIndex, FeaturePoint f)
        {
            if (_cells[chunkIndex].d.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].d.state].FillD(_cells[chunkIndex], f);
            }
        }

        private void FillABC(int chunkIndex, FeaturePoint f)
        {
            if (_cells[chunkIndex].a.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].a.state].FillABC(_cells[chunkIndex], f);
            }
        }

        private void FillABD(int chunkIndex, FeaturePoint f)
        {
            if (_cells[chunkIndex].a.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].a.state].FillABD(_cells[chunkIndex], f);
            }
        }

        private void FillACD(int chunkIndex, FeaturePoint f)
        {
            if (_cells[chunkIndex].a.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].a.state].FillACD(_cells[chunkIndex], f);
            }
        }

        private void FillBCD(int chunkIndex, FeaturePoint f)
        {
            if (_cells[chunkIndex].b.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].b.state].FillBCD(_cells[chunkIndex], f);
            }
        }

        private void FillAB(int chunkIndex, FeaturePoint f)
        {
            if (_cells[chunkIndex].a.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].a.state].FillAB(_cells[chunkIndex], f);
            }
        }

        private void FillAC(int chunkIndex, FeaturePoint f)
        {
            if (_cells[chunkIndex].a.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].a.state].FillAC(_cells[chunkIndex], f);
            }
        }

        private void FillBD(int chunkIndex, FeaturePoint f)
        {
            if (_cells[chunkIndex].b.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].b.state].FillBD(_cells[chunkIndex], f);
            }
        }

        private void FillCD(int chunkIndex, FeaturePoint f)
        {
            if (_cells[chunkIndex].c.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].c.state].FillCD(_cells[chunkIndex], f);
            }
        }

        private void FillADToB(int chunkIndex, FeaturePoint f)
        {
            if (_cells[chunkIndex].a.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].a.state].FillADToB(_cells[chunkIndex], f);
            }
        }

        private void FillADToC(int chunkIndex, FeaturePoint f)
        {
            if (_cells[chunkIndex].a.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].a.state].FillADToC(_cells[chunkIndex], f);
            }
        }

        private void FillBCToA(int chunkIndex, FeaturePoint f)
        {
            if (_cells[chunkIndex].b.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].b.state].FillBCToA(_cells[chunkIndex], f);
            }
        }

        private void FillBCToD(int chunkIndex, FeaturePoint f)
        {
            if (_cells[chunkIndex].b.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].b.state].FillBCToD(_cells[chunkIndex], f);
            }
        }

        private void FillABCD(int chunkIndex)
        {
            if (_cells[chunkIndex].a.Filled)
            {
                _renderers[chunkIndex][_cells[chunkIndex].a.state].FillABCD(_cells[chunkIndex]);
            }
        }

        private void FillJoinedCorners(int chunkIndex, FeaturePoint fA, FeaturePoint fB, FeaturePoint fC, FeaturePoint fD)
        {
            FeaturePoint point = FeaturePoint.Average(fA, fB, fC, fD);

            if (!point.exists)
            {
                point.position = _cells[chunkIndex].AverageNESW;
                point.exists = true;
            }

            FillA(chunkIndex, point);
            FillB(chunkIndex, point);
            FillC(chunkIndex, point);
            FillD(chunkIndex, point);
        }

        #endregion

        #endregion
    }
}