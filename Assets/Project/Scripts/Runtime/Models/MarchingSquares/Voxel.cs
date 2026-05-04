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
                return State > 0f;
            }
        }

        /// <summary>
        /// Point d'intersection sur l'edge X
        /// </summary>
        public Vector2 XEdgePoint
        {
            get
            {
                return new Vector2(XEdge, Position.y);
            }
        }

        /// <summary>
        /// Point d'intersection sur l'edge Y
        /// </summary>
        public Vector2 YEdgePoint
        {
            get
            {
                return new Vector2(Position.x, YEdge);
            }
        }

        /// <summary>
        /// Etat du voxel
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// Position
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// Position du vertex de l'edge sur l'axe X
        /// </summary>
        public float XEdge { get; set; }

        /// <summary>
        /// Position du vertex de l'edge sur l'axe Y
        /// </summary>
        public float YEdge { get; set; }

        /// <summary>
        /// Normale du voxel
        /// </summary>
        public Vector2 XNormal { get; set; }

        /// <summary>
        /// Normale du voxel
        /// </summary>
        public Vector2 YNormal { get; set; }

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
            Position = new Vector2((x + 0.5f) * size, (y + 0.5f) * size);
            XEdge = float.MinValue;
            YEdge = float.MinValue;
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
            State = voxel.State;
            Position = voxel.Position;
            Position = new Vector2(Position.x + offset, Position.y);
            XEdge = voxel.XEdge + offset;
            YEdge = voxel.YEdge;
            YNormal = voxel.YNormal;
        }

        /// <summary>
        /// Convertit le voxel en voxel factice pour la triangulation
        /// </summary>
        /// <param name="voxel">Voxel à convertir</param>
        /// <param name="offset">Taille du chunk</param>
        public void BecomeYDummyOf(Voxel voxel, float offset)
        {
            State = voxel.State;
            Position = voxel.Position;
            Position = new Vector2(Position.x, Position.y + offset);
            XEdge = voxel.XEdge;
            YEdge = voxel.YEdge + offset;
            XNormal = voxel.XNormal;
        }

        /// <summary>
        /// Convertit le voxel en voxel factice pour la triangulation
        /// </summary>
        /// <param name="voxel">Voxel à convertir</param>
        /// <param name="offset">Taille du chunk</param>
        public void BecomeXYDummyOf(Voxel voxel, float offset)
        {
            State = voxel.State;
            Position = voxel.Position;
            Position = new Vector2(Position.x + offset, Position.y + offset);
            XEdge = voxel.XEdge + offset;
            YEdge = voxel.YEdge + offset;
        }

        #endregion
    }
}