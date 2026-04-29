using UnityEngine;

[SelectionBase]
public class VoxelGrid : MonoBehaviour {

	public int resolution;

	public GameObject voxelPrefab;

	public VoxelGridSurface surfacePrefab;

	public VoxelGridWall wallPrefab;

	public VoxelMaterials[] materials;

	public VoxelGrid xNeighbor, yNeighbor, xyNeighbor;

	private Voxel[] voxels;

	private float voxelSize, gridSize;

	private Material[] voxelMaterials;

	private VoxelRenderer[] renderers;

	private Voxel dummyX, dummyY, dummyT;

	private VoxelCell cell = new VoxelCell();

	public void Initialize (
		int resolution, float size,
		float maxFeatureAngle, float maxParallelAngle) {

		cell.sharpFeatureLimit = Mathf.Cos(maxFeatureAngle * Mathf.Deg2Rad);
		cell.parallelLimit = Mathf.Cos(maxParallelAngle * Mathf.Deg2Rad);
		this.resolution = resolution;
		gridSize = size;
		voxelSize = size / resolution;
		voxels = new Voxel[resolution * resolution];
		voxelMaterials = new Material[voxels.Length];
		
		dummyX = new Voxel();
		dummyY = new Voxel();
		dummyT = new Voxel();
		
		for (int i = 0, y = 0; y < resolution; y++) {
			for (int x = 0; x < resolution; x++, i++) {
				CreateVoxel(i, x, y);
			}
		}

		CreateRenderers();
		Refresh();
	}

	private void CreateRenderers () {
		renderers = new VoxelRenderer[materials.Length + 1];
		for (int i = 0; i < materials.Length; i++) {
			VoxelGridSurface surface =
				Instantiate(surfacePrefab) as VoxelGridSurface;
			surface.transform.parent = transform;
			surface.transform.localPosition = Vector3.zero;
			surface.Initialize(resolution, materials[i].surfaceMaterial);
			
			VoxelGridWall wall = Instantiate(wallPrefab) as VoxelGridWall;
			wall.transform.parent = transform;
			wall.transform.localPosition = Vector3.zero;
			wall.Initialize(resolution, materials[i].wallMaterial);
			
			renderers[i + 1] = new VoxelRenderer(surface, wall);
		}
	}

