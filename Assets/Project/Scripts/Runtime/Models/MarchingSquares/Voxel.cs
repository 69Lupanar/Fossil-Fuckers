using System;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.Models.MarchingSquares
{
    /// <summary>
    /// Vartice d'une cellule du mesh généré par la triangulation
    /// </summary>
    [Serializable]
    public class Voxel
    {
        #region Propriétés

        /// <summary>
        /// true si le voxel contient une valeur
        /// </summary>
        public bool Filled
        {
            get
            {
                return state > 0f;
            }
        }

        /// <summary>
        /// Point d'intersection sur l'edge X
        /// </summary>
        public Vector2 XEdgePoint
        {
            get
            {
                return new Vector2(xEdge, position.y);
            }
        }

        /// <summary>
        /// Point d'intersection sur l'edge Y
        /// </summary>
        public Vector2 YEdgePoint
        {
            get
            {
                return new Vector2(position.x, yEdge);
            }
        }

        #endregion

        #region Variables d'instance

        /// <summary>
        /// Etat du voxel
        /// </summary>
        public int state;

        /// <summary>
        /// Position
        /// </summary>
        public Vector2 position;

        /// <summary>
        /// Position du vertex de l'edge sur l'axe X
        /// </summary>
        public float xEdge;

        /// <summary>
        /// Position du vertex de l'edge sur l'axe Y
        /// </summary>
        public float yEdge;

        /// <summary>
        /// Normale du voxel
        /// </summary>
        public Vector2 xNormal, yNormal;

        #endregion

        #region Constructeur

        /// <summary>
        /// Constructeur
        /// </summary>
        public Voxel() { }

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="x">Coord X</param>
        /// <param name="y">Coord Y</param>
        /// <param name="size">Taille du voxel</param>
        public Voxel(int x, int y, float size)
        {
            position.x = (x + 0.5f) * size;
            position.y = (y + 0.5f) * size;

            xEdge = float.MinValue;
            yEdge = float.MinValue;
        }

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Convertit le voxel en voxel factice pour la triangulation
        /// </summary>
        /// <param name="voxel">Voxel à convertir</param>
        /// <param name="offset">Taille du chunk</param>
        public void BecomeXDummyOf(Voxel voxel, float offset)
        {
            state = voxel.state;
            position = voxel.position;
            position.x += offset;
            xEdge = voxel.xEdge + offset;
            yEdge = voxel.yEdge;
            yNormal = voxel.yNormal;
        }

        /// <summary>
        /// Convertit le voxel en voxel factice pour la triangulation
        /// </summary>
        /// <param name="voxel">Voxel à convertir</param>
        /// <param name="offset">Taille du chunk</param>
        public void BecomeYDummyOf(Voxel voxel, float offset)
        {
            state = voxel.state;
            position = voxel.position;
            position.y += offset;
            xEdge = voxel.xEdge;
            yEdge = voxel.yEdge + offset;
            xNormal = voxel.xNormal;
        }

        /// <summary>
        /// Convertit le voxel en voxel factice pour la triangulation
        /// </summary>
        /// <param name="voxel">Voxel à convertir</param>
        /// <param name="offset">Taille du chunk</param>
        public void BecomeXYDummyOf(Voxel voxel, float offset)
        {
            state = voxel.state;
            position = voxel.position;
            position.x += offset;
            position.y += offset;
            xEdge = voxel.xEdge + offset;
            yEdge = voxel.yEdge + offset;
        }

        #endregion
    }
}