using System;
using UnityEngine;

namespace Assets.Project.Scripts.Runtime.ViewModels.MarchingSquares
{
    /// <summary>
    /// Materials d'une surface
    /// </summary>
    [Serializable]
    public struct VoxelMaterials
    {
        /// <summary>
        /// Material
        /// </summary>
        public Material surfaceMaterial, wallMaterial;
    }
}