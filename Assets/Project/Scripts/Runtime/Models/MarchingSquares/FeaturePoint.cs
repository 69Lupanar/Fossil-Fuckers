using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Models.MarchingSquares
{
    /// <summary>
    /// Feature
    /// </summary>
    public readonly struct FeaturePoint
    {
        #region Propriétés

        public static FeaturePoint Empty => new(Vector2.zero, false);

        /// <summary>
        /// Position
        /// </summary>
        public readonly Vector2 Position { get; }

        /// <summary>
        /// true si la feature existe
        /// </summary>
        public readonly bool Exists { get; }

        #endregion

        #region Constructeur

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="position">Position</param>
        /// <param name="exists">true si la feature existe</param>
        public FeaturePoint(Vector2 position, bool exists)
        {
            Position = position;
            Exists = exists;
        }

        #endregion

        #region Méthodes statiques publiques

        /// <summary>
        /// Ovtient la moyenne des positions des features renseignées
        /// </summary>
        public static FeaturePoint Average(FeaturePoint a, FeaturePoint b, FeaturePoint c)
        {
            Vector2 position = Vector2.zero;
            float features = 0f;

            if (a.Exists)
            {
                position += a.Position;
                features += 1f;
            }
            if (b.Exists)
            {
                position += b.Position;
                features += 1f;
            }
            if (c.Exists)
            {
                position += c.Position;
                features += 1f;
            }

            if (features > 0f)
                position /= features;

            return new FeaturePoint(position, features > 0f);
        }

        /// <summary>
        /// Ovtient la moyenne des positions des features renseignées
        /// </summary>
        public static FeaturePoint Average(FeaturePoint a, FeaturePoint b, FeaturePoint c, FeaturePoint d)
        {
            Vector2 position = Vector2.zero;
            float features = 0f;

            if (a.Exists)
            {
                position += a.Position;
                features += 1f;
            }
            if (b.Exists)
            {
                position += b.Position;
                features += 1f;
            }
            if (c.Exists)
            {
                position += c.Position;
                features += 1f;
            }
            if (d.Exists)
            {
                position += d.Position;
                features += 1f;
            }

            if (features > 0f)
                position /= features;

            return new FeaturePoint(position, features > 0f);
        }

        #endregion
    }
}