	private void CreateVoxel (int i, int x, int y) {
		GameObject o = Instantiate(voxelPrefab) as GameObject;
		o.transform.parent = transform;
		o.transform.localPosition =
			new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize, -0.01f);
		o.transform.localScale = Vector3.one * voxelSize * 0.1f;
		voxelMaterials[i] = o.GetComponent<MeshRenderer>().material;
		voxels[i] = new Voxel(x, y, voxelSize);
	}

	private void Refresh () {
		SetVoxelColors();
		Triangulate();
	}

	private void Triangulate () {
		for (int i = 1; i < renderers.Length; i++) {
			renderers[i].Clear();
		}
		FillFirstRowCache();
		TriangulateCellRows();
		if (yNeighbor != null) {
			TriangulateGapRow();
		}
		for (int i = 1; i < renderers.Length; i++) {
			renderers[i].Apply();
		}
	}

	private void FillFirstRowCache () {
		CacheFirstCorner(voxels[0]);
		int i;
		for (i = 0; i < resolution - 1; i++) {
			CacheNextEdgeAndCorner(i, voxels[i], voxels[i + 1]);
		}
		if (xNeighbor != null) {
			dummyX.BecomeXDummyOf(xNeighbor.voxels[0], gridSize);
			CacheNextEdgeAndCorner(i, voxels[i], dummyX);
		}
	}

	private void CacheFirstCorner (Voxel voxel) {
		if (voxel.Filled) {
			renderers[voxel.state].CacheFirstCorner(voxel);
		}
	}

	private void CacheNextEdgeAndCorner (int i, Voxel xMin, Voxel xMax) {
		if (xMin.state != xMax.state) {
			if (xMin.Filled) {
				if (xMax.Filled) {
					renderers[xMin.state].CacheXEdge(i, xMin);
					renderers[xMax.state].CacheXEdge(i, xMin);
				}
				else {
					renderers[xMin.state].CacheXEdgeWithWall(i, xMin);
				}
			}
			else {
				renderers[xMax.state].CacheXEdgeWithWall(i, xMin);
			}
		}
		if (xMax.Filled) {
			renderers[xMax.state].CacheNextCorner(i, xMax);
		}
	}

	private void CacheNextMiddleEdge (Voxel yMin, Voxel yMax) {
		for (int i = 1; i < renderers.Length; i++) {
			renderers[i].PrepareCacheForNextCell();
		}
		if (yMin.state != yMax.state) {
			if (yMin.Filled) {
				if (yMax.Filled) {
					renderers[yMin.state].CacheYEdge(yMin);
					renderers[yMax.state].CacheYEdge(yMin);
				}
				else {
					renderers[yMin.state].CacheYEdgeWithWall(yMin);
				}
			}
			else {
				renderers[yMax.state].CacheYEdgeWithWall(yMin);
			}
		}
	}

	private void SwapRowCaches () {
		for (int i = 1; i < renderers.Length; i++) {
			renderers[i].PrepareCacheForNextRow();
		}
	}
	
	private void TriangulateCellRows () {
		int cells = resolution - 1;
		for (int i = 0, y = 0; y < cells; y++, i++) {
			SwapRowCaches();
			CacheFirstCorner(voxels[i + resolution]);
			CacheNextMiddleEdge(voxels[i], voxels[i + resolution]);

			for (int x = 0; x < cells; x++, i++) {
				Voxel
					a = voxels[i],
					b = voxels[i + 1],
					c = voxels[i + resolution],
					d = voxels[i + resolution + 1];
				CacheNextEdgeAndCorner(x, c, d);
				CacheNextMiddleEdge(b, d);
				TriangulateCell(x, a, b, c, d);
			}
			if (xNeighbor != null) {
				TriangulateGapCell(i);
			}
		}
	}

	private void TriangulateGapCell (int i) {
		Voxel dummySwap = dummyT;
		dummySwap.BecomeXDummyOf(xNeighbor.voxels[i + 1], gridSize);
		dummyT = dummyX;
		dummyX = dummySwap;
		int cacheIndex = resolution - 1;
		CacheNextEdgeAndCorner(cacheIndex, voxels[i + resolution], dummyX);
		CacheNextMiddleEdge(dummyT, dummyX);
		TriangulateCell(
			cacheIndex, voxels[i], dummyT, voxels[i + resolution], dummyX);
	}

	private void TriangulateGapRow () {
		dummyY.BecomeYDummyOf(yNeighbor.voxels[0], gridSize);
		int cells = resolution - 1;
		int offset = cells * resolution;
		SwapRowCaches();
		CacheFirstCorner(dummyY);
		CacheNextMiddleEdge(voxels[cells * resolution], dummyY);

		for (int x = 0; x < cells; x++) {
			Voxel dummySwap = dummyT;
			dummySwap.BecomeYDummyOf(yNeighbor.voxels[x + 1], gridSize);
			dummyT = dummyY;
			dummyY = dummySwap;
			CacheNextEdgeAndCorner(x, dummyT, dummyY);
			CacheNextMiddleEdge(voxels[x + offset + 1], dummyY);
			TriangulateCell(
				x, voxels[x + offset], voxels[x + offset + 1], dummyT, dummyY);
		}

		if (xNeighbor != null) {
			dummyT.BecomeXYDummyOf(xyNeighbor.voxels[0], gridSize);
			CacheNextEdgeAndCorner(cells, dummyY, dummyT);
			CacheNextMiddleEdge(dummyX, dummyT);
			TriangulateCell(
				cells, voxels[voxels.Length - 1], dummyX, dummyY, dummyT);
		}
	}

	private void TriangulateCell (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
		cell.i = i;
		cell.a = a;
		cell.b = b;
		cell.c = c;
		cell.d = d;

		if (a.state == b.state) {
			if (a.state == c.state) {
				if (a.state == d.state) {
					Triangulate0000();
				}
				else {
					Triangulate0001();
				}
			}
			else {
				if (a.state == d.state) {
					Triangulate0010();
				}
				else if (c.state == d.state) {
					Triangulate0011();
				}
				else {
					Triangulate0012();
				}
			}
		}
		else {
			if (a.state == c.state) {
				if (a.state == d.state) {
					Triangulate0100();
				}
				else if (b.state == d.state) {
					Triangulate0101();
				}
				else {
					Triangulate0102();
				}
			}
			else if (b.state == c.state) {
				if (a.state == d.state) {
					Triangulate0110();
				}
				else if (b.state == d.state) {
					Triangulate0111();
				}
				else {
					Triangulate0112();
				}
			}
			else {
				if (a.state == d.state) {
					Triangulate0120();
				}
				else if (b.state == d.state) {
					Triangulate0121();
				}
				else if (c.state == d.state) {
					Triangulate0122();
				}
				else {
					Triangulate0123();
				}
			}
		}
	}

	private void Triangulate0000 () {
		FillABCD();
	}

	private void Triangulate0001 () {
		FeaturePoint f = cell.FeatureNE;
		FillABC(f);
		FillD(f);
	}
	
	private void Triangulate0010 () {
		FeaturePoint f = cell.FeatureNW;
		FillABD(f);
		FillC(f);
	}
	
	private void Triangulate0100 () {
		FeaturePoint f = cell.FeatureSE;
		FillACD(f);
		FillB(f);
	}

	private void Triangulate0111 () {
		FeaturePoint f = cell.FeatureSW;
		FillA(f);
		FillBCD(f);
	}

	private void Triangulate0011 () {
		FeaturePoint f = cell.FeatureEW;
		FillAB(f);
		FillCD(f);
	}

	private void Triangulate0101 () {
		FeaturePoint f = cell.FeatureNS;
		FillAC(f);
		FillBD(f);
	}

	private void Triangulate0012 () {
		FeaturePoint f = cell.FeatureNEW;
		FillAB(f);
		FillC(f);
		FillD(f);
	}
	
	private void Triangulate0102 () {
		FeaturePoint f = cell.FeatureNSE;
		FillAC(f);
		FillB(f);
		FillD(f);
	}
	
	private void Triangulate0121 () {
		FeaturePoint f = cell.FeatureNSW;
		FillA(f);
		FillBD(f);
		FillC(f);
	}
	
	private void Triangulate0122 () {
		FeaturePoint f = cell.FeatureSEW;
		FillA(f);
		FillB(f);
		FillCD(f);
	}

	private void Triangulate0110 () {
		FeaturePoint
			fA = cell.FeatureSW, fB = cell.FeatureSE,
			fC = cell.FeatureNW, fD = cell.FeatureNE;
		
		if (cell.HasConnectionAD(fA, fD)) {
			fB.exists &= cell.IsInsideABD(fB.position);
			fC.exists &= cell.IsInsideACD(fC.position);
			FillADToB(fB);
			FillADToC(fC);
			FillB(fB);
			FillC(fC);
		}
		else if (cell.HasConnectionBC(fB, fC)) {
			fA.exists &= cell.IsInsideABC(fA.position);
			fD.exists &= cell.IsInsideBCD(fD.position);
			FillA(fA);
			FillD(fD);
			FillBCToA(fA);
			FillBCToD(fD);
		}
		else if (cell.a.Filled && cell.b.Filled) {
			FillJoinedCorners(fA, fB, fC, fD);
		}
		else {
			FillA(fA);
			FillB(fB);
			FillC(fC);
			FillD(fD);
		}
	}

	private void Triangulate0112 () {
		FeaturePoint
			fA = cell.FeatureSW, fB = cell.FeatureSE,
			fC = cell.FeatureNW, fD = cell.FeatureNE;
		
		if (cell.HasConnectionBC(fB, fC)) {
			fA.exists &= cell.IsInsideABC(fA.position);
			fD.exists &= cell.IsInsideBCD(fD.position);
			FillA(fA);
			FillD(fD);
			FillBCToA(fA);
			FillBCToD(fD);
		}
		else if (cell.b.Filled || cell.HasConnectionAD(fA, fD)) {
			FillJoinedCorners(fA, fB, fC, fD);
		}
		else {
			FillA(fA);
			FillD(fD);
		}
	}
	
	private void Triangulate0120 () {
		FeaturePoint
			fA = cell.FeatureSW, fB = cell.FeatureSE,
			fC = cell.FeatureNW, fD = cell.FeatureNE;
		
		if (cell.HasConnectionAD(fA, fD)) {
			fB.exists &= cell.IsInsideABD(fB.position);
			fC.exists &= cell.IsInsideACD(fC.position);
			FillADToB(fB);
			FillADToC(fC);
			FillB(fB);
			FillC(fC);
		}
		else if (cell.a.Filled || cell.HasConnectionBC(fB, fC)) {
			FillJoinedCorners(fA, fB, fC, fD);
		}
		else {
			FillB(fB);
			FillC(fC);
		}
	}

	private void Triangulate0123 () {
		FillJoinedCorners(
			cell.FeatureSW, cell.FeatureSE,
			cell.FeatureNW, cell.FeatureNE);
	}
	
	private void FillA (FeaturePoint f) {
		if (cell.a.Filled) {
			renderers[cell.a.state].FillA(cell, f);
		}
	}

	private void FillB (FeaturePoint f) {
		if (cell.b.Filled) {
			renderers[cell.b.state].FillB(cell, f);
		}
	}
	
	private void FillC (FeaturePoint f) {
		if (cell.c.Filled) {
			renderers[cell.c.state].FillC(cell, f);
		}
	}
	
	private void FillD (FeaturePoint f) {
		if (cell.d.Filled) {
			renderers[cell.d.state].FillD(cell, f);
		}
	}

	private void FillABC (FeaturePoint f) {
		if (cell.a.Filled) {
			renderers[cell.a.state].FillABC(cell, f);
		}
	}
	
	private void FillABD (FeaturePoint f) {
		if (cell.a.Filled) {
			renderers[cell.a.state].FillABD(cell, f);
		}
	}
	
	private void FillACD (FeaturePoint f) {
		if (cell.a.Filled) {
			renderers[cell.a.state].FillACD(cell, f);
		}
	}
	
	private void FillBCD (FeaturePoint f) {
		if (cell.b.Filled) {
			renderers[cell.b.state].FillBCD(cell, f);
		}
	}

	private void FillAB (FeaturePoint f) {
		if (cell.a.Filled) {
			renderers[cell.a.state].FillAB(cell, f);
		}
	}
	
	private void FillAC (FeaturePoint f) {
		if (cell.a.Filled) {
			renderers[cell.a.state].FillAC(cell, f);
		}
	}
	
	private void FillBD (FeaturePoint f) {
		if (cell.b.Filled) {
			renderers[cell.b.state].FillBD(cell, f);
		}
	}
	
	private void FillCD (FeaturePoint f) {
		if (cell.c.Filled) {
			renderers[cell.c.state].FillCD(cell, f);
		}
	}

	private void FillADToB (FeaturePoint f) {
		if (cell.a.Filled) {
			renderers[cell.a.state].FillADToB(cell, f);
		}
	}
	
	private void FillADToC (FeaturePoint f) {
		if (cell.a.Filled) {
			renderers[cell.a.state].FillADToC(cell, f);
		}
	}
	
	private void FillBCToA (FeaturePoint f) {
		if (cell.b.Filled) {
			renderers[cell.b.state].FillBCToA(cell, f);
		}
	}
	
	private void FillBCToD (FeaturePoint f) {
		if (cell.b.Filled) {
			renderers[cell.b.state].FillBCToD(cell, f);
		}
	}

	private void FillABCD () {
		if (cell.a.Filled) {
			renderers[cell.a.state].FillABCD(cell);
		}
	}

	private void FillJoinedCorners (
		FeaturePoint fA, FeaturePoint fB, FeaturePoint fC, FeaturePoint fD) {
		
		FeaturePoint point = FeaturePoint.Average(fA, fB, fC, fD);
		if (!point.exists) {
			point.position = cell.AverageNESW;
			point.exists = true;
		}
		FillA(point);
		FillB(point);
		FillC(point);
		FillD(point);
	}

