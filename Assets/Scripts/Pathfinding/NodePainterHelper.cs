#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sperlich.Pathfinding {

	[CustomEditor(typeof(NodePainter))]
	public class NodePainterHelper : Editor {

		public NodePainter painter;
		public static Vector3 mousePos;
		public static Vector3 camPos;
		public static Ray mouseRay;
		public static bool mouseDown;
		public static bool shiftDown;
		public static bool altDown;

		void OnSceneGUI() {
			Event guiEvent = Event.current;
			if(guiEvent.type == EventType.MouseDown && Event.current.button == 0) {
				mouseDown = true;
			} else if(guiEvent.type == EventType.MouseUp && Event.current.button == 0) {
				mouseDown = false;
			}
			shiftDown = Event.current.shift;
			altDown = Event.current.alt;
			if(Camera.current != null) {
				camPos = Camera.current.transform.position;
			}
			mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
			float dstToDrawPlane = (0 - mouseRay.origin.y) / mouseRay.direction.y;
			mousePos = mouseRay.GetPoint(dstToDrawPlane);

			if(guiEvent.type == EventType.Layout) {
				HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
			}
		}
	}
}
#endif