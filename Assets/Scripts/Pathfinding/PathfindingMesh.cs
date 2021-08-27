using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using Sperlich.Types;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Sperlich.Debug.Draw;
#if UNITY_EDITOR
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
#endif

namespace Sperlich.Pathfinding {

#if UNITY_EDITOR
	[RequireComponent(typeof(NodePainter))]
	[ExecuteInEditMode]
#endif
	public class PathfindingMesh : MonoBehaviour {
		[Header("Configuration")]
		public string gridName;
		public List<Node> Nodes => pathMesh.nodes;

		[Header("Generation")]
		public bool enableCrossConnections;
		public float crossDistance = 1.5f;
		public Vector3 dimensions;
		public LayerMask generationLayer;

		[Header("Debug")]
		public float debugDrawDistance = 100f;
		public bool showNodes;
		public bool showPathValues;
		public bool showLines;
		public bool showDistances;
		[Header("Simulate Performance")]
		public int simulationIterations;
		public bool showExamplePath;
		public Transform examplePathStart;
		public Transform examplePathEnd;

		[Header("Information")]
		public int totalNodes;
		public int simulationFpsSpeed;
		string path;
		PathMesh pathMesh;
#if UNITY_EDITOR
		public NodePainter painter { get; set; }
		EditorSceneManager.SceneClosingCallback closingScene;
		EditorSceneManager.SceneSavingCallback saveScene;
#endif

		void Awake() {
			pathMesh = new PathMesh();
			totalNodes = Nodes.Count;
#if UNITY_EDITOR
			closingScene += (Scene scene, bool removingScene) => {
				SaveGrid();
				EditorSceneManager.sceneClosing -= closingScene;
			};
			saveScene += (Scene scene, string path) => {
				SaveGrid();
				EditorSceneManager.sceneSaving -= saveScene;
			};
			EditorSceneManager.sceneSaving += saveScene;
			EditorSceneManager.sceneClosing += closingScene;
			painter = GetComponent<NodePainter>();
#endif
			if(transform.childCount < 2) {
				examplePathStart = new GameObject("ExamplePathStart").transform;
				examplePathEnd = new GameObject("ExamplePathEnd").transform;
				examplePathStart.SetParent(transform);
				examplePathEnd.SetParent(transform);
			} else if(examplePathStart == null && examplePathEnd == null) {
				examplePathStart = transform.GetChild(0);
				examplePathEnd = transform.GetChild(1);
			}
			LoadGrid();
		}
		void Reset() {
			gridName = "untitled_" + gameObject.GetHashCode();
			showNodes = true;
			debugDrawDistance = 100f;
			pathMesh = new PathMesh();
		}
		void Update() {
			System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
			if(showExamplePath && pathMesh != null && examplePathStart != null && examplePathEnd != null) {
				watch.Start();
				var path = new List<Node>();
				for(int i = 0; i < simulationIterations; i++)
					path = FindPath(examplePathStart.position, examplePathEnd.position);
				watch.Stop();
				DrawPathLines(examplePathStart.position, examplePathEnd.position, path);
			}
			simulationFpsSpeed = (int)(1000d / watch.Elapsed.TotalMilliseconds);
		}

#if UNITY_EDITOR
		[DidReloadScripts]
		public static void ScriptReload() {
			new List<PathfindingMesh>(FindObjectsOfType<PathfindingMesh>()).ForEach(pm => {
				pm.Awake();
			});
		}
		void OnValidate() {
			Reload();
		}

		public void GenerateGrid() {
			ClearGrid();
			for(float x = -dimensions.x; x < dimensions.x; x++) {
				Ray? lastRay = null;
				for(float z = -dimensions.z; z < dimensions.z; z++) {
					var ray = new Ray(new Vector3(x * painter.paintRadius, dimensions.y, z * painter.paintRadius) + transform.position, Vector3.down);
					if(enableCrossConnections && lastRay != null) {
						Ray crossRay = new Ray(ray.origin + new Vector3((x / 2f) * painter.paintRadius, 0, (z / 2f) * painter.paintRadius), Vector3.down);
						if(Physics.Raycast(crossRay, out RaycastHit crossHit, Mathf.Infinity, generationLayer)) {
							AddNode(new Float3(crossHit.point), painter.paintRadius, Node.NodeType.ground);
						}
					}
					lastRay = ray;
					if(Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, generationLayer)) {
						AddNode(new Float3(hit.point), painter.paintRadius, Node.NodeType.ground);
					}
				}
			}
			Reload();
		}

