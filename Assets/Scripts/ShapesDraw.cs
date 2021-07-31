using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShapesDraw {
	
	public static void DrawLine(Vector3 start, Vector3 end, Color color) {
		using(Draw.Command(Camera.main)) {
			Draw.ThicknessSpace = ThicknessSpace.Meters;
			Draw.LineGeometry = LineGeometry.Billboard;
			Draw.Radius = 0.05f;
			Draw.Thickness = 0.01f;
			Draw.Line(start, end, color);
		}
	}

	public static void DrawPoint(Vector3 point, float radius, Color color) {
		using(Draw.Command(Camera.main)) {
			Draw.Radius = radius;
			Draw.Sphere(point, radius, color);
		}
	}
}
