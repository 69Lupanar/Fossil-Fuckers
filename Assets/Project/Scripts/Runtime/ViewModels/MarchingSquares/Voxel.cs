using System;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares
{
    /// <summary>
    /// Vartice d'une cellule du mesh généré par la triangulation
    /// </summary>
    [Serializable]
    public class Voxel
    {
        #region Variables d'instance

        /// <summary>
        /// Etat du voxel
        /// </summary>
        public bool state;

        /// <summary>
        /// Position
        /// </summary>
        public Vector2 position;

        /// <summary>
        /// Position du vertex de l'edge sur l'axe X
        /// </summary>
        public Vector2 xEdgePosition;

        /// <summary>
        /// Position du vertex de l'edge sur l'axe Y
        /// </summary>
        public Vector2 yEdgePosition;

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

            xEdgePosition = position;
            xEdgePosition.x += size * 0.5f;
            yEdgePosition = position;
            yEdgePosition.y += size * 0.5f;
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
            xEdgePosition = voxel.xEdgePosition;
            yEdgePosition = voxel.yEdgePosition;
            position.x += offset;
            xEdgePosition.x += offset;
            yEdgePosition.x += offset;
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
            xEdgePosition = voxel.xEdgePosition;
            yEdgePosition = voxel.yEdgePosition;
            position.y += offset;
            xEdgePosition.y += offset;
            yEdgePosition.y += offset;
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
            xEdgePosition = voxel.xEdgePosition;
            yEdgePosition = voxel.yEdgePosition;
            position.x += offset;
            position.y += offset;
            xEdgePosition.x += offset;
            xEdgePosition.y += offset;
            yEdgePosition.x += offset;
            yEdgePosition.y += offset;
        }

        #endregion
    }
}