		public void AddNode(Float3 pos, float distance, Node.NodeType typ) {
			pos = new Float3(pos, 2);

			if(Nodes.Find(n => n.pos == pos) != null) return;
			Nodes.Add(new Node(this, pos, typ, distance));

			EditorSceneManager.MarkSceneDirty(gameObject.scene);
			Reload();
		}

		public void RemoveNode(Float3 pos) {
			pos = new Float3(pos, 2);
			var node = Nodes.Find(n => n.pos == pos);
			if(node != null) {
				RemoveNode(node);
			}
		}

		public void RemoveNode(Node n) {
			Nodes.Remove(n);
			if(n != null && n.Neighbours != null) {
				foreach(KeyValuePair<Node, float> node in n.Neighbours) {
					node.Key.Neighbours.Remove(n);
				}
			}
			totalNodes = Nodes.Count;
			EditorSceneManager.MarkSceneDirty(gameObject.scene);
			Reload();
		}

		public void UpdateNode(Node node, float distance, Node.NodeType typ = Node.NodeType.ground) {
			if(node != null) {
				node.type = typ;
				node.dist = distance;
				node.Neighbours = new Dictionary<Node, float>();
				node.FetchNeighbours(distance);
				foreach(Node n in node.Neighbours.Keys.ToArray()) {
					n.FetchNeighbours(distance);
				}
				EditorSceneManager.MarkSceneDirty(gameObject.scene);
			}
		}

		public void SaveGrid() {
			/*if(!Directory.Exists(Application.streamingAssetsPath)) {
				Directory.CreateDirectory(Application.streamingAssetsPath);
				AssetDatabase.Refresh();
			}*/
			path = Application.dataPath + "/Resources/Levels/" + gridName + ".json";
			pathMesh.name = gridName;

			string json = JsonUtility.ToJson(pathMesh, false);
			File.WriteAllText(path, json);
			AssetDatabase.Refresh();
		}
#endif

		public void ClearGrid() {
			pathMesh = new PathMesh(gridName);
		}

		public List<Node> FindPath(Vector3 start, Vector3 end) => FindPath(GetNodeFromPos(start, Mathf.Infinity), GetNodeFromPos(end, Mathf.Infinity));
		public List<Node> FindPath(Node start, Node dest) {
			var openSet = new List<Node>();
			var closedSet = new HashSet<Node>();
			openSet.Add(start);

			while(openSet.Count > 0) {
				openSet.Sort();
				var current = openSet[0];
				openSet.Remove(current);
				closedSet.Add(current);

				if(current == dest) {
					return RetracePath(start, dest);
				}
				foreach(KeyValuePair<Node, float> neighbour in current.Neighbours) {
					if(closedSet.Contains(neighbour.Key)) {
						continue;
					}
					float newMovementCostToNeighbour = current.gCost + neighbour.Value;

					if(newMovementCostToNeighbour < neighbour.Key.gCost || !openSet.Contains(neighbour.Key)) {
						neighbour.Key.gCost = newMovementCostToNeighbour;
						neighbour.Key.hCost = neighbour.Key.GetDistance(dest);
						neighbour.Key.Parent = current;

						if(!openSet.Contains(neighbour.Key)) openSet.Add(neighbour.Key);
					}
				}
			}
			return null;
		}


		public Node GetNodeAt(Vector3 origin) => Nodes.Find(n => n.pos == new Float3(origin, 2));
		public Node GetNodeFromPos(Vector3 origin, float treshold = 0.01f) {
			if(Nodes != null) {
				Node nearest = null;
				float d = treshold;
				foreach(Node n in Nodes) {
					float dist = Vector3.Distance(n.pos, origin);
					if(dist <= treshold) {
						if(dist < d) {
							d = dist;
							nearest = n;
						}
					}
				}
				return nearest;
			}
			return null;
		}

