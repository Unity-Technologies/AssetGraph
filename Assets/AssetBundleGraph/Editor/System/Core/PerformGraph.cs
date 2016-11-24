using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AssetBundleGraph {
	public class PerformGraph {

		public delegate void Output(Dictionary<string, List<AssetReference>> outputGroupAsset);
		public delegate void Perform(NodeData data, ConnectionData src, ConnectionData dst, Dictionary<string, List<AssetReference>> inputGroups, Output outputFunc);

		public class Node {
			public NodeData data;
			public List<AssetStream> streamFrom;
			public List<AssetStream> streamTo;
			public bool dirty;

			public Node(NodeData d) {
				data = d;
				streamFrom = new List<AssetStream>();
				streamTo = new List<AssetStream>();
				dirty = true;
			}

			public override bool Equals(object rhs)
			{
				Node other = rhs as Node; 
				if (other == null) {
					return false;
				} else {
					return other == this;
				}
			}

			public override int GetHashCode()
			{
				return this.data.Id.GetHashCode(); 
			}

			public static bool operator == (Node lhs, Node rhs) {

				object lobj = lhs;
				object robj = rhs;

				if(lobj == null && robj == null) {
					return true;
				}
				if(lobj == null || robj == null) {
					return false;
				}

				return lhs.data.Id == rhs.data.Id;
			}

			public static bool operator != (Node lhs, Node rhs) {
				return !(lhs == rhs);
			}
		}

		public class AssetStream {
			public ConnectionData connection;
			public Node nodeFrom;
			public Node nodeTo;
			public Dictionary<string, List<AssetReference>> assetGroups;
			private Dictionary<string, List<AssetReference>> output;

			public AssetStream(ConnectionData c, Node f, Node t, AssetReferenceStreamManager m) {
				connection = c;
				nodeFrom = f;
				nodeTo = t;
				assetGroups = m.FindAssetGroup(c);
				output = null;
			}

			public void AddNewOutput(Dictionary<string, List<AssetReference>> o) {
				if(output == null) {
					output = new Dictionary<string, List<AssetReference>>();
				}

				foreach(var v in o) {
					if(!output.ContainsKey(v.Key)) {
						output[v.Key] = v.Value;
					} else {
						output[v.Key].AddRange(v.Value);
					}
				}
			}

			public bool IsStreamAssetRequireUpdate {
				get {
					if(output == null) {
						return false;
					}

					if(!output.Keys.SequenceEqual(assetGroups.Keys)) {
						return true;
					}

					foreach(var k in output.Keys) {
						if(!output[k].SequenceEqual(assetGroups[k])) {
							return true;
						}
					}

					return false;
				}
			}

			public void UpdateAssetGroup(AssetReferenceStreamManager m) {

				UnityEngine.Assertions.Assert.IsNotNull(output);

				nodeTo.dirty = true;
				Debug.LogFormat("{0} marked dirty ({1} => {2} updated)", nodeTo.data.Name, nodeFrom.data.Name, nodeTo.data.Name);
				assetGroups = output;
				m.AssignAssetGroup(connection, output);
				output = null;
			}

			public override bool Equals(object rhs)
			{
				AssetStream other = rhs as AssetStream; 
				if (other == null) {
					return false;
				} else {
					return other == this;
				}
			}

			public override int GetHashCode()
			{
				return this.connection.Id.GetHashCode(); 
			}

			public static bool operator == (AssetStream lhs, AssetStream rhs) {

				object lobj = lhs;
				object robj = rhs;

				if(lobj == null && robj == null) {
					return true;
				}
				if(lobj == null || robj == null) {
					return false;
				}

				return lhs.connection.Id == rhs.connection.Id;
			}

			public static bool operator != (AssetStream lhs, AssetStream rhs) {
				return !(lhs == rhs);
			}
		}

		private AssetReferenceStreamManager m_streamManager;
		private SaveData  m_saveData;
		private List<Node> m_nodes;
		private List<AssetStream> m_streams;
		private Dictionary<string, List<AssetReference>> m_emptyAssetGroup;
		private BuildTarget m_target;

		public PerformGraph(AssetReferenceStreamManager mgr) {
			m_nodes = new List<Node>();
			m_streams = new List<AssetStream>();
			m_streamManager = mgr;
			m_emptyAssetGroup = new Dictionary<string, List<AssetReference>>();
			m_target = (BuildTarget)int.MaxValue;
		}

		public void BuildGraphFromSaveData(SaveData saveData, BuildTarget target, PerformGraph old) {
			m_saveData = saveData;
			m_target = target;

			ValidateLoopConnection(m_saveData);

			m_nodes.Clear();
			m_streams.Clear();

			foreach (var n in saveData.Nodes) {
				SetupNode(n);
			}

			foreach (var c in saveData.Connections) {
				SetupStream(c);
			}

			/*
			 * All nodes needs revisit when target has changed.
			 * Do modification check only when targeting the same build target 
			 * from last one.
			*/
			if(m_target == old.m_target) {
				CompareAndMarkModified(old);
			}
		}

		private void SetupNode (NodeData node) {
			Node n = new Node(node);
			m_nodes.Add(n);
		}

		private void SetupStream (ConnectionData conn) {

			Node fromNode = m_nodes.Find(n => n.data.Id == conn.FromNodeId);
			Node toNode = m_nodes.Find(n => n.data.Id == conn.ToNodeId);

			Assert.IsNotNull(fromNode);
			Assert.IsNotNull(toNode);

			AssetStream s = new AssetStream(conn, fromNode, toNode, m_streamManager);
			m_streams.Add(s);

			fromNode.streamTo.Add(s);
			toNode.streamFrom.Add(s);
		}

		private void CompareAndMarkModified(PerformGraph old) {

			foreach(var n in m_nodes) {
				n.dirty = false;

				if(old == null) {
					n.dirty = true;
					Debug.Log(n.data.Name + " mark modified.(old=null)");
				} else {
					Node oldNode = old.m_nodes.Find(x => x.data.Id == n.data.Id);
					// this is new node
					if(oldNode == null) {
						Debug.Log(n.data.Name + " mark modified.(oldnode null)");
						n.dirty = true;
					}
					else if(!n.data.CompareIgnoreGUIChanges(oldNode.data)) {
						n.dirty = true;
					}
				}
			}

			foreach(var s in m_streams) {
				if(old == null) {
				} else {
					AssetStream oldStream = old.m_streams.Find(x => s.connection.Id == x.connection.Id);
					if(oldStream == null) {
						s.nodeFrom.dirty = true;
						s.nodeTo.dirty = true;
					}
				}
			}

			var deletedStreams = old.m_streams.Except(m_streams);
			if(deletedStreams.Any()) {
				foreach(var deleted in deletedStreams) {
					var receiver = m_nodes.Find( n => n.data.Id == deleted.nodeTo.data.Id );
					if(receiver != null) {
						Debug.LogFormat("{0} input is removed. making it dirty...", receiver.data.Name);
						receiver.dirty = true;
					}
				}
			}
		}


		public void VisitAll(Perform performFunc, bool redoRoot = false) {
			List<Node> rootNodes = m_nodes.FindAll(n => n.streamFrom.Count == 0);

			if(redoRoot) {
				rootNodes.ForEach(n => n.dirty = true);
			}

			foreach(var n in rootNodes) {
				_Visit(n, performFunc);
			}
		}

		public void VisitFrom(NodeData node, Perform performFunc) {
			Node n = m_nodes.Find(x => x.data == node);
			Assert.IsNotNull(n);
			_Visit(n, performFunc);
		}

		private void _Visit(Node n, Perform performFunc) {

			if(n.dirty) {
				n.dirty = false;
				n.data.NeedsRevisit = false;

				if(n.streamTo.Count == 0) {
					m_streamManager.ClearLeafAssetGroupOutout(n.data);
				}

				//root node
				if(n.streamFrom.Count == 0) {
					for(int i = 0; i < n.streamTo.Count; ++i) {
						var output = n.streamTo[i];
						Debug.Log(n.data.Name + " performed(root)");
						performFunc(n.data, null, output.connection, m_emptyAssetGroup, 
							(Dictionary<string, List<AssetReference>> newOutput) => 
							{
								if(output.assetGroups != newOutput) {
									output.nodeTo.dirty = true;
									Debug.LogFormat("{0} marked dirty ({1} => {2} updated)", output.nodeTo.data.Name, output.nodeFrom.data.Name, output.nodeTo.data.Name);
									m_streamManager.AssignAssetGroup(output.connection, newOutput);
									output.assetGroups = newOutput;
								}
							}
						);
					}
				} else {
					for(int i = 0; i < n.streamFrom.Count; ++i) {
						var input = n.streamFrom[i];

						if(n.streamTo.Count > 0) {
							for(int j = 0; j < n.streamTo.Count; ++j) {
								var output = n.streamTo[j];
								Debug.LogFormat("{0} perfomed (from {1} input)", n.data.Name, input.nodeFrom.data.Name);
								performFunc(n.data,input.connection, output.connection, input.assetGroups, 
									(Dictionary<string, List<AssetReference>> newOutput) => 
									{
										output.AddNewOutput(newOutput);
									}
								);
							}
						} else {
							Debug.LogFormat("{0} perfomed (from {1} input)", n.data.Name, input.nodeFrom.data.Name);
							performFunc(n.data,input.connection, null, input.assetGroups, 
								(Dictionary<string, List<AssetReference>> newOutput) => 
								{
									m_streamManager.AppendLeafnodeAssetGroupOutout(n.data, newOutput);
								}
							);
						}
					}
					// Test output asset group after all input-output pairs are performed
					if(n.streamTo.Count > 0) {
						foreach(var to in n.streamTo) {
							if(to.IsStreamAssetRequireUpdate) {
								to.UpdateAssetGroup(m_streamManager);
							} else {
								Debug.LogFormat("[skipped]stream update skipped. Result is equivarent: {0} -> {1}", n.data.Name, to.nodeTo.data.Name);
							}
						}
					}
				}
			}

			foreach(var output in n.streamTo) {
				_Visit(output.nodeTo, performFunc);
			}
		}

		private static bool CompareAssetGroup(Dictionary<string, List<AssetReference>> lhs, Dictionary<string, List<AssetReference>> rhs) {

			if(lhs == null && rhs == null) {
				return true;
			}
			if(lhs == null || rhs == null) {
				return false;
			}

			if(lhs.Count != rhs.Count || rhs.Keys.Count != lhs.Keys.Count) {
				return false;
			}

			if( !lhs.Keys.Equals(rhs.Keys) ) {
				return false;
			}

			foreach(var k in lhs.Keys) {
				var lassets = lhs[k];
				var rassets = rhs[k];
				if(lassets.Count != rassets.Count || !lassets.Equals(rassets)) {
					return false;
				}
			}

			return true;
		}

		/*
		 * Verify nodes does not create cycle
		 */
		private void ValidateLoopConnection(SaveData saveData) {
			var leaf = saveData.CollectAllLeafNodes();
			foreach (var leafNode in leaf) {
				MarkAndTraverseParent(saveData, leafNode, new List<ConnectionData>(), new List<NodeData>());
			}
		}

		private void MarkAndTraverseParent(SaveData saveData, NodeData current, List<ConnectionData> visitedConnections, List<NodeData> visitedNode) {

			// if node is visited from other route, just quit
			if(visitedNode.Contains(current)) {
				return;
			}

			var connectionsToParents = saveData.Connections.FindAll(con => con.ToNodeId == current.Id);
			foreach(var c in connectionsToParents) {
				if(visitedConnections.Contains(c)) {
					throw new NodeException("Looped connection detected. Please fix connections to avoid loop.", current.Id);
				}

				var parentNode = saveData.Nodes.Find(node => node.Id == c.FromNodeId);
				UnityEngine.Assertions.Assert.IsNotNull(parentNode);

				visitedConnections.Add(c);
				MarkAndTraverseParent(saveData, parentNode, visitedConnections, visitedNode);
			}

			visitedNode.Add(current);
		}
	}
}
