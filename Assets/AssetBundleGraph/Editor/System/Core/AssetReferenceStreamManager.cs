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

		public AssetReferenceStreamManager() {
			m_connectionStreamMap = new Dictionary<string, Dictionary<string, List<AssetReference>>>();
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
	}
}