//	private bool IsSharpFeature (Vector2 n1, Vector2 n2) {
//		float dot = Vector2.Dot(n1, -n2);
//		return dot >= sharpFeatureLimit && dot < 0.9999f;
//	}
//
//	private static Vector2 GetIntersection (
//		Vector2 p1, Vector2 n1, Vector2 p2, Vector2 n2) {
//
//		Vector2 d2 = new Vector2(-n2.y, n2.x);
//		float u2 = -Vector2.Dot(n1, p2 - p1) / Vector2.Dot(n1, d2);
//		return p2 + d2 * u2;
//	}
//
//	private static bool IsInsideCell (Vector2 point, Voxel min, Voxel max) {
//		return
//			point.x > min.position.x && point.y > min.position.y &&
//			point.x < max.position.x && point.y < max.position.y;
//	}
//
//	private static bool IsBelowLine (Vector2 p, Vector2 start, Vector2 end) {
//		float determinant =
//			(end.x - start.x) * (p.y - start.y) -
//			(end.y - start.y) * (p.x - start.x);
//		return determinant < 0f;
//	}
//
//	private static bool ClampToCellMinMin (
//		ref Vector2 point, Voxel min, Voxel max) {
//
//		if (point.x > max.position.x || point.y > max.position.y) {
//			return false;
//		}
//		if (point.x < min.position.x) {
//			point.x = min.position.x;
//		}
//		if (point.y < min.position.y) {
//			point.y = min.position.y;
//		}
//		return true;
//	}
//
//	private static bool ClampToCellMinMax (
//		ref Vector2 point, Voxel min, Voxel max) {
//
//		if (point.x > max.position.x || point.y < min.position.y) {
//			return false;
//		}
//		if (point.x < min.position.x) {
//			point.x = min.position.x;
//		}
//		if (point.y > max.position.y) {
//			point.y = max.position.y;
//		}
//		return true;
//	}
//
//	private static bool ClampToCellMaxMin (
//		ref Vector2 point, Voxel min, Voxel max) {
//
//		if (point.x < min.position.x || point.y > max.position.y) {
//			return false;
//		}
//		if (point.x > max.position.x) {
//			point.x = max.position.x;
//		}
//		if (point.y < min.position.y) {
//			point.y = min.position.y;
//		}
//		return true;
//	}
//
//	private static bool ClampToCellMaxMax (
//		ref Vector2 point, Voxel min, Voxel max) {
//
//		if (point.x < min.position.x || point.y < min.position.y) {
//			return false;
//		}
//		if (point.x > max.position.x) {
//			point.x = max.position.x;
//		}
//		if (point.y > max.position.y) {
//			point.y = max.position.y;
//		}
//		return true;
//	}
//
//	private void TriangulateCase0 (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//	}
//
//	private void TriangulateCase15 (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//		surface.AddQuadABCD(i);
//	}
//	
//	private void TriangulateCase1 (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//		Vector2 n1 = a.xNormal;
//		Vector2 n2 = a.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			Vector2 point = GetIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
//			if (ClampToCellMaxMax(ref point, a, d)) {
//				surface.AddQuadA(i, point);
//				wall.AddACAB(i, point);
//				return;
//			}
//		}
//		surface.AddTriangleA(i);
//		wall.AddACAB(i);
//	}
//
//	private void TriangulateCase2 (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//		Vector2 n1 = a.xNormal;
//		Vector2 n2 = b.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			Vector2 point = GetIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
//			if (ClampToCellMinMax(ref point, a, d)) {
//				surface.AddQuadB(i, point);
//				wall.AddABBD(i, point);
//				return;
//			}
//		}
//		surface.AddTriangleB(i);
//		wall.AddABBD(i);
//	}
//
//	private void TriangulateCase4 (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//		Vector2 n1 = c.xNormal;
//		Vector2 n2 = a.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			Vector2 point = GetIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
//			if (ClampToCellMaxMin(ref point, a, d)) {
//				surface.AddQuadC(i, point);
//				wall.AddCDAC(i, point);
//				return;
//			}
//		}
//		surface.AddTriangleC(i);
//		wall.AddCDAC(i);
//	}
//	
//	private void TriangulateCase8 (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//		Vector2 n1 = c.xNormal;
//		Vector2 n2 = b.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			Vector2 point = GetIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
//			if (ClampToCellMinMin(ref point, a, d)) {
//				surface.AddQuadD(i, point);
//				wall.AddBDCD(i, point);
//				return;
//			}
//		}
//		surface.AddTriangleD(i);
//		wall.AddBDCD(i);
//	}
//
//	private void TriangulateCase7 (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//		Vector2 n1 = c.xNormal;
//		Vector2 n2 = b.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			Vector2 point = GetIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
//			if (IsInsideCell(point, a, d)) {
//				surface.AddHexagonABC(i, point);
//				wall.AddCDBD(i, point);
//				return;
//			}
//		}
//		surface.AddPentagonABC(i);
//		wall.AddCDBD(i);
//	}
//	
//	private void TriangulateCase11 (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//		Vector2 n1 = c.xNormal;
//		Vector2 n2 = a.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			Vector2 point = GetIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
//			if (IsInsideCell(point, a, d)) {
//				surface.AddHexagonABD(i, point);
//				wall.AddACCD(i, point);
//				return;
//			}
//		}
//		surface.AddPentagonABD(i);
//		wall.AddACCD(i);
//	}
//	
//	private void TriangulateCase13 (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//		Vector2 n1 = a.xNormal;
//		Vector2 n2 = b.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			Vector2 point = GetIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
//			if (IsInsideCell(point, a, d)) {
//				surface.AddHexagonACD(i, point);
//				wall.AddBDAB(i, point);
//				return;
//			}
//		}
//		surface.AddPentagonACD(i);
//		wall.AddBDAB(i);
//	}
//	
//	private void TriangulateCase14 (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//		Vector2 n1 = a.xNormal;
//		Vector2 n2 = a.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			Vector2 point = GetIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
//			if (IsInsideCell(point, a, d)) {
//				surface.AddHexagonBCD(i, point);
//				wall.AddABAC(i, point);
//				return;
//			}
//		}
//		surface.AddPentagonBCD(i);
//		wall.AddABAC(i);
//	}
//	
//	private void TriangulateCase3 (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//		Vector2 n1 = a.yNormal;
//		Vector2 n2 = b.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			Vector2 point = GetIntersection(a.YEdgePoint, n1, b.YEdgePoint, n2);
//			if (IsInsideCell(point, a, d)) {
//				surface.AddPentagonAB(i, point);
//				wall.AddACBD(i, point);
//				return;
//			}
//		}
//		surface.AddQuadAB(i);
//		wall.AddACBD(i);
//	}
//
//	private void TriangulateCase5 (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//		Vector2 n1 = a.xNormal;
//		Vector2 n2 = c.xNormal;
//		if (IsSharpFeature(n1, n2)) {
//			Vector2 point = GetIntersection(a.XEdgePoint, n1, c.XEdgePoint, n2);
//			if (IsInsideCell(point, a, d)) {
//				surface.AddPentagonAC(i, point);
//				wall.AddCDAB(i, point);
//				return;
//			}
//		}
//		surface.AddQuadAC(i);
//		wall.AddCDAB(i);
//	}
//
//	private void TriangulateCase10 (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//		Vector2 n1 = a.xNormal;
//		Vector2 n2 = c.xNormal;
//		if (IsSharpFeature(n1, n2)) {
//			Vector2 point = GetIntersection(a.XEdgePoint, n1, c.XEdgePoint, n2);
//			if (IsInsideCell(point, a, d)) {
//				surface.AddPentagonBD(i, point);
//				wall.AddABCD(i, point);
//				return;
//			}
//		}
//		surface.AddQuadBD(i);
//		wall.AddABCD(i);
//	}
//	
//	private void TriangulateCase12 (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//		Vector2 n1 = a.yNormal;
//		Vector2 n2 = b.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			Vector2 point = GetIntersection(a.YEdgePoint, n1, b.YEdgePoint, n2);
//			if (IsInsideCell(point, a, d)) {
//				surface.AddPentagonCD(i, point);
//				wall.AddBDAC(i, point);
//				return;
//			}
//		}
//		surface.AddQuadCD(i);
//		wall.AddBDAC(i);
//	}
//	
//	private void TriangulateCase6 (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//		bool sharp1, sharp2;
//		Vector2 point1, point2;
//
//		Vector2 n1 = a.xNormal;
//		Vector2 n2 = b.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			point1 = GetIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
//			sharp1 = ClampToCellMinMax(ref point1, a, d);
//		}
//		else {
//			point1.x = point1.y = 0f;
//			sharp1 = false;
//		}
//
//		n1 = c.xNormal;
//		n2 = a.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			point2 = GetIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
//			sharp2 = ClampToCellMaxMin(ref point2, a, d);
//		}
//		else {
//			point2.x = point2.y = 0f;
//			sharp2 = false;
//		}
//
//		if (sharp1) {
//			if (sharp2) {
//				if (IsBelowLine(point2, a.XEdgePoint, point1)) {
//					if (IsBelowLine(point2, point1, b.YEdgePoint) ||
//					    IsBelowLine(point1, point2, a.YEdgePoint)) {
//						TriangulateCase6Connected(i, a, b, c, d);
//						return;
//					}
//				}
//				else if (IsBelowLine(point2, point1, b.YEdgePoint) &&
//				         IsBelowLine(point1, c.XEdgePoint, point2)) {
//					TriangulateCase6Connected(i, a, b, c, d);
//					return;
//				}
//				surface.AddQuadB(i, point1);
//				wall.AddABBD(i, point1);
//				surface.AddQuadC(i, point2);
//				wall.AddCDAC(i, point2);
//				return;
//			}
//			if (IsBelowLine(point1, c.XEdgePoint, a.YEdgePoint)) {
//				TriangulateCase6Connected(i, a, b, c, d);
//				return;
//			}
//			surface.AddQuadB(i, point1);
//			wall.AddABBD(i, point1);
//			surface.AddTriangleC(i);
//			wall.AddCDAC(i);
//			return;
//		}
//		if (sharp2) {
//			if (IsBelowLine(point2, a.XEdgePoint, b.YEdgePoint)) {
//				TriangulateCase6Connected(i, a, b, c, d);
//				return;
//			}
//			surface.AddTriangleB(i);
//			wall.AddABBD(i);
//			surface.AddQuadC(i, point2);
//			wall.AddCDAC(i, point2);
//			return;
//		}
//		surface.AddTriangleB(i);
//		wall.AddABBD(i);
//		surface.AddTriangleC(i);
//		wall.AddCDAC(i);
//	}
//
//	private void TriangulateCase6Connected (
//		int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//
//		Vector2 n1 = a.xNormal;
//		Vector2 n2 = a.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			Vector2 point = GetIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
//			if (IsInsideCell(point, a, d) &&
//			    IsBelowLine(point, c.position, b.position)) {
//				surface.AddPentagonBCToA(i, point);
//				wall.AddABAC(i, point);
//			}
//			else {
//				surface.AddQuadBCToA(i);
//				wall.AddABAC(i);
//			}
//		}
//		else {
//			surface.AddQuadBCToA(i);
//			wall.AddABAC(i);
//		}
//
//		n1 = c.xNormal;
//		n2 = b.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			Vector2 point = GetIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
//			if (IsInsideCell(point, a, d) &&
//			    IsBelowLine(point, b.position, c.position)) {
//				surface.AddPentagonBCToD(i, point);
//				wall.AddCDBD(i, point);
//				return;
//			}
//		}
//		surface.AddQuadBCToD(i);
//		wall.AddCDBD(i);
//	}
//
//	private void TriangulateCase9 (int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//		bool sharp1, sharp2;
//		Vector2 point1, point2;
//		Vector2 n1 = a.xNormal;
//		Vector2 n2 = a.yNormal;
//
//		if (IsSharpFeature(n1, n2)) {
//			point1 = GetIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
//			sharp1 = ClampToCellMaxMax(ref point1, a, d);
//		}
//		else {
//			point1.x = point1.y = 0f;
//			sharp1 = false;
//		}
//
//		n1 = c.xNormal;
//		n2 = b.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			point2 = GetIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
//			sharp2 = ClampToCellMinMin(ref point2, a, d);
//		}
//		else {
//			point2.x = point2.y = 0f;
//			sharp2 = false;
//		}
//
//		if (sharp1) {
//			if (sharp2) {
//				if (IsBelowLine(point1, b.YEdgePoint, point2)) {
//					if (IsBelowLine(point1, point2, c.XEdgePoint) ||
//					    IsBelowLine(point2, point1, a.XEdgePoint)) {
//						TriangulateCase9Connected(i, a, b, c, d);
//						return;
//					}
//				}
//				else if (IsBelowLine(point1, point2, c.XEdgePoint) &&
//				         IsBelowLine(point2, a.YEdgePoint, point1)) {
//					TriangulateCase9Connected(i, a, b, c, d);
//					return;
//				}
//				surface.AddQuadA(i, point1);
//				wall.AddACAB(i, point1);
//				surface.AddQuadD(i, point2);
//				wall.AddBDCD(i, point2);
//				return;
//			}
//			if (IsBelowLine(point1, b.YEdgePoint, c.XEdgePoint)) {
//				TriangulateCase9Connected(i, a, b, c, d);
//				return;
//			}
//			surface.AddQuadA(i, point1);
//			wall.AddACAB(i, point1);
//			surface.AddTriangleD(i);
//			wall.AddBDCD(i);
//			return;
//		}
//		if (sharp2) {
//			if (IsBelowLine(point2, a.YEdgePoint, a.XEdgePoint)) {
//				TriangulateCase9Connected(i, a, b, c, d);
//				return;
//			}
//			surface.AddTriangleA(i);
//			wall.AddACAB(i);
//			surface.AddQuadD(i, point2);
//			wall.AddBDCD(i, point2);
//			return;
//		}
//		surface.AddTriangleA(i);
//		wall.AddACAB(i);
//		surface.AddTriangleD(i);
//		wall.AddBDCD(i);
//	}
//
//	private void TriangulateCase9Connected (
//		int i, Voxel a, Voxel b, Voxel c, Voxel d) {
//
//		Vector2 n1 = a.xNormal;
//		Vector2 n2 = b.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			Vector2 point = GetIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
//			if (IsInsideCell(point, a, d) &&
//			    IsBelowLine(point, a.position, d.position)) {
//				surface.AddPentagonADToB(i, point);
//				wall.AddBDAB(i, point);
//			}
//			else {
//				surface.AddQuadADToB(i);
//				wall.AddBDAB(i);
//			}
//		}
//		else {
//			surface.AddQuadADToB(i);
//			wall.AddBDAB(i);
//		}
//		
//		n1 = c.xNormal;
//		n2 = a.yNormal;
//		if (IsSharpFeature(n1, n2)) {
//			Vector2 point = GetIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
//			if (IsInsideCell(point, a, d) &&
//			    IsBelowLine(point, d.position, a.position)) {
//				surface.AddPentagonADToC(i, point);
//				wall.AddACCD(i, point);
//				return;
//			}
//		}
//		surface.AddQuadADToC(i);
//		wall.AddACCD(i);
//	}

	private void SetVoxelColors () {
		for (int i = 0; i < voxels.Length; i++) {
			voxelMaterials[i].color =
				voxels[i].Filled ? Color.black : Color.white;
		}
	}

	public void Apply (VoxelStencil stencil) {
		int xStart = (int)(stencil.XStart / voxelSize);
		if (xStart < 0) {
			xStart = 0;
		}
		int xEnd = (int)(stencil.XEnd / voxelSize);
		if (xEnd >= resolution) {
			xEnd = resolution - 1;
		}
		int yStart = (int)(stencil.YStart / voxelSize);
		if (yStart < 0) {
			yStart = 0;
		}
		int yEnd = (int)(stencil.YEnd / voxelSize);
		if (yEnd >= resolution) {
			yEnd = resolution - 1;
		}

		for (int y = yStart; y <= yEnd; y++) {
			int i = y * resolution + xStart;
			for (int x = xStart; x <= xEnd; x++, i++) {
				stencil.Apply(voxels[i]);
			}
		}
		SetCrossings(stencil, xStart, xEnd, yStart, yEnd);
		Refresh();
	}

	private void SetCrossings (
		VoxelStencil stencil, int xStart, int xEnd, int yStart, int yEnd) {

		bool crossHorizontalGap = false;
		bool includeLastVerticalRow = false;
		bool crossVerticalGap = false;
		
		if (xStart > 0) {
			xStart -= 1;
		}
		if (xEnd == resolution - 1) {
			xEnd -= 1;
			crossHorizontalGap = xNeighbor != null;
		}
		if (yStart > 0) {
			yStart -= 1;
		}
		if (yEnd == resolution - 1) {
			yEnd -= 1;
			includeLastVerticalRow = true;
			crossVerticalGap = yNeighbor != null;
		}

		Voxel a, b;
		for (int y = yStart; y <= yEnd; y++) {
			int i = y * resolution + xStart;
			b = voxels[i];
			for (int x = xStart; x <= xEnd; x++, i++) {
				a = b;
				b = voxels[i + 1];
				stencil.SetHorizontalCrossing(a, b);
				stencil.SetVerticalCrossing(a, voxels[i + resolution]);
			}
			stencil.SetVerticalCrossing(b, voxels[i + resolution]);
			if (crossHorizontalGap) {
				dummyX.BecomeXDummyOf(
					xNeighbor.voxels[y * resolution], gridSize);
				stencil.SetHorizontalCrossing(b, dummyX);
			}
		}

		if (includeLastVerticalRow) {
			int i = voxels.Length - resolution + xStart;
			b = voxels[i];
			for (int x = xStart; x <= xEnd; x++, i++) {
				a = b;
				b = voxels[i + 1];
				stencil.SetHorizontalCrossing(a, b);
				if (crossVerticalGap) {
					dummyY.BecomeYDummyOf(yNeighbor.voxels[x], gridSize);
					stencil.SetVerticalCrossing(a, dummyY);
				}
			}
			if (crossVerticalGap) {
				dummyY.BecomeYDummyOf(yNeighbor.voxels[xEnd + 1], gridSize);
				stencil.SetVerticalCrossing(b, dummyY);
			}
			if (crossHorizontalGap) {
				dummyX.BecomeXDummyOf(
					xNeighbor.voxels[voxels.Length - resolution], gridSize);
				stencil.SetHorizontalCrossing(b, dummyX);
			}
		}
	}
}