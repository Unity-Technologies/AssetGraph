using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AssetBundleGraph {

	public class AssetReferenceStreamManager {

		private Dictionary<ConnectionData, Dictionary<string, List<AssetReference>>> m_connectionStreamMap;
		private Dictionary<NodeData, Dictionary<string, List<AssetReference>>> m_leafnodeOutput;
		private Dictionary<NodeData, Dictionary<string, List<AssetReference>>> m_nodeStreamCache;

		public AssetReferenceStreamManager() {
			m_connectionStreamMap = new Dictionary<ConnectionData, Dictionary<string, List<AssetReference>>>();
			m_nodeStreamCache = new Dictionary<NodeData, Dictionary<string, List<AssetReference>>>();
			m_leafnodeOutput = new Dictionary<NodeData, Dictionary<string, List<AssetReference>>>();
		}

		public Dictionary<string, List<AssetReference>> GetIncomingAssetGroups(ConnectionPointData inputPoint) {
			UnityEngine.Assertions.Assert.IsNotNull(inputPoint);
			UnityEngine.Assertions.Assert.IsTrue (inputPoint.IsInput);

			var keyEnum = m_connectionStreamMap.Keys.Where(c => c.ToNodeConnectionPointId == inputPoint.Id);
			if (keyEnum.Any()) { 
				return m_connectionStreamMap[keyEnum.First()];
			}
			return null;
		}

		public Dictionary<string, List<AssetReference>> FindAssetGroup(string connectionId) {

			var keyEnum = m_connectionStreamMap.Keys.Where(c => c.Id == connectionId);
			if (keyEnum.Any()) { 
				return m_connectionStreamMap[keyEnum.First()];
			} else {
				return new Dictionary<string, List<AssetReference>>();
			}
		}

		public Dictionary<string, List<AssetReference>> FindAssetGroup(ConnectionData connection) {
			if(!m_connectionStreamMap.ContainsKey(connection)) {
				m_connectionStreamMap[connection] = new Dictionary<string, List<AssetReference>>();
			}
			return m_connectionStreamMap[connection];
		}

		public void AssignAssetGroup(ConnectionData connection, Dictionary<string, List<AssetReference>> groups) {
			m_connectionStreamMap[connection] = groups;
		}

		public void ClearLeafAssetGroupOutout(NodeData node) {
			if(m_leafnodeOutput.ContainsKey(node)) {
				m_leafnodeOutput[node].Clear();
			}
		}

		public void AppendLeafnodeAssetGroupOutout(NodeData node, Dictionary<string, List<AssetReference>> groups) {

			if(!m_leafnodeOutput.ContainsKey(node)) {
				m_leafnodeOutput[node] = new Dictionary<string, List<AssetReference>>();
			}

			var g = m_leafnodeOutput[node];

			foreach(var k in groups.Keys) {
				g[k].AddRange(groups[k]);
			}
		}

		public Dictionary<string, List<AssetReference>> FindAssetGroup(NodeData node) {

			if(!m_nodeStreamCache.ContainsKey(node)) {
				m_nodeStreamCache[node] = new Dictionary<string, List<AssetReference>>();
			}
			// TODO fix this
			//return m_connectionStreamMap[node];


			return m_nodeStreamCache[node];

//			Dictionary<string, List<AssetReference>> group = new Dictionary<string, List<AssetReference>>();

//			foreach (var c in m_connectionStreamMap.Keys) {
//				if(c.FromNodeId != node.Id) {
//					continue;
//				}
//				var targetNode = data.Nodes.Find(node => node.Id == c.FromNodeId);
//				var groupDict = result[c];
//
//				if (!nodeDatas.ContainsKey(targetNode)) {
//					nodeDatas[targetNode] = new Dictionary<string, List<AssetReference>>();
//				}
//				foreach (var groupKey in groupDict.Keys) {
//					if (!nodeDatas[targetNode].ContainsKey(groupKey)) {
//						nodeDatas[targetNode][groupKey] = new List<AssetReference>();
//					}
//					nodeDatas[targetNode][groupKey].AddRange(groupDict[groupKey]);
//				}
//			}
		}

	}
}
