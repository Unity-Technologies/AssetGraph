using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AssetBundleGraph {

	public class AssetReferenceStreamManager {

		// key: connectiondata id
		private Dictionary<string, Dictionary<string, List<AssetReference>>> m_connectionStreamMap;

		// key: nodedata id
		private Dictionary<string, Dictionary<string, List<AssetReference>>> m_leafnodeOutput;

		// key: nodedata id
		private Dictionary<string, Dictionary<string, List<AssetReference>>> m_nodeStreamCache;

		public AssetReferenceStreamManager() {
			m_connectionStreamMap = new Dictionary<string, Dictionary<string, List<AssetReference>>>();
			m_nodeStreamCache = new Dictionary<string, Dictionary<string, List<AssetReference>>>();
			m_leafnodeOutput = new Dictionary<string, Dictionary<string, List<AssetReference>>>();
		}

		public IEnumerable<Dictionary<string, List<AssetReference>>> EnumurateIncomingAssetGroups(ConnectionPointData inputPoint) {
			UnityEngine.Assertions.Assert.IsNotNull(inputPoint);
			UnityEngine.Assertions.Assert.IsTrue (inputPoint.IsInput);

			var connections = SaveData.Data.Connections;

			return m_connectionStreamMap.Where(v => { 
				var conn = connections.Find(c => c.Id == v.Key);
				return conn!= null && conn.ToNodeConnectionPointId == inputPoint.Id;
			}).Select(v => v.Value);
		}

		public Dictionary<string, List<AssetReference>> FindAssetGroup(string connectionId) {

			if (!m_connectionStreamMap.ContainsKey(connectionId)) {
				m_connectionStreamMap[connectionId] = new Dictionary<string, List<AssetReference>>();
			}

			return m_connectionStreamMap[connectionId];
		}

		public Dictionary<string, List<AssetReference>> FindAssetGroup(ConnectionData connection) {
			if (!m_connectionStreamMap.ContainsKey(connection.Id)) {
				m_connectionStreamMap[connection.Id] = new Dictionary<string, List<AssetReference>>();
			}

			return m_connectionStreamMap[connection.Id];
		}

		public void AssignAssetGroup(ConnectionData connection, Dictionary<string, List<AssetReference>> groups) {
			m_connectionStreamMap[connection.Id] = groups;
		}

		public void RemoveAssetGroup(ConnectionData connection) {
			if (m_connectionStreamMap.ContainsKey(connection.Id)) { 
				m_connectionStreamMap.Remove(connection.Id);
			}
		}

		public void ClearLeafAssetGroupOutout(NodeData node) {
			if( m_leafnodeOutput.ContainsKey(node.Id) ) {
				m_leafnodeOutput[node.Id].Clear();
			}
		}

		public void AppendLeafnodeAssetGroupOutout(NodeData node, Dictionary<string, List<AssetReference>> groups) {

			if(!m_leafnodeOutput.ContainsKey(node.Id)) {
				m_leafnodeOutput[node.Id] = new Dictionary<string, List<AssetReference>>();
			}

			var g = m_leafnodeOutput[node.Id];

			foreach(var k in groups.Keys) {
				if(!g.ContainsKey(k)) {
					g[k] = new List<AssetReference>();
				}
				g[k].AddRange(groups[k]);
			}
		}

//		public Dictionary<string, List<AssetReference>> FindAssetGroup(NodeData node) {
//
//			if(!m_nodeStreamCache.ContainsKey(node.Id)) {
//				m_nodeStreamCache[node.Id] = new Dictionary<string, List<AssetReference>>();
//			}
//
//			// TODO: fix this
//			//return m_connectionStreamMap[node];
//
//			return m_nodeStreamCache[node.Id];
//
////			Dictionary<string, List<AssetReference>> group = new Dictionary<string, List<AssetReference>>();
//
////			foreach (var c in m_connectionStreamMap.Keys) {
////				if(c.FromNodeId != node.Id) {
////					continue;
////				}
////				var targetNode = data.Nodes.Find(node => node.Id == c.FromNodeId);
////				var groupDict = result[c];
////
////				if (!nodeDatas.ContainsKey(targetNode)) {
////					nodeDatas[targetNode] = new Dictionary<string, List<AssetReference>>();
////				}
////				foreach (var groupKey in groupDict.Keys) {
////					if (!nodeDatas[targetNode].ContainsKey(groupKey)) {
////						nodeDatas[targetNode][groupKey] = new List<AssetReference>();
////					}
////					nodeDatas[targetNode][groupKey].AddRange(groupDict[groupKey]);
////				}
////			}
//		}

	}
}
