using System;
using Unity.Mathematics;

namespace Assets.Project.Scripts.Runtime.Models.MarchingSquares
{
    /// <summary>
    /// Vartice d'une cellule du mesh généré par la triangulation
    /// </summary>
    [Serializable]
    public struct Voxel
    {
        #region Propriétés

        /// <summary>
        /// true si le voxel contient une valeur
        /// </summary>
        public readonly bool Filled => State > 0f;

        /// <summary>
        /// Point d'intersection sur l'edge X
        /// </summary>
        public readonly float2 XEdgePoint => new(XEdge, Position.y);

        /// <summary>
        /// Point d'intersection sur l'edge Y
        /// </summary>
        public readonly float2 YEdgePoint => new(Position.x, YEdge);

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
        /// <param name="x">Coord X</param>
        /// <param name="y">Coord Y</param>
        /// <param name="voxelSize">Taille du voxel</param>
        public Voxel(int x, int y, float voxelSize)
        {
            State = 0;
            Position = new float2((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize);
            XEdge = float.MinValue;
            YEdge = float.MinValue;
            XNormal = float2.zero;
            YNormal = float2.zero;
        }

        /// <summary>
        /// Constructeur
        /// </summary>
        public Voxel(int state, float2 position, float xEdge, float yEdge, float2 xNormal, float2 yNormal)
        {
            State = state;
            Position = position;
            XEdge = xEdge;
            YEdge = yEdge;
            XNormal = xNormal;
            YNormal = yNormal;
        }

        #endregion
    }
}