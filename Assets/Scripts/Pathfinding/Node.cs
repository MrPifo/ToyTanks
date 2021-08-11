using System;
using UnityEngine;
using Sperlich.Types;
using System.Collections.Generic;

namespace Sperlich.Pathfinding {

	[Serializable]
	public class Node : IComparable<Node> {

		public enum NodeType { undefined, ground, wall }
		
		public Float3 pos;
		public NodeType type;
		public float gCost;
		public float hCost;
		public float dist;
		public float FCost => gCost + hCost;
		[field: NonSerialized] public Node Parent { get; set; }
		[field: NonSerialized] public PathfindingMesh Grid { get; set; }
		[field: NonSerialized] public Dictionary<Node, float> Neighbours { get; set; } = new Dictionary<Node, float>();

		public Node() {
			Neighbours = new Dictionary<Node, float>();
			Grid = null;
			pos = new Float3();
			type = NodeType.undefined;
		}

		public Node(PathfindingMesh grid, Float3 pos, NodeType typ = NodeType.undefined, float distance = 0) {
			Grid = grid;
			this.pos = pos;
			dist = distance;
			if(typ != NodeType.undefined) {
				type = typ;
			} else {
				type = NodeType.ground;
			}
		}

		public void FetchNeighbours(float distance) {
			if(Neighbours == null || Grid == null) Neighbours = new Dictionary<Node, float>();

			foreach(Node n in Grid.Nodes) {
				float dist = GetDistance(n);
				if(dist <= distance && n != this) {
					if(!n.Neighbours.ContainsKey(this)) {
						n.Neighbours.Add(this, dist);
					} else if(!Neighbours.ContainsKey(n)) {
						Neighbours.Add(n, dist);
					}
				}
			}
		}

		public float GetDistance(Node nodeB) {
			if(Neighbours.ContainsKey(nodeB)) {
				return Neighbours[nodeB];
			}
			return Vector3.Distance(pos, nodeB.pos);
		}

		public int CompareTo(Node n) {
			if(n.FCost > FCost) return -1;
			if(n.FCost < FCost) return 1;
			return 0;
		}

		public override string ToString() {
			return "Position: " + pos + "\n" + type + "\n G-Cost: " + gCost + "\n H-Cost: " + hCost;
		}
	}
}