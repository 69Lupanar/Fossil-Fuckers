using System.Runtime.CompilerServices;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares;
using Assets.Project.Scripts.Runtime.Models.MarchingSquares.Stencils;
using Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Views.MarchingSquares
{
    /// <summary>
    /// Charger de trianguler les voxels de la grille
    /// </summary>
    [BurstCompile]
    public sealed class VoxelGridMeshRendererView : MonoBehaviour
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
        }

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Crée les renderers pour les surfaces et murs
        /// </summary>
        /// <param name="e">Données de l'événement</param>
        public void Initialize(Vector3[] chunkPositions)
        {
            _voxelResolution = _grid.VoxelResolution;
            _chunkSize = _grid.GridSize / _grid.ChunkResolution;
            _cells = new VoxelCell[chunkPositions.Length];
            _dummyXs = new Voxel[chunkPositions.Length];
            _dummyYs = new Voxel[chunkPositions.Length];
            _dummyTs = new Voxel[chunkPositions.Length];
            _renderers = new VoxelRenderer[chunkPositions.Length][];

            for (int i = 0; i < chunkPositions.Length; ++i)
            {
                _cells[i] = new VoxelCell(Mathf.Cos(_grid.MaxFeatureAngle * Mathf.Deg2Rad), Mathf.Cos(_grid.MaxParallelAngle * Mathf.Deg2Rad));
                _dummyXs[i] = new Voxel();
                _dummyYs[i] = new Voxel();
                _dummyTs[i] = new Voxel();

                // On crée un Renderer de plus que nécessaire
                // pour pouvoir utiliser directement l'état de voxel comme index.
                // Ca nous éviter de soustraire 1 à chaque fois.

                _renderers[i] = new VoxelRenderer[Materials.Length + 1];

                for (int j = 0; j < Materials.Length; ++j)
                {
                    VoxelChunkSurface surface = Instantiate(SurfacePrefab, chunkPositions[i], Quaternion.identity, transform);
                    surface.Initialize(_voxelResolution, Materials[j].surfaceMaterial);

                    VoxelChunkWall wall = Instantiate(WallPrefab, chunkPositions[i], Quaternion.identity, transform);
                    wall.Initialize(_voxelResolution, Materials[j].wallMaterial);

                    _renderers[i][j + 1] = new VoxelRenderer(surface, wall);
                }
            }
        }

        /// <summary>
        /// Màj les meshs de tous les chunks
        /// </summary>
        public void Fill()
        {
            for (int i = 0; i < _grid.Chunks.Length; ++i)
            {
                Refresh(_grid.Chunks[i], i);
            }
        }

        /// <summary>
        /// Calcule les intersections
        /// </summary>
        /// <param name="stencil">La brosse</param>
        /// <param name="chunk">Le chunk affecté</param>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        /// <param name="bounds">Limite de la zone rectangulaire affectée par la brosse</param>
        public void SetCrossings(VoxelStencil stencil, VoxelChunk chunk, int chunkIndex, int4 bounds)
        {
            int xStart = bounds.x;
            int xEnd = bounds.y;
            int yStart = bounds.z;
            int yEnd = bounds.w;
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

            for (int y = yStart; y <= yEnd; y++)
            {
                int i = y * _voxelResolution + xStart;
                ref Voxel b = ref chunk.Voxels[i];

                for (int x = xStart; x <= xEnd; x++, i++)
                {
                    ref Voxel a = ref b;
                    b = ref chunk.Voxels[i + 1];
                    stencil.SetHorizontalCrossing(ref a, in b);
                    stencil.SetVerticalCrossing(ref a, in chunk.Voxels[i + _voxelResolution]);
                }

                stencil.SetVerticalCrossing(ref b, in chunk.Voxels[i + _voxelResolution]);

                if (crossHorizontalGap)
                {
                    BecomeXDummyOf(ref _dummyXs[chunkIndex], in chunk.XNeighbor.Voxels[y * _voxelResolution], _chunkSize);
                    stencil.SetHorizontalCrossing(ref b, in _dummyXs[chunkIndex]);
                }
            }

            if (includeLastVerticalRow)
            {
                int i = chunk.Voxels.Length - _voxelResolution + xStart;
                ref Voxel b = ref chunk.Voxels[i];

                for (int x = xStart; x <= xEnd; x++, i++)
                {
                    ref Voxel a = ref b;
                    b = ref chunk.Voxels[i + 1];
                    stencil.SetHorizontalCrossing(ref a, in b);

                    if (crossVerticalGap)
                    {
                        BecomeYDummyOf(ref _dummyYs[chunkIndex], in chunk.YNeighbor.Voxels[x], _chunkSize);
                        stencil.SetVerticalCrossing(ref a, in _dummyYs[chunkIndex]);
                    }
                }

                if (crossVerticalGap)
                {
                    BecomeYDummyOf(ref _dummyYs[chunkIndex], in chunk.YNeighbor.Voxels[xEnd + 1], _chunkSize);
                    stencil.SetVerticalCrossing(ref b, in _dummyYs[chunkIndex]);
                }
                if (crossHorizontalGap)
                {
                    BecomeXDummyOf(ref _dummyXs[chunkIndex], in chunk.XNeighbor.Voxels[chunk.Voxels.Length - _voxelResolution], _chunkSize);
                    stencil.SetHorizontalCrossing(ref b, in _dummyXs[chunkIndex]);
                }
            }
        }

        /// <summary>
        /// Màj le mesh
        /// </summary>
        /// <param name="chunk">Le chunk</param>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        public void Refresh(VoxelChunk chunk, int chunkIndex)
        {
            Triangulate(chunk, chunkIndex);
        }

        #endregion

        #region Méthodes privées

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
            CacheFirstCorner(chunkIndex, in chunk.Voxels[0]);
            int voxelIndex;

            for (voxelIndex = 0; voxelIndex < _voxelResolution - 1; ++voxelIndex)
            {
                CacheNextEdgeAndCorner(chunkIndex, voxelIndex, in chunk.Voxels[voxelIndex], in chunk.Voxels[voxelIndex + 1]);
            }

            if (chunk.XNeighbor != null)
            {
                BecomeXDummyOf(ref _dummyXs[chunkIndex], in chunk.XNeighbor.Voxels[0], _chunkSize);
                CacheNextEdgeAndCorner(chunkIndex, voxelIndex, in chunk.Voxels[voxelIndex], in _dummyXs[chunkIndex]);
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
                CacheFirstCorner(chunkIndex, in chunk.Voxels[i + _voxelResolution]);
                CacheNextMiddleEdge(chunkIndex, in chunk.Voxels[i], in chunk.Voxels[i + _voxelResolution]);

                for (int x = 0; x < cells; ++x, ++i)
                {
                    Voxel
                     a = chunk.Voxels[i],
                     b = chunk.Voxels[i + 1],
                     c = chunk.Voxels[i + _voxelResolution],
                     d = chunk.Voxels[i + _voxelResolution + 1];
                    CacheNextEdgeAndCorner(chunkIndex, x, in c, in d);
                    CacheNextMiddleEdge(chunkIndex, in b, in d);
                    TriangulateCell(chunkIndex, x, in a, in b, in c, in d);
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
            BecomeXDummyOf(ref dummySwap, in chunk.XNeighbor.Voxels[i + 1], _chunkSize);
            _dummyTs[chunkIndex] = _dummyXs[chunkIndex];
            _dummyXs[chunkIndex] = dummySwap;
            int cacheIndex = _voxelResolution - 1;
            CacheNextEdgeAndCorner(chunkIndex, cacheIndex, in chunk.Voxels[i + _voxelResolution], in _dummyXs[chunkIndex]);
            CacheNextMiddleEdge(chunkIndex, in _dummyTs[chunkIndex], in _dummyXs[chunkIndex]);
            TriangulateCell(chunkIndex, cacheIndex, in chunk.Voxels[i], in _dummyTs[chunkIndex], in chunk.Voxels[i + _voxelResolution], in _dummyXs[chunkIndex]);
        }

        /// <summary>
        /// Calcule les truangles des cellules séparant deux chunks
        /// </summary>
        /// <param name="chunk">Le chunk</param>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        private void TriangulateGapRow(VoxelChunk chunk, int chunkIndex)
        {
            BecomeYDummyOf(ref _dummyYs[chunkIndex], in chunk.YNeighbor.Voxels[0], _chunkSize);
            int cells = _voxelResolution - 1;
            int offset = cells * _voxelResolution;
            SwapRowCaches(chunkIndex);
            CacheFirstCorner(chunkIndex, in _dummyYs[chunkIndex]);
            CacheNextMiddleEdge(chunkIndex, in chunk.Voxels[cells * _voxelResolution], in _dummyYs[chunkIndex]);

            for (int cellIndex = 0; cellIndex < cells; ++cellIndex)
            {
                Voxel dummySwap = _dummyTs[chunkIndex];
                BecomeYDummyOf(ref dummySwap, in chunk.YNeighbor.Voxels[cellIndex + 1], _chunkSize);
                _dummyTs[chunkIndex] = _dummyYs[chunkIndex];
                _dummyYs[chunkIndex] = dummySwap;
                CacheNextEdgeAndCorner(chunkIndex, cellIndex, in _dummyTs[chunkIndex], in _dummyYs[chunkIndex]);
                CacheNextMiddleEdge(chunkIndex, in chunk.Voxels[cellIndex + offset + 1], in _dummyYs[chunkIndex]);
                TriangulateCell(chunkIndex, cellIndex, in chunk.Voxels[cellIndex + offset], in chunk.Voxels[cellIndex + offset + 1], in _dummyTs[chunkIndex], in _dummyYs[chunkIndex]);
            }

            if (chunk.XNeighbor != null)
            {
                BecomeXYDummyOf(ref _dummyTs[chunkIndex], in chunk.XYNeighbor.Voxels[0], _chunkSize);
                CacheNextEdgeAndCorner(chunkIndex, cells, in _dummyYs[chunkIndex], in _dummyTs[chunkIndex]);
                CacheNextMiddleEdge(chunkIndex, in _dummyXs[chunkIndex], in _dummyTs[chunkIndex]);
                TriangulateCell(chunkIndex, cells, in chunk.Voxels[^1], in _dummyXs[chunkIndex], in _dummyYs[chunkIndex], in _dummyTs[chunkIndex]);
            }
        }

        /// <summary>
        /// Cache le 1er voxel (bas gauche)
        /// </summary>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        [BurstCompile]
        private void CacheFirstCorner(int chunkIndex, in Voxel voxel)
        {
            if (voxel.Filled)
            {
                _renderers[chunkIndex][voxel.State].CacheFirstCorner(voxel.Position);
            }
        }

        /// <summary>
        /// Cache l'edge et le voxel suivant
        /// </summary>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        /// <param name="cacheIndex">Position du voxel dans le cache</param>
        /// <param name="xMin">Voxel de gauche</param>
        /// <param name="xMax">Voxel de droite</param>
        [BurstCompile]
        private void CacheNextEdgeAndCorner(int chunkIndex, int cacheIndex, in Voxel xMin, in Voxel xMax)
        {
            VoxelRenderer[] chunkRenderers = _renderers[chunkIndex];

            if (xMin.State != xMax.State)
            {
                if (xMin.Filled)
                {
                    if (xMax.Filled)
                    {
                        chunkRenderers[xMin.State].CacheXEdge(cacheIndex, xMin.XEdgePoint);
                        chunkRenderers[xMax.State].CacheXEdge(cacheIndex, xMin.XEdgePoint);
                    }
                    else
                    {
                        chunkRenderers[xMin.State].CacheXEdgeWithWall(cacheIndex, xMin.XEdgePoint, xMin.XNormal);
                    }
                }
                else
                {
                    chunkRenderers[xMax.State].CacheXEdgeWithWall(cacheIndex, xMin.XEdgePoint, xMin.XNormal);
                }
            }
            if (xMax.Filled)
            {
                chunkRenderers[xMax.State].CacheNextCorner(cacheIndex, xMax.Position);
            }
        }

        /// <summary>
        /// Cache l'edge du milieu
        /// </summary>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        /// <param name="yMin">Voxel du milieu gauche</param>
        /// <param name="yMax">Voxel du milieu droit</param>
        [BurstCompile]
        private void CacheNextMiddleEdge(int chunkIndex, in Voxel yMin, in Voxel yMax)
        {
            VoxelRenderer[] chunkRenderers = _renderers[chunkIndex];

            for (int i = 1; i < chunkRenderers.Length; ++i)
            {
                chunkRenderers[i].PrepareCacheForNextCell();
            }

            if (yMin.State != yMax.State)
            {
                if (yMin.Filled)
                {
                    if (yMax.Filled)
                    {
                        chunkRenderers[yMin.State].CacheYEdge(yMin.YEdgePoint);
                        chunkRenderers[yMax.State].CacheYEdge(yMin.YEdgePoint);
                    }
                    else
                    {
                        chunkRenderers[yMin.State].CacheYEdgeWithWall(yMin.YEdgePoint, yMin.YNormal);
                    }
                }
                else
                {
                    chunkRenderers[yMax.State].CacheYEdgeWithWall(yMin.YEdgePoint, yMin.YNormal);
                }
            }
        }

        /// <summary>
        /// Echange les lignes de cache
        /// </summary>
        /// <param name="chunkIndex">Index du chunk dans la grille</param>
        [BurstCompile]
        private void SwapRowCaches(int chunkIndex)
        {
            for (int i = 1; i < _renderers[chunkIndex].Length; ++i)
            {
                _renderers[chunkIndex][i].PrepareCacheForNextRow();
            }
        }

        /// <summary>
        /// Convertit le voxel en voxel factice pour la triangulation
        /// </summary>
        /// <param name="dummy">Voxel à convertir</param>
        /// <param name="other">Voxel à cloner</param>
        /// <param name="offset">Taille du chunk</param>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BecomeXDummyOf(ref Voxel dummy, in Voxel other, float offset)
        {
            dummy = new Voxel(other.State,
                              new float2(other.Position.x + offset, other.Position.y),
                              other.XEdge + offset,
                              other.YEdge,
                              dummy.XNormal,
                              other.YNormal);
        }

        /// <summary>
        /// Convertit le voxel en voxel factice pour la triangulation
        /// </summary>
        /// <param name="dummy">Voxel à convertir</param>
        /// <param name="other">Voxel à cloner</param>
        /// <param name="offset">Taille du chunk</param>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BecomeYDummyOf(ref Voxel dummy, in Voxel other, float offset)
        {
            dummy = new Voxel(other.State,
                              new float2(other.Position.x, other.Position.y + offset),
                              other.XEdge,
                              other.YEdge + offset,
                              other.XNormal,
                              dummy.YNormal);
        }

        /// <summary>
        /// Convertit le voxel en voxel factice pour la triangulation
        /// </summary>
        /// <param name="dummy">Voxel à convertir</param>
        /// <param name="other">Voxel à cloner</param>
        /// <param name="offset">Taille du chunk</param>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BecomeXYDummyOf(ref Voxel dummy, in Voxel other, float offset)
        {
            dummy = new Voxel(other.State,
                              new float2(other.Position.x + offset, other.Position.y + offset),
                              other.XEdge + offset,
                              other.YEdge + offset,
                              dummy.XNormal,
                              dummy.YNormal);
        }

        [BurstCompile]
        private void TriangulateCell(int chunkIndex, int cellIndex, in Voxel a, in Voxel b, in Voxel c, in Voxel d)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            cell.SetData(cellIndex, in a, in b, in c, in d);

            if (a.State == b.State)
            {
                if (a.State == c.State)
                {
                    if (a.State == d.State)
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
                    if (a.State == d.State)
                    {
                        Triangulate0010(chunkIndex);
                    }
                    else if (c.State == d.State)
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
                if (a.State == c.State)
                {
                    if (a.State == d.State)
                    {
                        Triangulate0100(chunkIndex);
                    }
                    else if (b.State == d.State)
                    {
                        Triangulate0101(chunkIndex);
                    }
                    else
                    {
                        Triangulate0102(chunkIndex);
                    }
                }
                else if (b.State == c.State)
                {
                    if (a.State == d.State)
                    {
                        Triangulate0110(chunkIndex);
                    }
                    else if (b.State == d.State)
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
                    if (a.State == d.State)
                    {
                        Triangulate0120(chunkIndex);
                    }
                    else if (b.State == d.State)
                    {
                        Triangulate0121(chunkIndex);
                    }
                    else if (c.State == d.State)
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

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Triangulate0000(int chunkIndex)
        {
            FillABCD(chunkIndex);
        }

        [BurstCompile]
        private void Triangulate0001(int chunkIndex)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            FillABC(chunkIndex, cell.FeatureNE);
            FillD(chunkIndex, cell.FeatureNE);
        }

        [BurstCompile]
        private void Triangulate0010(int chunkIndex)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            FillABD(chunkIndex, cell.FeatureNW);
            FillC(chunkIndex, cell.FeatureNW);
        }

        [BurstCompile]
        private void Triangulate0100(int chunkIndex)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            FillACD(chunkIndex, cell.FeatureSE);
            FillB(chunkIndex, cell.FeatureSE);
        }

        [BurstCompile]
        private void Triangulate0111(int chunkIndex)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            FillA(chunkIndex, cell.FeatureSW);
            FillBCD(chunkIndex, cell.FeatureSW);
        }

        [BurstCompile]
        private void Triangulate0011(int chunkIndex)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            FillAB(chunkIndex, cell.FeatureEW);
            FillCD(chunkIndex, cell.FeatureEW);
        }

        [BurstCompile]
        private void Triangulate0101(int chunkIndex)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            FillAC(chunkIndex, cell.FeatureNS);
            FillBD(chunkIndex, cell.FeatureNS);
        }

        [BurstCompile]
        private void Triangulate0012(int chunkIndex)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            FillAB(chunkIndex, cell.FeatureNEW);
            FillC(chunkIndex, cell.FeatureNEW);
            FillD(chunkIndex, cell.FeatureNEW);
        }

        [BurstCompile]
        private void Triangulate0102(int chunkIndex)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            FillAC(chunkIndex, cell.FeatureNSE);
            FillB(chunkIndex, cell.FeatureNSE);
            FillD(chunkIndex, cell.FeatureNSE);
        }

        [BurstCompile]
        private void Triangulate0121(int chunkIndex)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            FillA(chunkIndex, cell.FeatureNSW);
            FillBD(chunkIndex, cell.FeatureNSW);
            FillC(chunkIndex, cell.FeatureNSW);
        }

        [BurstCompile]
        private void Triangulate0122(int chunkIndex)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            FillA(chunkIndex, cell.FeatureSEW);
            FillB(chunkIndex, cell.FeatureSEW);
            FillCD(chunkIndex, cell.FeatureSEW);
        }

        [BurstCompile]
        private void Triangulate0110(int chunkIndex)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            FeaturePoint
                fA = cell.FeatureSW, fB = cell.FeatureSE,
                fC = cell.FeatureNW, fD = cell.FeatureNE;

            if (cell.HasConnectionAD(in fA, in fD))
            {
                bool fBExists = fB.Exists;
                bool fCExists = fC.Exists;
                fBExists &= cell.IsInsideABD(fB.Position);
                fCExists &= cell.IsInsideACD(fC.Position);
                fB = new FeaturePoint(fB.Position, fBExists);
                fC = new FeaturePoint(fC.Position, fCExists);

                FillADToB(chunkIndex, in fB);
                FillADToC(chunkIndex, in fC);
                FillB(chunkIndex, in fB);
                FillC(chunkIndex, in fC);
            }
            else if (cell.HasConnectionBC(in fB, in fC))
            {
                bool fAExists = fA.Exists;
                bool fDExists = fD.Exists;
                fAExists &= cell.IsInsideABC(fA.Position);
                fDExists &= cell.IsInsideBCD(fD.Position);
                fA = new FeaturePoint(fA.Position, fAExists);
                fD = new FeaturePoint(fD.Position, fDExists);

                FillA(chunkIndex, in fA);
                FillD(chunkIndex, in fD);
                FillBCToA(chunkIndex, in fA);
                FillBCToD(chunkIndex, in fD);
            }
            else if (cell.A.Filled && cell.B.Filled)
            {
                FillJoinedCorners(chunkIndex, in fA, in fB, in fC, in fD);
            }
            else
            {
                FillA(chunkIndex, in fA);
                FillB(chunkIndex, in fB);
                FillC(chunkIndex, in fC);
                FillD(chunkIndex, in fD);
            }
        }

        [BurstCompile]
        private void Triangulate0112(int chunkIndex)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            FeaturePoint
                fA = cell.FeatureSW, fB = cell.FeatureSE,
                fC = cell.FeatureNW, fD = cell.FeatureNE;

            if (cell.HasConnectionBC(in fB, in fC))
            {
                bool fAExists = fA.Exists;
                bool fDExists = fD.Exists;
                fAExists &= cell.IsInsideABC(fA.Position);
                fDExists &= cell.IsInsideBCD(fD.Position);
                fA = new FeaturePoint(fA.Position, fAExists);
                fD = new FeaturePoint(fD.Position, fDExists);

                FillA(chunkIndex, in fA);
                FillD(chunkIndex, in fD);
                FillBCToA(chunkIndex, in fA);
                FillBCToD(chunkIndex, in fD);
            }
            else if (cell.B.Filled || cell.HasConnectionAD(in fA, in fD))
            {
                FillJoinedCorners(chunkIndex, in fA, in fB, in fC, in fD);
            }
            else
            {
                FillA(chunkIndex, in fA);
                FillD(chunkIndex, in fD);
            }
        }

        [BurstCompile]
        private void Triangulate0120(int chunkIndex)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            FeaturePoint
                fA = cell.FeatureSW, fB = cell.FeatureSE,
                fC = cell.FeatureNW, fD = cell.FeatureNE;

            if (cell.HasConnectionAD(in fA, in fD))
            {
                bool fBExists = fB.Exists;
                bool fCExists = fC.Exists;
                fBExists &= cell.IsInsideABD(fB.Position);
                fCExists &= cell.IsInsideACD(fC.Position);
                fB = new FeaturePoint(fB.Position, fBExists);
                fC = new FeaturePoint(fC.Position, fCExists);

                FillADToB(chunkIndex, in fB);
                FillADToC(chunkIndex, in fC);
                FillB(chunkIndex, in fB);
                FillC(chunkIndex, in fC);
            }
            else if (cell.A.Filled || cell.HasConnectionBC(in fB, in fC))
            {
                FillJoinedCorners(chunkIndex, in fA, in fB, in fC, in fD);
            }
            else
            {
                FillB(chunkIndex, in fB);
                FillC(chunkIndex, in fC);
            }
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Triangulate0123(int chunkIndex)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            FillJoinedCorners(chunkIndex, cell.FeatureSW, cell.FeatureSE, cell.FeatureNW, cell.FeatureNE);
        }

        [BurstCompile]
        private void FillA(int chunkIndex, in FeaturePoint f)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.A.Filled)
            {
                _renderers[chunkIndex][cell.A.State].FillA(in cell, in f);
            }
        }

        [BurstCompile]
        private void FillB(int chunkIndex, in FeaturePoint f)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.B.Filled)
            {
                _renderers[chunkIndex][cell.B.State].FillB(in cell, in f);
            }
        }

        [BurstCompile]
        private void FillC(int chunkIndex, in FeaturePoint f)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.C.Filled)
            {
                _renderers[chunkIndex][cell.C.State].FillC(in cell, in f);
            }
        }

        [BurstCompile]
        private void FillD(int chunkIndex, in FeaturePoint f)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.D.Filled)
            {
                _renderers[chunkIndex][cell.D.State].FillD(in cell, in f);
            }
        }

        [BurstCompile]
        private void FillABC(int chunkIndex, in FeaturePoint f)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.A.Filled)
            {
                _renderers[chunkIndex][cell.A.State].FillABC(in cell, in f);
            }
        }

        [BurstCompile]
        private void FillABD(int chunkIndex, in FeaturePoint f)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.A.Filled)
            {
                _renderers[chunkIndex][cell.A.State].FillABD(in cell, in f);
            }
        }

        [BurstCompile]
        private void FillACD(int chunkIndex, in FeaturePoint f)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.A.Filled)
            {
                _renderers[chunkIndex][cell.A.State].FillACD(in cell, in f);
            }
        }

        [BurstCompile]
        private void FillBCD(int chunkIndex, in FeaturePoint f)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.B.Filled)
            {
                _renderers[chunkIndex][cell.B.State].FillBCD(in cell, in f);
            }
        }

        [BurstCompile]
        private void FillAB(int chunkIndex, in FeaturePoint f)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.A.Filled)
            {
                _renderers[chunkIndex][cell.A.State].FillAB(in cell, in f);
            }
        }

        [BurstCompile]
        private void FillAC(int chunkIndex, in FeaturePoint f)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.A.Filled)
            {
                _renderers[chunkIndex][cell.A.State].FillAC(in cell, in f);
            }
        }

        [BurstCompile]
        private void FillBD(int chunkIndex, in FeaturePoint f)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.B.Filled)
            {
                _renderers[chunkIndex][cell.B.State].FillBD(in cell, in f);
            }
        }

        [BurstCompile]
        private void FillCD(int chunkIndex, in FeaturePoint f)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.C.Filled)
            {
                _renderers[chunkIndex][cell.C.State].FillCD(in cell, in f);
            }
        }

        [BurstCompile]
        private void FillADToB(int chunkIndex, in FeaturePoint f)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.A.Filled)
            {
                _renderers[chunkIndex][cell.A.State].FillADToB(in cell, in f);
            }
        }

        [BurstCompile]
        private void FillADToC(int chunkIndex, in FeaturePoint f)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.A.Filled)
            {
                _renderers[chunkIndex][cell.A.State].FillADToC(in cell, in f);
            }
        }

        [BurstCompile]
        private void FillBCToA(int chunkIndex, in FeaturePoint f)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.B.Filled)
            {
                _renderers[chunkIndex][cell.B.State].FillBCToA(in cell, in f);
            }
        }

        [BurstCompile]
        private void FillBCToD(int chunkIndex, in FeaturePoint f)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.B.Filled)
            {
                _renderers[chunkIndex][cell.B.State].FillBCToD(in cell, in f);
            }
        }

        [BurstCompile]
        private void FillABCD(int chunkIndex)
        {
            ref VoxelCell cell = ref _cells[chunkIndex];
            if (cell.A.Filled)
            {
                _renderers[chunkIndex][cell.A.State].FillABCD(cell.I);
            }
        }

        [BurstCompile]
        private void FillJoinedCorners(int chunkIndex, in FeaturePoint fA, in FeaturePoint fB, in FeaturePoint fC, in FeaturePoint fD)
        {
            FeaturePoint.Average(in fA, in fB, in fC, in fD, out FeaturePoint point);

            if (!point.Exists)
            {
                point = new FeaturePoint(_cells[chunkIndex].AverageNESW, true);
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