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
		public const string BUNDLECONFIG_BUNDLENAME_TEMPLATE_DEFAULT = "*";

		// by default, AssetBundleGraph's node has only 1 InputPoint. and 
		// this is only one definition of it's label.
		public const string DEFAULT_INPUTPOINT_LABEL = "-";
		public const string DEFAULT_OUTPUTPOINT_LABEL = "+";
		public const string BUNDLECONFIG_BUNDLE_OUTPUTPOINT_LABEL = "bundles";
		public const string BUNDLECONFIG_VARIANTNAME_DEFAULT = "";

		public const string DEFAULT_FILTER_KEYWORD = "keyword";
		public const string DEFAULT_FILTER_KEYTYPE = "Any";

		public const string FILTER_KEYWORD_WILDCARD = "*";

		public const string NODE_INPUTPOINT_FIXED_LABEL = "FIXED_INPUTPOINT_ID";
	}

	/*
	 * Save data which holds all AssetBundleGraph settings and configurations.
	 */ 
	public class SaveData : ScriptableObject {

		public const string LASTMODIFIED 	= "lastModified";
		public const string NODES 			= "nodes";
		public const string CONNECTIONS 	= "connections";
		public const string VERSION 		= "version";

		/*
		 * Important: 
		 * ABG_FILE_VERSION must be increased by one when any structure change(s) happen
		 */ 
		public const int ABG_FILE_VERSION = 1;

		[SerializeField] private List<NodeData> m_allNodes;
		[SerializeField] private List<ConnectionData> m_allConnections;
		[SerializeField] private string m_lastModified;
		[SerializeField] private int m_version;

		private static SaveData s_saveData;

		void OnEnable() {
		}

		private string GetFileTimeUtcString() {
			return DateTime.UtcNow.ToFileTimeUtc().ToString();
		}

		private void Initialize() {
			m_lastModified = GetFileTimeUtcString();
			m_allNodes = new List<NodeData>();
			m_allConnections = new List<ConnectionData>();
			m_version = ABG_FILE_VERSION;
			EditorUtility.SetDirty(this);
		}

		private void InitializeFromJson(Dictionary<string, object> jsonData) {
			m_allNodes = new List<NodeData>();
			m_allConnections = new List<ConnectionData>();
			m_lastModified = jsonData[LASTMODIFIED] as string;
			m_version = ABG_FILE_VERSION;

			var nodeList = jsonData[NODES] as List<object>;
			var connList = jsonData[CONNECTIONS] as List<object>;

			foreach(var n in nodeList) {
				m_allNodes.Add(new NodeData(n as Dictionary<string, object>));
			}

			foreach(var c in connList) {
				m_allConnections.Add(new ConnectionData(c as Dictionary<string, object>));
			}
			EditorUtility.SetDirty(this);
		}

		public string LastModified {
			get {
				return m_lastModified;
			}
		}

		public int Version {
			get {
				return m_version;
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
				{VERSION, m_version},
				{LASTMODIFIED, m_lastModified},
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

		private static string SaveDataAssetPath {
			get {
				return FileUtility.PathCombine("Assets/", AssetBundleGraphSettings.ASSETNBUNDLEGRAPH_DATA_PATH, AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_NAME);
			}
		}

		private static string SaveDataJsonPath {
			get {
				return FileUtility.PathCombine(SaveDataDirectoryPath, AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_JSON_NAME);
			}
		}

		public static SaveData Data {
			get {
				if(s_saveData == null) {
					// while AssetDatabase.CreateAsset() invokes OnPostprocessAllAssets where
					// SaveData.Data is used through AssetReferenceDatabasePostprocessor,
					// s_saveData must be set carefully in right order inside LoadFromDisk()
					// so setting s_saveData is handled inside LoadFromDisk()
					LoadFromDisk();
				}
				return s_saveData;
			}
		}

		public void ExportToJson () {

			LogUtility.Logger.Log("[SaveData] Saved to Disk.");

			var dir = SaveDataDirectoryPath;
			if (!Directory.Exists(dir)) {
				Directory.CreateDirectory(dir);
			}

			m_version = ABG_FILE_VERSION;

			var dataStr = Json.Serialize(ToJsonDictionary());
			var prettified = Json.Prettify(dataStr);

			using (var sw = new StreamWriter(SaveDataJsonPath)) {
				sw.Write(prettified);
			}
			// reflect change of data.
			AssetDatabase.Refresh();
		}

		public static void SetSavedataDirty() {
			EditorUtility.SetDirty(Data);
		}


		public void ApplyGraph(List<NodeGUI> nodes, List<ConnectionGUI> connections) {

			List<NodeData> n = nodes.Select(v => v.Data).ToList();
			List<ConnectionData> c = connections.Select(v => v.Data).ToList();

			if( !Enumerable.SequenceEqual(n.OrderBy(v => v.Id), m_allNodes.OrderBy(v => v.Id)) ||
				!Enumerable.SequenceEqual(c.OrderBy(v => v.Id), m_allConnections.OrderBy(v => v.Id)) ) 
			{
				LogUtility.Logger.Log("[ApplyGraph] SaveData updated.");

				m_version = ABG_FILE_VERSION;
				m_lastModified = GetFileTimeUtcString();
				m_allNodes = n;
				m_allConnections = c;
				EditorUtility.SetDirty(this);
			} else {
				LogUtility.Logger.Log("[ApplyGraph] SaveData update skipped. graph is equivarent.");
			}
		}

		public static SaveData Reload() {
			s_saveData = null;
			return Data;
		}
			
		public static bool IsSaveDataAvailableAtDisk() {
			return File.Exists(SaveDataAssetPath) || File.Exists(SaveDataJsonPath);
		}

		private static void LoadJsonData() {
			var dataStr = string.Empty;
			using (var sr = new StreamReader(SaveDataJsonPath)) {
				dataStr = sr.ReadToEnd();
			}
			var deserialized = AssetBundleGraph.Json.Deserialize(dataStr) as Dictionary<string, object>;
			var data = ScriptableObject.CreateInstance<SaveData>();
			data.InitializeFromJson(deserialized);

			data.Validate();

			// s_saveData must be set before calling AssetDatabase.CreateAsset() (for OnPostprocessAllAssets())
			s_saveData = data;
			AssetDatabase.CreateAsset(data, SaveDataAssetPath);
		}

		private static void CreateNewSaveData () {

			var dir = SaveDataDirectoryPath;

			if (!Directory.Exists(dir)) {
				Directory.CreateDirectory(dir);
			}

			var data = ScriptableObject.CreateInstance<SaveData>();
			data.Initialize();

			data.Validate();

			// s_saveData must be set before calling AssetDatabase.CreateAsset() (for OnPostprocessAllAssets())
			s_saveData = data;
			AssetDatabase.CreateAsset(data, SaveDataAssetPath);
		}
			
		private static void LoadFromDisk() {

			// First, try loading from asset.
			try {
				var path = SaveDataAssetPath;

				if(File.Exists(path)) 
				{
					SaveData data = AssetDatabase.LoadAssetAtPath<SaveData>(path);

					if(data != null) {
						if(data.m_version > ABG_FILE_VERSION) {
							LogUtility.Logger.LogFormat(LogType.Warning, "AssetBundleGraph Savedata on disk is too new(our version:{0} config version:{1}). Saving project may cause data loss.", 
								ABG_FILE_VERSION, data.m_version);
						}

						data.Validate();
						s_saveData = data;
						return;
					}
				}
			} catch(Exception e) {
				LogUtility.Logger.LogWarning(LogUtility.kTag, e);
			}

			// If there is no asset found, look for json.
			try {
				if(File.Exists(SaveDataJsonPath)) {
					LoadJsonData();
					return;
				}
			} catch (Exception e) {
				LogUtility.Logger.LogError(LogUtility.kTag, "Failed to deserialize AssetBundleGraph settings. Error:" + e + " File:" + SaveDataJsonPath);
			}

			CreateNewSaveData ();
		}

		/*
		 * Checks deserialized SaveData, and make some changes if necessary
		 * return false if any changes are perfomed.
		 */
		private bool Validate () {
			var changed = false;

			List<NodeData> removingNodes = new List<NodeData>();
			List<ConnectionData> removingConnections = new List<ConnectionData>();

			/*
				delete undetectable node.
			*/
			foreach (var n in m_allNodes) {
				if(!n.Validate(m_allNodes, m_allConnections)) {
					removingNodes.Add(n);
					changed = true;
				}
			}

			foreach (var c in m_allConnections) {
				if(!c.Validate(m_allNodes, m_allConnections)) {
					removingConnections.Add(c);
					changed = true;
				}
			}

			if(changed) {
				Nodes.RemoveAll(n => removingNodes.Contains(n));
				Connections.RemoveAll(c => removingConnections.Contains(c));
				m_lastModified = GetFileTimeUtcString();
			}

			return !changed;
		}
	}
}