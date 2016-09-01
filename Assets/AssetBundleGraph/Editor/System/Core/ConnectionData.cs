using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AssetBundleGraph {
	public class ConnectionData {
		public readonly string connectionId;
		public readonly string connectionLabel;
		public readonly string fromNodeId;
		public readonly string fromNodeOutputPointId;
		public readonly string toNodeId;
		public readonly string toNodeInputPointId;

		public ConnectionData (string connectionId, string connectionLabel, string fromNodeId, string fromNodeOutputPointId, string toNodeId, string toNodeInputPointId) {
			this.connectionId = connectionId;
			this.connectionLabel = connectionLabel;
			this.fromNodeId = fromNodeId;
			this.fromNodeOutputPointId = fromNodeOutputPointId;
			this.toNodeId = toNodeId;
			this.toNodeInputPointId = toNodeInputPointId;
		}

		public ConnectionData (ConnectionData connection) {
			this.connectionId = connection.connectionId;
			this.connectionLabel = connection.connectionLabel;
			this.fromNodeId = connection.fromNodeId;
			this.fromNodeOutputPointId = connection.fromNodeOutputPointId;
			this.toNodeId = connection.toNodeId;
			this.toNodeInputPointId = connection.toNodeInputPointId;
		}
	}
}