		public List<Node> GetNodesWithinRadius(Vector3 origin, float radius) {
			var ns = new List<Node>();
			if(Nodes != null) {
				foreach(Node n in Nodes) {
					float dist = Vector3.Distance(n.pos, origin);
					if(dist <= radius) {
						ns.Add(n);
					}
				}
			}
			return ns;
		}

		public void DrawPathLines(List<Node> path) {
			if(path != null && path.Count >= 2) {
				DrawPathLines(path[0].pos, path[path.Count - 1].pos, path);
			}
		}
		public void DrawPathLines(Vector3 origin, Vector3 destination, List<Node> nodeList) {
			if(nodeList != null && nodeList.Count > 0) {
				Vector3 lastPos = origin;
				for(int i = 0; i < nodeList.Count; i++) {
					Vector3 pos = nodeList[i].pos;
					Draw.Line(pos, lastPos, Color.blue);
					lastPos = pos;
				}
				Draw.Line(lastPos, destination, Color.blue);
			}
		}
		
		List<Node> RetracePath(Node startNode, Node endNode) {
			var path = new List<Node>();
			var current = endNode;

			while(current != startNode) {
				path.Add(current);
				current = current.Parent;
			}
			path.Reverse();
			return path;
		}

		public void Reload() {
			if(pathMesh != null) {
				foreach(Node n in Nodes) {
					n.Grid = this;
				}
				foreach(Node n in Nodes) {
					n.FetchNeighbours(n.dist);
				}
				totalNodes = Nodes.Count;
			}
		}

		public void LoadGrid() {
			ClearGrid();
			path = "Levels/" + gridName;
			var asset = Resources.Load<TextAsset>(path);
			if(asset != null) {
				pathMesh = JsonUtility.FromJson<PathMesh>(asset.text);
			} else if(pathMesh == null) {
#if UNITY_EDITOR
				Reset();
				SaveGrid();
#endif
			}

			Reload();
		}

#if UNITY_EDITOR
		void OnDrawGizmos() {
			if(pathMesh != null) {
				foreach(Node n in Nodes) {
					if(Vector3.Distance(n.pos, NodePainterHelper.camPos) < debugDrawDistance) {
						switch(n.type) {
							case Node.NodeType.ground:
								Gizmos.color = new Color32(255, 255, 255, 200);
								break;
							case Node.NodeType.wall:
								Gizmos.color = new Color32(200, 25, 50, 200);
								break;
							default:
								Gizmos.color = new Color(0, 0, 0);
								break;
						}
						if(showDistances) {
							Handles.Label(n.pos, n.dist + "");
						}
						if(n.Neighbours != null) {
							foreach(KeyValuePair<Node, float> neigh in n.Neighbours) {
								if(showLines) {
									if(neigh.Key.type == Node.NodeType.wall) {
										Gizmos.color = new Color(0.5f, 0.05f, 0.05f);
									}
									Gizmos.DrawLine(n.pos, neigh.Key.pos);
								}
								if(showPathValues) {
									Handles.Label(Vector3.Lerp(n.pos, neigh.Key.pos, 0.5f), neigh.Value + "");
								}
							}
						}
						if(showNodes) {
							if(painter.selectedNodes.Contains(n)) {
								Gizmos.color = Color.white;
								if(NodePainterHelper.shiftDown) {
									Gizmos.color = Color.red;
								}
							}
							Gizmos.DrawSphere(n.pos, 0.25f);
						}
					}
				}
			}
		}
#endif
	}
#if UNITY_EDITOR
	[CustomEditor(typeof(PathfindingMesh))]
	public class NodeGridEditor : Editor {
		public override void OnInspectorGUI() {
			DrawDefaultInspector();
			PathfindingMesh builder = (PathfindingMesh)target;
			if(GUILayout.Button("Manual Save")) {
				builder.SaveGrid();
			}
			if(GUILayout.Button("Manual Load")) {
				builder.LoadGrid();
			}
			if(GUILayout.Button("Generate")) {
				builder.GenerateGrid();
			}
		}
	}
#endif
}