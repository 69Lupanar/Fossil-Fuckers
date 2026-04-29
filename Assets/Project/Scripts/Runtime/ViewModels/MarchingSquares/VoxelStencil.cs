using UnityEngine;

public class VoxelStencil {

	protected int fillType;

	protected float centerX, centerY, radius;

	public float XStart {
		get {
			return centerX - radius;
		}
	}
	
	public float XEnd {
		get {
			return centerX + radius;
		}
	}
	
	public float YStart {
		get {
			return centerY - radius;
		}
	}
	
	public float YEnd {
		get {
			return centerY + radius;
		}
	}

	public virtual void Initialize (int fillType, float radius) {
		this.fillType = fillType;
		this.radius = radius;
	}

	public virtual void SetCenter (float x, float y) {
		centerX = x;
		centerY = y;
	}

	public virtual void Apply (Voxel voxel) {
		Vector2 p = voxel.position;
		if (p.x >= XStart && p.x <= XEnd && p.y >= YStart && p.y <= YEnd) {
			voxel.state = fillType;
		}
	}

	public void SetHorizontalCrossing (Voxel xMin, Voxel xMax) {
		if (xMin.state != xMax.state) {
			FindHorizontalCrossing(xMin, xMax);
		}
		else {
			xMin.xEdge = float.MinValue;
		}
	}

	protected virtual void FindHorizontalCrossing (Voxel xMin, Voxel xMax) {
		if (xMin.position.y < YStart || xMin.position.y > YEnd) {
			return;
		}
		if (xMin.state == fillType) {
			if (xMin.position.x <= XEnd && xMax.position.x >= XEnd) {
				if (xMin.xEdge == float.MinValue || xMin.xEdge < XEnd) {
					xMin.xEdge = XEnd;
					xMin.xNormal =
						new Vector2(fillType > xMax.state ? 1f : -1f, 0f);
				}
				else {
					ValidateHorizontalNormal(xMin, xMax);
				}
			}
		}
		else if (xMax.state == fillType) {
			if (xMin.position.x <= XStart && xMax.position.x >= XStart) {
				if (xMin.xEdge == float.MinValue || xMin.xEdge > XStart) {
					xMin.xEdge = XStart;
					xMin.xNormal =
						new Vector2(fillType > xMin.state ? -1f : 1f, 0f);
				}
				else {
					ValidateHorizontalNormal(xMin, xMax);
				}
			}
		}
	}
	
	public void SetVerticalCrossing (Voxel yMin, Voxel yMax) {
		if (yMin.state != yMax.state) {
			FindVerticalCrossing(yMin, yMax);
		}
		else {
			yMin.yEdge = float.MinValue;
		}
	}
	
	protected virtual void FindVerticalCrossing (Voxel yMin, Voxel yMax) {
		if (yMin.position.x < XStart || yMin.position.x > XEnd) {
			return;
		}
		if (yMin.state == fillType) {
			if (yMin.position.y <= YEnd && yMax.position.y >= YEnd) {
				if (yMin.yEdge == float.MinValue || yMin.yEdge < YEnd) {
					yMin.yEdge = YEnd;
					yMin.yNormal =
						new Vector2(0f, fillType > yMax.state ? 1f : -1f);
				}
				else {
					ValidateVerticalNormal(yMin, yMax);
				}
			}
		}
		else if (yMax.state == fillType) {
			if (yMin.position.y <= YStart && yMax.position.y >= YStart) {
				if (yMin.yEdge == float.MinValue || yMin.yEdge > YStart) {
					yMin.yEdge = YStart;
					yMin.yNormal =
						new Vector2(0f, fillType > yMin.state ? -1f : 1f);
				}
				else {
					ValidateVerticalNormal(yMin, yMax);
				}
			}
		}
	}

	protected static void ValidateHorizontalNormal (Voxel xMin, Voxel xMax) {
		if (xMin.state < xMax.state) {
			if (xMin.xNormal.x > 0f) {
				xMin.xNormal = -xMin.xNormal;
			}
		}
		else if (xMin.xNormal.x < 0f) {
			xMin.xNormal = -xMin.xNormal;
		}
	}

	protected static void ValidateVerticalNormal (Voxel yMin, Voxel yMax) {
		if (yMin.state < yMax.state) {
			if (yMin.yNormal.y > 0f) {
				yMin.yNormal = -yMin.yNormal;
			}
		}
		else if (yMin.yNormal.y < 0f) {
			yMin.yNormal = -yMin.yNormal;
		}
	}
}