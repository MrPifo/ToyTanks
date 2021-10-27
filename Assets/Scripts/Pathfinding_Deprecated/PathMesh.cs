using System;
using System.Collections.Generic;

namespace Sperlich.Pathfinding {

	[Serializable]
	public class PathMesh {
		public string name;
		public List<Node> nodes;

		public PathMesh() {
			name = "untitled";
			nodes = new List<Node>();
		}
		public PathMesh(string name) {
			this.name = name;
			nodes = new List<Node>();
		}
		public PathMesh(string name, List<Node> nodes) {
			this.name = name;
			this.nodes = nodes;
		}
	}
}
