using System;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Models.QuadTree
{
    /// <summary>
    /// Tableau de booléens indiquant la présence du sol
    /// à un pixel donné
    /// </summary>
    public readonly struct TerrainData
    {
        #region Propriétés

        /// <summary>
        /// Tableau de booléens indiquant la présence du sol
        /// à un pixel donné
        /// </summary>
        public readonly bool Empty => Points == null;

        /// <summary>
        /// Tableau de booléens indiquant la présence du sol
        /// à un pixel donné
        /// </summary>
        public readonly bool[] Points { get; }

        /// <summary>
        /// Largeur du tableau
        /// </summary>
        public readonly int Width { get; }

        /// <summary>
        /// Hauteur du tableau
        /// </summary>
        public readonly int Height { get; }

        #endregion

        #region Constructeur

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="texture2D">Source des points du terrain</param>
        public TerrainData(Texture2D texture2D)
        {
            Width = texture2D.width;
            Height = texture2D.height;
            Points = new bool[Width * Height];

            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    Points[x + y * Width] = texture2D.GetPixel(x, y).a > Single.Epsilon;
                }
            }
        }

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Indique si le quad est uniforme (càd que toutes ses valeurs contenues sont identiques)
        /// </summary>
        public bool IsQuadUniform(Quad quad)
        {
            bool isOriginSolid = Points[quad.Pos.x + quad.Pos.y * Width];

            for (int x = 0; x < quad.Dimensions.x; ++x)
            {
                for (int y = 0; y < quad.Dimensions.y; ++y)
                {
                    bool isOtherSolid = Points[(quad.Pos.x + x) + (quad.Pos.y + y) * Width];

                    if (isOriginSolid != isOtherSolid)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Indique si le quad entièrement solide
        /// </summary>
        public bool IsSolid(Quad quad)
        {
            for (int x = 0; x < quad.Dimensions.x; ++x)
            {
                for (int y = 0; y < quad.Dimensions.y; ++y)
                {
                    if (!Points[(quad.Pos.x + x) + (quad.Pos.y + y) * Width])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Détruit une section du terrain
        /// </summary>
        public void DestroyTerrain(int centerX, int centerY, int range)
        {
            int xOrigin = Mathf.Max(0, centerX - range);
            int xEnd = Mathf.Min(Width - 1, centerX + range);
            int yOrigin = Mathf.Max(0, centerY - range);
            int yEnd = Mathf.Min(Height - 1, centerY + range);

            for (int x = xOrigin; x < xEnd; ++x)
            {
                for (int y = yOrigin; y < yEnd; ++y)
                {
                    int xDelta = x - centerX;
                    int yDelta = y - centerY;
                    float sqrDistance = (xDelta * xDelta) + (yDelta * yDelta);

                    if (sqrDistance < range * range)
                    {
                        Points[x + y * Width] = false;
                    }
                }
            }
        }
        #endregion
    }
}