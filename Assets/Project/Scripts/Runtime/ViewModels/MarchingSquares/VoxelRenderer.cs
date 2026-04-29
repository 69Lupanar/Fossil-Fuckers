using UnityEngine;
using System;

[Serializable]
public struct VoxelRenderer {
	
	[SerializeField]
	private VoxelGridSurface surface;
	
	[SerializeField]
	private VoxelGridWall wall;
	
	public VoxelRenderer (VoxelGridSurface surface, VoxelGridWall wall) {
		this.surface = surface;
		this.wall = wall;
	}

	public void Clear () {
		surface.Clear();
		wall.Clear();
	}
	
	public void Apply () {
		surface.Apply();
		wall.Apply();
	}
	
	public void PrepareCacheForNextCell () {
		surface.PrepareCacheForNextCell();
		wall.PrepareCacheForNextCell();
	}
	
	public void PrepareCacheForNextRow () {
		surface.PrepareCacheForNextRow();
		wall.PrepareCacheForNextRow();
	}
	
	public void CacheFirstCorner (Voxel voxel) {
		surface.CacheFirstCorner(voxel);
	}
	
	public void CacheNextCorner (int i, Voxel voxel) {
		surface.CacheNextCorner(i, voxel);
	}
	
	public void CacheXEdge (int i, Voxel voxel) {
		surface.CacheXEdge(i, voxel);
	}
	
	public void CacheXEdgeWithWall (int i, Voxel voxel) {
		surface.CacheXEdge(i, voxel);
		wall.CacheXEdge(i, voxel);
	}
	
	public void CacheYEdge (Voxel voxel) {
		surface.CacheYEdge(voxel);
	}
	
	public void CacheYEdgeWithWall (Voxel voxel) {
		surface.CacheYEdge(voxel);
		wall.CacheYEdge(voxel);
	}

	public void FillA (VoxelCell cell, FeaturePoint f) {
		if (f.exists) {
			surface.AddQuadA(cell.i, f.position);
			if (!cell.c.Filled) {
				wall.AddFromAC(cell.i, f.position);
			}
			if (!cell.b.Filled) {
				wall.AddToAB(cell.i, f.position);
			}
		}
		else {
			surface.AddTriangleA(cell.i);
			if (!cell.b.Filled) {
				wall.AddACAB(cell.i);
			}
		}
	}

	public void FillB (VoxelCell cell, FeaturePoint f) {
		if (f.exists) {
			surface.AddQuadB(cell.i, f.position);
			if (!cell.a.Filled) {
				wall.AddFromAB(cell.i, f.position);
			}
			if (!cell.d.Filled) {
				wall.AddToBD(cell.i, f.position);
			}
		}
		else {
			surface.AddTriangleB(cell.i);
			if (!cell.a.Filled) {
				wall.AddABBD(cell.i);
			}
		}
	}
	
	public void FillC (VoxelCell cell, FeaturePoint f) {
		if (f.exists) {
			surface.AddQuadC(cell.i, f.position);
			if (!cell.d.Filled) {
				wall.AddFromCD(cell.i, f.position);
			}
			if (!cell.a.Filled) {
				wall.AddToAC(cell.i, f.position);
			}
		}
		else {
			surface.AddTriangleC(cell.i);
			if (!cell.a.Filled) {
				wall.AddCDAC(cell.i);
			}
		}
	}
	
	public void FillD (VoxelCell cell, FeaturePoint f) {
		if (f.exists) {
			surface.AddQuadD(cell.i, f.position);
			if (!cell.b.Filled) {
				wall.AddFromBD(cell.i, f.position);
			}
			if (!cell.c.Filled) {
				wall.AddToCD(cell.i, f.position);
			}
		}
		else {
			surface.AddTriangleD(cell.i);
			if (!cell.b.Filled) {
				wall.AddBDCD(cell.i);
			}
		}
	}

	public void FillABC (VoxelCell cell, FeaturePoint f) {
		if (f.exists) {
			surface.AddHexagonABC(cell.i, f.position);
			if (!cell.d.Filled) {
				wall.AddCDBD(cell.i, f.position);
			}
		}
		else {
			surface.AddPentagonABC(cell.i);
			if (!cell.d.Filled) {
				wall.AddCDBD(cell.i);
			}
		}
	}
	
	public void FillABD (VoxelCell cell, FeaturePoint f) {
		if (f.exists) {
			surface.AddHexagonABD(cell.i, f.position);
			if (!cell.c.Filled) {
				wall.AddACCD(cell.i, f.position);
			}
		}
		else {
			surface.AddPentagonABD(cell.i);
			if (!cell.c.Filled) {
				wall.AddACCD(cell.i);
			}
		}
	}
	
