using System.Collections.Generic;
using Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares;
using UnityEngine;

/// <summary>
/// Chargé de l'affichage du mesh des murs d'un chunk
/// </summary>
public class VoxelGridWall : MonoBehaviour
{
    #region Variables Unity

    /// <summary>
    /// Niveau d'élévation du mur
    /// </summary>
    [Tooltip("Niveau d'élévation du mur")]
    public float bottom = 0.1f, top = 0f;

    #endregion

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
    /// Normales du mesh
    /// </summary>
    private List<Vector3> normals;

    /// <summary>
    /// Triangles du mesh
    /// </summary>
    private List<int> triangles;

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
        normals = new List<Vector3>();
        triangles = new List<int>();
        xEdgesMax = new int[resolution];
        xEdgesMin = new int[resolution];
    }

    /// <summary>
    /// Efface le mesh
    /// </summary>
    public void Clear()
    {
        vertices.Clear();
        normals.Clear();
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
        mesh.normals = normals.ToArray();
        mesh.triangles = triangles.ToArray();
    }

    /// <summary>
    /// Met en cache le point sur l'edge X
    /// </summary>
    public void CacheXEdge(int i, Voxel voxel)
    {
        xEdgesMax[i] = vertices.Count;
        Vector3 v = voxel.XEdgePoint;
        v.z = bottom;
        vertices.Add(v);
        v.z = top;
        vertices.Add(v);
        Vector3 n = voxel.xNormal;
        normals.Add(n);
        normals.Add(n);
    }

    /// <summary>
    /// Met en cache le point sur l'edge Y
    /// </summary>
    public void CacheYEdge(Voxel voxel)
    {
        yEdgeMax = vertices.Count;
        Vector3 v = voxel.YEdgePoint;
        v.z = bottom;
        vertices.Add(v);
        v.z = top;
        vertices.Add(v);
        Vector3 n = voxel.yNormal;
        normals.Add(n);
        normals.Add(n);
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
        (xEdgesMax, xEdgesMin) = (xEdgesMin, xEdgesMax);
    }

    public void AddACAB(int i)
    {
        AddSection(yEdgeMin, xEdgesMin[i]);
    }

    public void AddACAB(int i, Vector2 extraVertex)
    {
        AddSection(yEdgeMin, xEdgesMin[i], extraVertex);
    }

    public void AddABAC(int i)
    {
        AddSection(xEdgesMin[i], yEdgeMin);
    }

    public void AddABAC(int i, Vector2 extraVertex)
    {
        AddSection(xEdgesMin[i], yEdgeMin, extraVertex);
    }

    public void AddABBD(int i)
    {
        AddSection(xEdgesMin[i], yEdgeMax);
    }

    public void AddABBD(int i, Vector2 extraVertex)
    {
        AddSection(xEdgesMin[i], yEdgeMax, extraVertex);
    }

    public void AddABCD(int i)
    {
        AddSection(xEdgesMin[i], xEdgesMax[i]);
    }

    public void AddABCD(int i, Vector2 extraVertex)
    {
        AddSection(xEdgesMin[i], xEdgesMax[i], extraVertex);
    }

    public void AddACBD(int i)
    {
        AddSection(yEdgeMin, yEdgeMax);
    }

    public void AddACBD(int i, Vector2 extraVertex)
    {
        AddSection(yEdgeMin, yEdgeMax, extraVertex);
    }

    public void AddACCD(int i)
    {
        AddSection(yEdgeMin, xEdgesMax[i]);
    }

    public void AddACCD(int i, Vector2 extraVertex)
    {
        AddSection(yEdgeMin, xEdgesMax[i], extraVertex);
    }

    public void AddBDAB(int i)
    {
        AddSection(yEdgeMax, xEdgesMin[i]);
    }

    public void AddBDAB(int i, Vector2 extraVertex)
    {
        AddSection(yEdgeMax, xEdgesMin[i], extraVertex);
    }

    public void AddBDAC(int i)
    {
        AddSection(yEdgeMax, yEdgeMin);
    }

    public void AddBDAC(int i, Vector2 extraVertex)
    {
        AddSection(yEdgeMax, yEdgeMin, extraVertex);
    }

    public void AddBDCD(int i)
    {
        AddSection(yEdgeMax, xEdgesMax[i]);
    }

    public void AddBDCD(int i, Vector2 extraVertex)
    {
        AddSection(yEdgeMax, xEdgesMax[i], extraVertex);
    }

    public void AddCDAB(int i)
    {
        AddSection(xEdgesMax[i], xEdgesMin[i]);
    }

    public void AddCDAB(int i, Vector2 extraVertex)
    {
        AddSection(xEdgesMax[i], xEdgesMin[i], extraVertex);
    }

    public void AddCDAC(int i)
    {
        AddSection(xEdgesMax[i], yEdgeMin);
    }

    public void AddCDAC(int i, Vector2 extraVertex)
    {
        AddSection(xEdgesMax[i], yEdgeMin, extraVertex);
    }

    public void AddCDBD(int i)
    {
        AddSection(xEdgesMax[i], yEdgeMax);
    }

    public void AddCDBD(int i, Vector2 extraVertex)
    {
        AddSection(xEdgesMax[i], yEdgeMax, extraVertex);
    }

    public void AddFromAB(int i, Vector2 extraVertex)
    {
        AddHalfSection(xEdgesMin[i], extraVertex);
    }

    public void AddToAB(int i, Vector2 extraVertex)
    {
        AddHalfSection(extraVertex, xEdgesMin[i]);
    }

    public void AddFromAC(int i, Vector2 extraVertex)
    {
        AddHalfSection(yEdgeMin, extraVertex);
    }

    public void AddToAC(int i, Vector2 extraVertex)
    {
        AddHalfSection(extraVertex, yEdgeMin);
    }

    public void AddFromBD(int i, Vector2 extraVertex)
    {
        AddHalfSection(yEdgeMax, extraVertex);
    }

    public void AddToBD(int i, Vector2 extraVertex)
    {
        AddHalfSection(extraVertex, yEdgeMax);
    }

    public void AddFromCD(int i, Vector2 extraVertex)
    {
        AddHalfSection(xEdgesMax[i], extraVertex);
    }

    public void AddToCD(int i, Vector2 extraVertex)
    {
        AddHalfSection(extraVertex, xEdgesMax[i]);
    }
    #endregion

    #region Méthodes privées

    /// <summary>
    /// Ajoute une section du mur
    /// </summary>
    private void AddSection(int a, int b)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(b + 1);
        triangles.Add(a);
        triangles.Add(b + 1);
        triangles.Add(a + 1);
    }

    /// <summary>
    /// Ajoute une section du mur
    /// </summary>
    private void AddSection(int a, int b, Vector3 extraPoint)
    {
        AddSection(a, AddPoint(extraPoint, a));
        AddSection(AddPoint(extraPoint, b), b);
    }

    private void AddHalfSection(int a, Vector3 extraPoint)
    {
        AddSection(a, AddPoint(extraPoint, a));
    }

    private void AddHalfSection(Vector3 extraPoint, int a)
    {
        AddSection(AddPoint(extraPoint, a), a);
    }

    private int AddPoint(Vector3 extraPoint, int normalIndex)
    {
        int p = vertices.Count;
        extraPoint.z = bottom;
        vertices.Add(extraPoint);
        extraPoint.z = top;
        vertices.Add(extraPoint);
        Vector3 n = normals[normalIndex];
        normals.Add(n);
        normals.Add(n);
        return p;
    }

    #endregion
}