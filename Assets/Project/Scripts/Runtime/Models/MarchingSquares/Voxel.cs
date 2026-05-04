using System;
using Unity.Mathematics;

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
        public bool Filled => State > 0f;

        /// <summary>
        /// Point d'intersection sur l'edge X
        /// </summary>
        public float2 XEdgePoint => new(XEdge, Position.y);

        /// <summary>
        /// Point d'intersection sur l'edge Y
        /// </summary>
        public float2 YEdgePoint => new(Position.x, YEdge);

        /// <summary>
        /// Etat du voxel
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// Position
        /// </summary>
        public float2 Position { get; set; }

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
        public float2 XNormal { get; set; }

        /// <summary>
        /// Normale du voxel
        /// </summary>
        public float2 YNormal { get; set; }

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
            Position = new float2((x + 0.5f) * size, (y + 0.5f) * size);
            XEdge = float.MinValue;
            YEdge = float.MinValue;
        }

        #endregion
    }
}