	public void FillACD (VoxelCell cell, FeaturePoint f) {
		if (f.exists) {
			surface.AddHexagonACD(cell.i, f.position);
			if (!cell.b.Filled) {
				wall.AddBDAB(cell.i, f.position);
			}
		}
		else {
			surface.AddPentagonACD(cell.i);
			if (!cell.b.Filled) {
				wall.AddBDAB(cell.i);
			}
		}
	}
	
	public void FillBCD (VoxelCell cell, FeaturePoint f) {
		if (f.exists) {
			surface.AddHexagonBCD(cell.i, f.position);
			if (!cell.a.Filled) {
				wall.AddABAC(cell.i, f.position);
			}
		}
		else {
			surface.AddPentagonBCD(cell.i);
			if (!cell.a.Filled) {
				wall.AddABAC(cell.i);
			}
		}
	}

	public void FillAB (VoxelCell cell, FeaturePoint f) {
		if (f.exists) {
			surface.AddPentagonAB(cell.i, f.position);
			if (!cell.c.Filled) {
				wall.AddFromAC(cell.i, f.position);
			}
			if (!cell.d.Filled) {
				wall.AddToBD(cell.i, f.position);
			}
		}
		else {
			surface.AddQuadAB(cell.i);
			if (!cell.c.Filled) {
				wall.AddACBD(cell.i);
			}
		}
	}
	
	public void FillAC (VoxelCell cell, FeaturePoint f) {
		if (f.exists) {
			surface.AddPentagonAC(cell.i, f.position);
			if (!cell.d.Filled) {
				wall.AddFromCD(cell.i, f.position);
			}
			if (!cell.b.Filled) {
				wall.AddToAB(cell.i, f.position);
			}
		}
		else {
			surface.AddQuadAC(cell.i);
			if (!cell.b.Filled) {
				wall.AddCDAB(cell.i);
			}
		}
	}
	
	public void FillBD (VoxelCell cell, FeaturePoint f) {
		if (f.exists) {
			surface.AddPentagonBD(cell.i, f.position);
			if (!cell.a.Filled) {
				wall.AddFromAB(cell.i, f.position);
			}
			if (!cell.c.Filled) {
				wall.AddToCD(cell.i, f.position);
			}
		}
		else {
			surface.AddQuadBD(cell.i);
			if (!cell.a.Filled) {
				wall.AddABCD(cell.i);
			}
		}
	}
	
	public void FillCD (VoxelCell cell, FeaturePoint f) {
		if (f.exists) {
			surface.AddPentagonCD(cell.i, f.position);
			if (!cell.b.Filled) {
				wall.AddFromBD(cell.i, f.position);
			}
			if (!cell.a.Filled) {
				wall.AddToAC(cell.i, f.position);
			}
		}
		else {
			surface.AddQuadCD(cell.i);
			if (!cell.a.Filled) {
				wall.AddBDAC(cell.i);
			}
		}
	}

	public void FillADToB (VoxelCell cell, FeaturePoint f) {
		if (f.exists) {
			surface.AddPentagonADToB(cell.i, f.position);
			if (!cell.b.Filled) {
				wall.AddBDAB(cell.i, f.position);
			}
		}
		else {
			surface.AddQuadADToB(cell.i);
			if (!cell.b.Filled) {
				wall.AddBDAB(cell.i);
			}
		}
	}
	
	public void FillADToC (VoxelCell cell, FeaturePoint f) {
		if (f.exists) {
			surface.AddPentagonADToC(cell.i, f.position);
			if (!cell.c.Filled) {
				wall.AddACCD(cell.i, f.position);
			}
		}
		else {
			surface.AddQuadADToC(cell.i);
			if (!cell.c.Filled) {
				wall.AddACCD(cell.i);
			}
		}
	}
	
	public void FillBCToA (VoxelCell cell, FeaturePoint f) {
		if (f.exists) {
			surface.AddPentagonBCToA(cell.i, f.position);
			if (!cell.a.Filled) {
				wall.AddABAC(cell.i, f.position);
			}
		}
		else {
			surface.AddQuadBCToA(cell.i);
			if (!cell.a.Filled) {
				wall.AddABAC(cell.i);
			}
		}
	}
	
	public void FillBCToD (VoxelCell cell, FeaturePoint f) {
		if (f.exists) {
			surface.AddPentagonBCToD(cell.i, f.position);
			if (!cell.d.Filled) {
				wall.AddCDBD(cell.i, f.position);
			}
		}
		else {
			surface.AddQuadBCToD(cell.i);
			if (!cell.d.Filled) {
				wall.AddCDBD(cell.i);
			}
		}
	}

	public void FillABCD (VoxelCell cell) {
		surface.AddQuadABCD(cell.i);
	}
}