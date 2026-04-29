using UnityEngine;

public struct FeaturePoint {
	
	public Vector2 position;
	
	public bool exists;

	public static FeaturePoint Average (
		FeaturePoint a, FeaturePoint b, FeaturePoint c) {
		
		FeaturePoint average;
		average.position = Vector2.zero;
		float features = 0f;
		if (a.exists) {
			average.position += a.position;
			features += 1f;
		}
		if (b.exists) {
			average.position += b.position;
			features += 1f;
		}
		if (c.exists) {
			average.position += c.position;
			features += 1f;
		}
		if (features > 0f) {
			average.position /= features;
			average.exists = true;
		}
		else {
			average.exists = false;
		}
		return average;
	}

	public static FeaturePoint Average (
		FeaturePoint a, FeaturePoint b, FeaturePoint c, FeaturePoint d) {
		
		FeaturePoint average;
		average.position = Vector2.zero;
		float features = 0f;
		if (a.exists) {
			average.position += a.position;
			features += 1f;
		}
		if (b.exists) {
			average.position += b.position;
			features += 1f;
		}
		if (c.exists) {
			average.position += c.position;
			features += 1f;
		}
		if (d.exists) {
			average.position += d.position;
			features += 1f;
		}
		if (features > 0f) {
			average.position /= features;
			average.exists = true;
		}
		else {
			average.exists = false;
		}
		return average;
	}
}