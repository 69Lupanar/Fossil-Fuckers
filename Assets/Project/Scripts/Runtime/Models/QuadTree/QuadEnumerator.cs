using System.Collections;
using System.Collections.Generic;

namespace Assets.Project.Scripts.Runtime.Models.QuadTree
{
    /// <summary>
    /// Enumérateur de quads
    /// </summary>
    public class QuadEnumerator : IEnumerator<Quad>
    {
        #region Propriétés

        /// <summary>
        /// Element actuel de la recherche
        /// </summary>
        public Quad Current => _current;

        /// <summary>
        /// Element actuel de la recherche
        /// </summary>
        object IEnumerator.Current => Current;

        #endregion

        #region Variables d'instance

        /// <summary>
        /// Liste des quads à traverser
        /// </summary>
        private Stack<Quad> _quadsToIterate;

        /// <summary>
        /// 1er élément de la traversée
        /// </summary>
        private Quad _root;

        /// <summary>
        /// Element actuel de la recherche
        /// </summary>
        private Quad _current;

        #endregion

        #region Constructeur

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="root">1er élément de la traversée</param>
        public QuadEnumerator(Quad root)
        {
            _quadsToIterate = new Stack<Quad>();
            _root = root;
            _quadsToIterate.Push(_root);
        }

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Evalue l'élément suivant s'il existe
        /// </summary>
        public bool MoveNext()
        {
            if (_quadsToIterate.TryPop(out Quad quad))
            {
                _current = quad;

                if (quad.TryGetChildren(out Quad[] children))
                {
                    foreach (Quad child in children)
                    {
                        _quadsToIterate.Push(child);
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Réinitialise l'énumérateur
        /// </summary>
        public void Reset()
        {
            _quadsToIterate.Clear();
            _current = _root;

            if (_current.TryGetChildren(out Quad[] children))
            {
                foreach (Quad child in children)
                {
                    _quadsToIterate.Push(child);
                }
            }
        }

        /// <summary>
        /// Nettoyage
        /// </summary>
        public void Dispose()
        {
            _quadsToIterate.Clear();
            _quadsToIterate = null;
        }

        #endregion
    }
}