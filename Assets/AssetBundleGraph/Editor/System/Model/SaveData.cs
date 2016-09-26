using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AssetBundleGraph {

	class SaveDataConstants {
		/*
			data key for AssetBundleGraph.json
		*/

		public const string GROUPING_KEYWORD_DEFAULT = "/Group_*/";
		public const string BUNDLIZER_BUNDLENAME_TEMPLATE_DEFAULT = "bundle_*.assetbundle";

		// by default, AssetBundleGraph's node has only 1 InputPoint. and 
		// this is only one definition of it's label.
		public const string DEFAULT_INPUTPOINT_LABEL = "-";
		public const string DEFAULT_OUTPUTPOINT_LABEL = "+";
		public const string BUNDLIZER_BUNDLE_OUTPUTPOINT_LABEL = "bundles";
		public const string BUNDLIZER_VARIANTNAME_DEFAULT = "";

		public const string BUNDLIZER_FAKE_CONNECTION_ID = "b_______-____-____-____-____________";

		public const string DEFAULT_FILTER_KEYWORD = "keyword";
		public const string DEFAULT_FILTER_KEYTYPE = "Any";

		public const string FILTER_KEYWORD_WILDCARD = "*";
		public const string FILTER_FAKE_CONNECTION_ID = "f_______-____-____-____-____________";

		public const string NODE_INPUTPOINT_FIXED_LABEL = "FIXED_INPUTPOINT_ID";
	}

	/*
	 * Json save data which holds all AssetBundleGraph settings and configurations.
	 */ 
	public class SaveData {

		public const string LASTMODIFIED 	= "lastModified";
		public const string NODES 			= "nodes";
		public const string CONNECTIONS 	= "connections";

		private Dictionary<string, object> m_jsonData;

		private List<NodeData> m_allNodes;
		private List<ConnectionData> m_allConnections;
		private DateTime m_lastModified;

		public SaveData() {
			m_lastModified = DateTime.UtcNow;
			m_allNodes = new List<NodeData>();
			m_allConnections = new List<ConnectionData>();
		}

		public SaveData(Dictionary<string, object> jsonData) {
			m_jsonData = jsonData;
			m_allNodes = new List<NodeData>();
			m_allConnections = new List<ConnectionData>();

			m_lastModified = Convert.ToDateTime(m_jsonData[LASTMODIFIED] as string);

			var nodeList = m_jsonData[NODES] as List<object>;
			var connList = m_jsonData[CONNECTIONS] as List<object>;

			foreach(var n in nodeList) {
				m_allNodes.Add(new NodeData(n as Dictionary<string, object>));
			}

			foreach(var c in connList) {
				m_allConnections.Add(new ConnectionData(c as Dictionary<string, object>));
			}

			AddConnectionsToNodes();
		}

		public SaveData(List<NodeGUI> nodes, List<ConnectionGUI> connections) {
			m_jsonData = null;

			m_lastModified = DateTime.UtcNow;
			m_allNodes = nodes.Select(n => n.Data).ToList();
			m_allConnections = new List<ConnectionData>();

			foreach(var cgui in connections) {
				m_allConnections.Add(new ConnectionData(cgui));
			}

			AddConnectionsToNodes();
		}

		private void AddConnectionsToNodes() {
			/*
				adding parentNode to childNode for run up relationship from childNode.
			*/
			foreach (var connection in m_allConnections) {
				var targetNodes = m_allNodes.Where(node => node.Id == connection.ToNodeId).ToList();
				foreach (var targetNode in targetNodes) {
					targetNode.AddConnectionToParent(connection);
				}
			}

		}

		public DateTime LastModified {
			get {
				return m_lastModified;
			}
		}

		public List<NodeData> Nodes {
			get{ 
				return m_allNodes;
			}
		}

		public List<ConnectionData> Connections {
			get{ 
				return m_allConnections;
			}
		}

		private Dictionary<string, object> ToJsonDictionary() {

			var nodeList = new List<Dictionary<string, object>>();
			var connList = new List<Dictionary<string, object>>();

			foreach(NodeData n in m_allNodes) {
				nodeList.Add(n.ToJsonDictionary());
			}

			foreach(ConnectionData c in m_allConnections) {
				connList.Add(c.ToJsonDictionary());
			}

			return new Dictionary<string, object>{
				{LASTMODIFIED, m_lastModified.ToString()},
				{NODES, nodeList},
				{CONNECTIONS, connList}
			};
		}

		public List<NodeData> CollectAllLeafNodes() {

			var nodesWithChild = new List<NodeData>();
			foreach (var c in m_allConnections) {
				NodeData n = m_allNodes.Find(v => v.Id == c.FromNodeId);
				if(n != null) {
					nodesWithChild.Add(n);
				}
			}
			return m_allNodes.Except(nodesWithChild).ToList();
		}

		//
		// Save/Load to disk
		//

		private static string SaveDataDirectoryPath {
			get {
				return FileUtility.PathCombine(Application.dataPath, AssetBundleGraphSettings.ASSETNBUNDLEGRAPH_DATA_PATH);
			}
		}

		private static string SaveDataPath {
			get {
				return FileUtility.PathCombine(SaveDataDirectoryPath, AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_NAME);
			}
		}

		public void Save () {
			var dir = SaveDataDirectoryPath;
			if (!Directory.Exists(dir)) {
				Directory.CreateDirectory(dir);
			}

			m_lastModified = DateTime.UtcNow;

			var dataStr = Json.Serialize(ToJsonDictionary());
			var prettified = Json.Prettify(dataStr);

			using (var sw = new StreamWriter(SaveDataPath)) {
				sw.Write(prettified);
			}
			// reflect change of data.
			AssetDatabase.Refresh();
		}

		public static bool IsSaveDataAvailableAtDisk() {
			return File.Exists(SaveDataPath);
		}

		private static SaveData Load() {
			var dataStr = string.Empty;
			using (var sr = new StreamReader(SaveDataPath)) {
				dataStr = sr.ReadToEnd();
			}
			var deserialized = AssetBundleGraph.Json.Deserialize(dataStr) as Dictionary<string, object>;
			return new SaveData(deserialized);
		}

		public static SaveData RecreateDataOnDisk () {
			SaveData newSaveData = new SaveData();
			newSaveData.Save();
			return newSaveData;
		}
			
		public static SaveData LoadFromDisk() {

			if(!IsSaveDataAvailableAtDisk()) {
				return RecreateDataOnDisk ();
			} 

			try {
				SaveData saveData = Load();
				if(!saveData.Validate()) {
					saveData.Save();

					// reload and construct again from disk
					return Load();
				} 
				else {
					return saveData;
				}
			} catch (Exception e) {
				Debug.LogError("Failed to deserialize AssetBundleGraph settings. Error:" + e + " File:" + SaveDataPath);
			}

			return new SaveData();
		}

		/*
		 * Checks deserialized SaveData, and make some changes if necessary
		 * return false if any changes are perfomed.
		 */
		public bool Validate () {
			var changed = false;

			List<NodeData> removingNodes = new List<NodeData>();
			List<ConnectionData> removingConnections = new List<ConnectionData>();

			/*
				delete undetectable node.
			*/
			foreach (var n in m_allNodes) {
				if(!n.Validate()) {
					removingNodes.Add(n);
					changed = true;
				}
			}

			foreach (var c in m_allConnections) {
				if(!c.Validate()) {
					removingConnections.Add(c);
					changed = true;
				}
			}

			if(changed) {
				Nodes.RemoveAll(n => removingNodes.Contains(n));
				Connections.RemoveAll(c => removingConnections.Contains(c));
				m_lastModified = DateTime.UtcNow;
			}

			return !changed;
		}
	}
}