using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Assets.Project.Scripts.Runtime.Models.QuadTree
{
    /// <summary>
    /// Arbre quaternaire
    /// </summary>
    public readonly struct QuadTree : IEnumerable<Quad>
    {
        #region Propriétés

        /// <summary>
        /// Tableau de booléens indiquant la présence du sol
        /// à un pixel donné
        /// </summary>
        public readonly bool Empty => TerrainData.Empty;

        /// <summary>
        /// Les données du terrain
        /// </summary>
        public TerrainData TerrainData { get; }

        /// <summary>
        /// La racine de l'arbre
        /// </summary>
        public Quad Root { get; }

        #endregion

        #region Constructeur

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="terrainData">Les données du terrain</param>
        public QuadTree(TerrainData terrainData)
        {
            //UnityEngine.Debug.Log($"Texture width and height : ({terrainData.Width}, {terrainData.Height})");

            TerrainData = terrainData;
            Root = new Quad(int2.zero, new int2(terrainData.Width, terrainData.Height));
            Stack<Quad> quadsToCheck = new();
            quadsToCheck.Push(Root);

            while (quadsToCheck.TryPop(out Quad quad))
            {
                if (!terrainData.IsQuadUniform(quad))
                {
                    Quad[] children = quad.Subdivide();

                    foreach (Quad child in children)
                    {
                        if (child.IsDivisible)
                            quadsToCheck.Push(child);
                    }
                }
            }
        }

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Implémentation de l'énumérateur
        /// </summary>
        public IEnumerator<Quad> GetEnumerator()
        {
            return new QuadEnumerator(Root);
        }

        /// <summary>
        /// Implémentation de l'énumérateur
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}