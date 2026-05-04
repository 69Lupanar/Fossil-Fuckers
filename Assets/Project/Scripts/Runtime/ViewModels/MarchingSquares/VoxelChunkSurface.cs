using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares
{
    /// <summary>
    /// Chargé de l'affichage du mesh de la surface d'un chunk
    /// </summary>
    public class VoxelChunkSurface : MonoBehaviour
    {
        #region Variables d'instance

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
        /// Cache des vertices
        /// </summary>
        private int[] cornersMax, cornersMin;

        /// <summary>
        /// Cache des vertices
        /// </summary>
        private int[] xEdgesMax, xEdgesMin;

        /// <summary>
        /// Cache des vertices
        /// </summary>
        private int yEdgeMin, yEdgeMax;

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// init
        /// </summary>
        /// <param name="resolution">Résolution des voxels pour ce chunk</param>
        public void Initialize(int resolution, Material material)
        {
            GetComponent<MeshRenderer>().material = material;
            GetComponent<MeshFilter>().mesh = mesh = new Mesh();
            mesh.name = "VoxelGridSurface Mesh";
            vertices = new List<Vector3>();
            triangles = new List<int>();
            cornersMax = new int[resolution + 1];
            cornersMin = new int[resolution + 1];
            xEdgesMax = new int[resolution];
            xEdgesMin = new int[resolution];
        }

        /// <summary>
        /// Efface le mesh
        /// </summary>
        public void Clear()
        {
            vertices.Clear();
            triangles.Clear();
            mesh.Clear();
        }

        /// <summary>
        /// Assigne au mesh ses nouveaux composants
        /// </summary>
        public void Apply()
        {
            // TAF : Passer à des tableaux fixes au lieu de listes
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
        }

        /// <summary>
        /// Crée un triangle à partir des vertices renseignés
        /// </summary>
        public void AddTriangle(int a, int b, int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        /// <summary>
        /// Crée un quad à partir des vertices renseignés
        /// </summary>
        public void AddQuad(int a, int b, int c, int d)
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
        public void AddPentagon(int a, int b, int c, int d, int e)
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


        public void AddQuadABCD(int i)
        {
            AddQuad(cornersMin[i], cornersMax[i], cornersMax[i + 1], cornersMin[i + 1]);
        }

        public void AddTriangleA(int i)
        {
            AddTriangle(cornersMin[i], yEdgeMin, xEdgesMax[i]);
        }

        public void AddTriangleB(int i)
        {
            AddTriangle(cornersMin[i + 1], xEdgesMax[i], yEdgeMax);
        }

        public void AddTriangleC(int i)
        {
            AddTriangle(cornersMax[i], xEdgesMin[i], yEdgeMin);
        }

        public void AddTriangleD(int i)
        {
            AddTriangle(cornersMax[i + 1], yEdgeMax, xEdgesMin[i]);
        }

        public void AddPentagonABC(int i)
        {
            AddPentagon(cornersMin[i], cornersMax[i], xEdgesMin[i], yEdgeMax, cornersMin[i + 1]);
        }

        public void AddPentagonABD(int i)
        {
            AddPentagon(cornersMin[i + 1], cornersMin[i], yEdgeMin, xEdgesMin[i], cornersMax[i + 1]);
        }

        public void AddPentagonACD(int i)
        {
            AddPentagon(cornersMax[i], cornersMax[i + 1], yEdgeMax, xEdgesMax[i], cornersMin[i]);
        }

        public void AddPentagonBCD(int i)
        {
            AddPentagon(cornersMax[i + 1], cornersMin[i + 1], xEdgesMax[i], yEdgeMin, cornersMax[i]);
        }

        public void AddPentagonAB(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, yEdgeMax, cornersMin[i + 1], cornersMin[i], yEdgeMin);
            vertices.Add(extraVertex);
        }

        public void AddPentagonAC(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, xEdgesMax[i], cornersMin[i], cornersMax[i], xEdgesMin[i]);
            vertices.Add(extraVertex);
        }

        public void AddPentagonBD(int i, Vector2 extraVertex)
        {
            AddPentagon(
                vertices.Count, xEdgesMin[i], cornersMax[i + 1], cornersMin[i + 1], xEdgesMax[i]);
            vertices.Add(extraVertex);
        }

        public void AddPentagonCD(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, yEdgeMin, cornersMax[i], cornersMax[i + 1], yEdgeMax);
            vertices.Add(extraVertex);
        }

        public void AddPentagonBCToA(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, yEdgeMin, cornersMax[i], cornersMin[i + 1], xEdgesMax[i]);
            vertices.Add(extraVertex);
        }

        public void AddPentagonBCToD(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, yEdgeMax, cornersMin[i + 1], cornersMax[i], xEdgesMin[i]);
            vertices.Add(extraVertex);
        }

        public void AddPentagonADToB(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, xEdgesMax[i], cornersMin[i], cornersMax[i + 1], yEdgeMax);
            vertices.Add(extraVertex);
        }

        public void AddPentagonADToC(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, xEdgesMin[i], cornersMax[i + 1], cornersMin[i], yEdgeMin);
            vertices.Add(extraVertex);
        }

        public void AddQuadAB(int i)
        {
            AddQuad(cornersMin[i], yEdgeMin, yEdgeMax, cornersMin[i + 1]);
        }

        public void AddQuadAC(int i)
        {
            AddQuad(cornersMin[i], cornersMax[i], xEdgesMin[i], xEdgesMax[i]);
        }

        public void AddQuadBD(int i)
        {
            AddQuad(xEdgesMax[i], xEdgesMin[i], cornersMax[i + 1], cornersMin[i + 1]);
        }

        public void AddQuadCD(int i)
        {
            AddQuad(yEdgeMin, cornersMax[i], cornersMax[i + 1], yEdgeMax);
        }

        public void AddQuadA(int i, Vector2 extraVertex)
        {
            AddQuad(vertices.Count, xEdgesMax[i], cornersMin[i], yEdgeMin);
            vertices.Add(extraVertex);
        }

        public void AddQuadB(int i, Vector2 extraVertex)
        {
            AddQuad(vertices.Count, yEdgeMax, cornersMin[i + 1], xEdgesMax[i]);
            vertices.Add(extraVertex);
        }

        public void AddQuadC(int i, Vector2 extraVertex)
        {
            AddQuad(vertices.Count, yEdgeMin, cornersMax[i], xEdgesMin[i]);
            vertices.Add(extraVertex);
        }

        public void AddQuadD(int i, Vector2 extraVertex)
        {
            AddQuad(vertices.Count, xEdgesMin[i], cornersMax[i + 1], yEdgeMax);
            vertices.Add(extraVertex);
        }

        public void AddQuadBCToA(int i)
        {
            AddQuad(yEdgeMin, cornersMax[i], cornersMin[i + 1], xEdgesMax[i]);
        }

        public void AddQuadBCToD(int i)
        {
            AddQuad(yEdgeMax, cornersMin[i + 1], cornersMax[i], xEdgesMin[i]);
        }

        public void AddQuadADToB(int i)
        {
            AddQuad(xEdgesMax[i], cornersMin[i], cornersMax[i + 1], yEdgeMax);
        }

        public void AddQuadADToC(int i)
        {
            AddQuad(xEdgesMin[i], cornersMax[i + 1], cornersMin[i], yEdgeMin);
        }

        public void AddHexagon(int a, int b, int c, int d, int e, int f)
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

        public void AddHexagonABC(int i, Vector2 extraVertex)
        {
            AddHexagon(
                vertices.Count, yEdgeMax, cornersMin[i + 1],
                cornersMin[i], cornersMax[i], xEdgesMin[i]);
            vertices.Add(extraVertex);
        }

        public void AddHexagonABD(int i, Vector2 extraVertex)
        {
            AddHexagon(
                vertices.Count, xEdgesMin[i], cornersMax[i + 1],
                cornersMin[i + 1], cornersMin[i], yEdgeMin);
            vertices.Add(extraVertex);
        }

        public void AddHexagonACD(int i, Vector2 extraVertex)
        {
            AddHexagon(
                vertices.Count, xEdgesMax[i], cornersMin[i],
                cornersMax[i], cornersMax[i + 1], yEdgeMax);
            vertices.Add(extraVertex);
        }

        public void AddHexagonBCD(int i, Vector2 extraVertex)
        {
            AddHexagon(
                vertices.Count, yEdgeMin, cornersMax[i],
                cornersMax[i + 1], cornersMin[i + 1], xEdgesMax[i]);
            vertices.Add(extraVertex);
        }

        /// <summary>
        /// Cache le 1er voxel (bas gauche)
        /// </summary>
        public void CacheFirstCorner(float2 voxelPosition)
        {
            cornersMax[0] = vertices.Count;
            vertices.Add(new Vector3(voxelPosition.x, voxelPosition.y, 0f));
        }

        /// <summary>
        /// Cache l'edge et le voxel suivant
        /// </summary>
        /// <param name="i">Position du voxel dans le cache</param>
        public void CacheNextCorner(int i, float2 voxelPosition)
        {
            cornersMax[i + 1] = vertices.Count;
            vertices.Add(new Vector3(voxelPosition.x, voxelPosition.y, 0f));
        }

        /// <summary>
        /// Met en cache le point sur l'edge X
        /// </summary>
        public void CacheXEdge(int i, float2 xEdgePoint)
        {
            xEdgesMin[i] = vertices.Count;
            vertices.Add(new Vector3(xEdgePoint.x, xEdgePoint.y, 0f));
        }

        /// <summary>
        /// Met en cache le point sur l'edge Y
        /// </summary>
        public void CacheYEdge(float2 yEdgePoint)
        {
            yEdgeMax = vertices.Count;
            vertices.Add(new Vector3(yEdgePoint.x, yEdgePoint.y, 0f));
        }

        /// <summary>
        /// Prépare le cache pour la cellule voisine
        /// </summary>
        public void PrepareCacheForNextCell()
        {
            yEdgeMin = yEdgeMax;
        }

        /// <summary>
        /// Echange les lignes de cache
        /// </summary>
        public void PrepareCacheForNextRow()
        {
            (cornersMax, cornersMin) = (cornersMin, cornersMax);
            (xEdgesMin, xEdgesMax) = (xEdgesMax, xEdgesMin);
        }

        #endregion
    }
}