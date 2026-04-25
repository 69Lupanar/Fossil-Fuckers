using Unity.Mathematics;

namespace Assets.Project.Scripts.Runtime.Models.QuadTree
{
    /// <summary>
    /// Element d'un arbre quaternaire
    /// </summary>
    public sealed class Quad
    {
        #region Propriétés

        /// <summary>
        /// true si l'objet peut être divisé
        /// </summary>
        public bool IsDivisible => Dimensions.x * Dimensions.y >= 2;

        /// <summary>
        /// Position du quad dans l'espace
        /// </summary>
        public int2 Pos { get; }

        /// <summary>
        /// Dimensions du quad
        /// </summary>
        public int2 Dimensions { get; }

        /// <summary>
        /// Dimensions du quad
        /// </summary>
        public Quad[] Children { get; private set; }

        /// <summary>
        /// true si ce quad possède des sous-éléments
        /// </summary>
        public bool HasChildren { get; private set; }

        #endregion

        #region Constructeur

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="pos">Position du quad dans l'espace</param>
        /// <param name="dimensions">Dimensions du quad</param>
        public Quad(int2 pos, int2 dimensions)
        {
            Pos = pos;
            Dimensions = dimensions;
            Children = null;
            HasChildren = false;
        }

        /// <summary>
        /// Constructeur
        /// </summary>
        public Quad(int x, int y, int width, int height) : this(new int2(x, y), new int2(width, height)) { }

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Divise le quad
        /// </summary>
        /// <returns>Les sous-quads de cet élément</returns>
        public Quad[] Subdivide()
        {
            if (Dimensions.x >= 2 * Dimensions.y)
            {
                DivideHorizontally();
            }
            else if (Dimensions.y >= 2 * Dimensions.x)
            {
                DivideVertically();
            }
            else
            {
                DivideEqually();
            }

            HasChildren = true;
            return Children;
        }

        /// <summary>
        /// Divise le quad verticalement
        /// </summary>
        private void DivideVertically()
        {
            if (Dimensions.y == 2)
            {
                Children = new[]
                {
                new Quad(Pos.x, Pos.y, Dimensions.x, 1),
                new Quad(Pos.x, Pos.y +1, Dimensions.x, 1),
            };
            }
            else
            {
                int heightOddFix = Dimensions.y % 4;
                int quarterHeight = Dimensions.y / 4;
                Children = new[]
                {
                    new Quad(Pos.x, Pos.y, Dimensions.x, quarterHeight),
                    new Quad(Pos.x, Pos.y+ quarterHeight, Dimensions.x, quarterHeight),
                    new Quad(Pos.x, Pos.y + (quarterHeight*2), Dimensions.x, quarterHeight),
                    new Quad(Pos.x, Pos.y  +(quarterHeight*3), Dimensions.x, quarterHeight + heightOddFix),
                };
            }
        }

        /// <summary>
        /// Divise le quad horizontalement
        /// </summary>
        private void DivideHorizontally()
        {
            if (Dimensions.x == 2)
            {
                Children = new[]
                {
                new Quad(Pos.x, Pos.y, 1, Dimensions.y),
                new Quad(Pos.x + 1, Pos.y, 1, Dimensions.y),
            };
            }
            else
            {
                int widthOddFix = Dimensions.x % 4;
                int quarterWidth = Dimensions.x / 4;
                Children = new[]
                {
                    new Quad(Pos.x, Pos.y, quarterWidth, Dimensions.y),
                    new Quad(Pos.x + quarterWidth, Pos.y, quarterWidth, Dimensions.y),
                    new Quad(Pos.x + (quarterWidth * 2), Pos.y, quarterWidth, Dimensions.y),
                    new Quad(Pos.x + (quarterWidth * 3) + widthOddFix, Pos.y, quarterWidth, Dimensions.y),
                };
            }
        }

        /// <summary>
        /// Divise le quad de manière égale
        /// </summary>
        private void DivideEqually()
        {
            int widthOddFix = Dimensions.x % 2;
            int heightOddFix = Dimensions.y % 2;
            int halfWidth = Dimensions.x / 2;
            int halfHeight = Dimensions.y / 2;
            int maxHeight = halfHeight + heightOddFix;
            int maxWidth = halfWidth + widthOddFix;
            Children = new[]
                {
                new Quad(Pos.x, Pos.y, halfWidth, halfHeight),
                new Quad(Pos.x + halfWidth, Pos.y, maxWidth, halfHeight),
                new Quad(Pos.x, Pos.y + halfHeight, halfWidth, maxHeight),
                new Quad(Pos.x + halfWidth, Pos.y + halfHeight, maxWidth, maxHeight )
            };
        }

        /// <summary>
        /// Indique si le quad a des sous-divisions
        /// </summary>
        /// <param name="children">Les sous-divisions de ce quad</param>
        public bool TryGetChildren(out Quad[] children)
        {
            if (HasChildren)
            {
                children = Children;
                return true;
            }

            children = null;
            return false;
        }

        #endregion
    }
}