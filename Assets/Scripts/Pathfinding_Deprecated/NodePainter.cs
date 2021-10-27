#if UNITY_EDITOR
using Sperlich.Types;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sperlich.Pathfinding {

    [ExecuteInEditMode]
    public class NodePainter : MonoBehaviour {

        public enum PaintMode { single, area }
        public enum PaintOperation { add, remove, overwrite }

        [Header("Configuration")]
        public Node.NodeType nodeType;
        public PaintMode mode;
        public PaintOperation operation;
        [Range(0f, 100f)]
        public float snapTreshold;
        [Min(0f)]
        public float paintSpeed;
        [Min(1f)]
        public float paintRadius = 5f;
        [HideInInspector] 
        public List<Node> selectedNodes;
        PathfindingMesh grid;
        Float3 rayPoint;
        Float3 rayNormal;
        Float3 currentPoint;
        bool lastMouseDownState;
        bool lastAltDownState;

		void Awake() {
            OnValidate();
		}
		void OnValidate() {
            grid = GetComponent<PathfindingMesh>();
        }
		void Reset() {
            nodeType = Node.NodeType.ground;
            mode = PaintMode.single;
            operation = PaintOperation.add;
            snapTreshold = 1f;
            paintSpeed = 0.2f;
            paintRadius = 10f;
            OnValidate();
		}

        void Update() {
            selectedNodes = new List<Node>();
            if(lastAltDownState != NodePainterHelper.altDown && NodePainterHelper.altDown) {
                if(mode == PaintMode.single) {
                    mode = PaintMode.area;
				} else {
                    mode = PaintMode.single;
				}
			}

            if(Physics.Raycast(NodePainterHelper.mouseRay, out RaycastHit hit, Mathf.Infinity)) {
                rayNormal = hit.normal;
                rayPoint = new Vector3(Mathf.Round(hit.point.x * snapTreshold) / snapTreshold, Mathf.Round(hit.point.y * snapTreshold) / snapTreshold, Mathf.Round(hit.point.z * snapTreshold) / snapTreshold);
                currentPoint = new Float3(rayPoint, 2);
                if(mode == PaintMode.area) {
                    selectedNodes = grid.GetNodesWithinRadius(rayPoint, paintRadius);
                } else {
                    selectedNodes.Add(grid.GetNodeFromPos(rayPoint));
				}
                if(NodePainterHelper.shiftDown) {
                    RemoveNode();
                } else {
                    if((operation == PaintOperation.add || operation == PaintOperation.overwrite) && !NodePainterHelper.shiftDown) {
                        AddNode();
                    } else if(operation == PaintOperation.remove) {
                        RemoveNode();
                    }
                }
            }
            lastAltDownState = NodePainterHelper.altDown;
            lastMouseDownState = NodePainterHelper.mouseDown;
        }

        public void AddNode() {
            if(NodePainterHelper.mouseDown) {
                if(mode == PaintMode.area) {
                    if(operation != PaintOperation.overwrite) {
                        foreach(Node n in grid.GetNodesWithinRadius(rayPoint, paintRadius)) {
                            grid.AddNode(n.pos, paintRadius, nodeType);
                        }
                    } else {
                        foreach(Node n in grid.GetNodesWithinRadius(rayPoint, paintRadius)) {
                            grid.UpdateNode(n, paintRadius, nodeType);
                        }
                    }
                } else if(mode == PaintMode.single) {
                    if(operation != PaintOperation.overwrite) {
                        grid.AddNode(currentPoint, paintRadius, nodeType);
                    } else {
                        grid.UpdateNode(grid.GetNodeAt(currentPoint), paintRadius, nodeType);
                    }
                }
            }
        }

        public void RemoveNode() {
            if(NodePainterHelper.mouseDown) {
                if(mode == PaintMode.area) {
                    int count = 0;
                    foreach(Node n in grid.GetNodesWithinRadius(rayPoint, paintRadius)) {
                        grid.RemoveNode(n);
                        count++;
                    }
                } else if(mode == PaintMode.single) {
                    grid.RemoveNode(rayPoint);
                }
            }
        }

		void OnDrawGizmosSelected() {
            Update();
            Handles.color = Color.black;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Handles.DrawWireDisc(rayPoint, rayNormal, paintRadius);
            Handles.color = new Color32(45, 125, 200, 50);
            if(NodePainterHelper.shiftDown) {
                Handles.color = new Color32(200, 125, 45, 50);
			}
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
            Handles.DrawSolidDisc(rayPoint + rayNormal / 100f, rayNormal, paintRadius);
            Handles.color = Color.white;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Handles.DrawSolidDisc(rayPoint + rayNormal / 100f, rayNormal, 0.1f);
        }
    }
}
#endif