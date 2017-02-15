using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine.AssetBundles.GraphTool;
using V1=AssetBundleGraph;

namespace UnityEngine.AssetBundles.GraphTool.DataModel.Version2 {

	/*
	 * Save data which holds all AssetBundleGraph settings and configurations.
	 */ 
	[CreateAssetMenu( fileName = "AssetBundleGraph", menuName = "AssetBundle Graph", order = 650 )]
	public class ConfigGraph : ScriptableObject {

		/*
		 * Important: 
		 * ABG_FILE_VERSION must be increased by one when any structure change(s) happen
		 */ 
		public const int ABG_FILE_VERSION = 2;

		[SerializeField] private List<NodeData> m_allNodes;
		[SerializeField] private List<ConnectionData> m_allConnections;
		[SerializeField] private string m_lastModified;
		[SerializeField] private int m_version;

		void OnEnable() {
			Initialize();
			Validate();
		}

		private string GetFileTimeUtcString() {
			return DateTime.UtcNow.ToFileTimeUtc().ToString();
		}

		private void Initialize() {
			if(string.IsNullOrEmpty(m_lastModified)) {
				m_lastModified = GetFileTimeUtcString();
				m_allNodes = new List<NodeData>();
				m_allConnections = new List<ConnectionData>();
				m_version = ABG_FILE_VERSION;
				EditorUtility.SetDirty(this);
			}
		}

		private void Import(V1.SaveData v1) {
			m_lastModified = GetFileTimeUtcString();
			m_version = ABG_FILE_VERSION;

			foreach(var n in v1.Nodes) {
				m_allNodes.Add(new NodeData(n));
			}

			foreach(var c in v1.Connections) {
				m_allConnections.Add(new ConnectionData(c));
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

		public void Save() {
			m_allNodes.ForEach(n => n.Operation.Save());
			SetGraphDirty();
		}

		public void SetGraphDirty() {
			EditorUtility.SetDirty(this);
		}
			
		//
		// Save/Load to disk
		//

		private static string SaveDataDirectoryPath {
			get {
				return FileUtility.PathCombine(Application.dataPath, Settings.ASSETNBUNDLEGRAPH_DATA_PATH);
			}
		}

		private static string DefaultSaveDataAssetPath {
			get {
				return FileUtility.PathCombine("Assets/", Settings.ASSETNBUNDLEGRAPH_DATA_PATH, Settings.ASSETBUNDLEGRAPH_DATA_NAME);
			}
		}

		public static ConfigGraph GetDefaultGraph() {
			// while AssetDatabase.CreateAsset() invokes OnPostprocessAllAssets where
			// SaveData.Data is used through AssetReferenceDatabasePostprocessor,
			// s_saveData must be set carefully in right order inside LoadFromDisk()
			// so setting s_saveData is handled inside LoadFromDisk()
			return LoadDefaultDataFromDisk();
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
				Save();
			} else {
				LogUtility.Logger.Log("[ApplyGraph] SaveData update skipped. graph is equivarent.");
			}
		}

		public static bool IsSaveDataAvailableAtDisk() {
			return File.Exists(DefaultSaveDataAssetPath);
		}

		private static ConfigGraph CreateNewSaveData () {

			var data = ScriptableObject.CreateInstance<ConfigGraph>();

			return data;
		}
			
		private static ConfigGraph LoadDefaultDataFromDisk() {

			// First, try loading from asset.
			try {
				var path = DefaultSaveDataAssetPath;

				if(File.Exists(path)) 
				{
					ConfigGraph data = AssetDatabase.LoadAssetAtPath<ConfigGraph>(path);

					if(data != null) {
						if(data.m_version > ABG_FILE_VERSION) {
							LogUtility.Logger.LogFormat(LogType.Warning, "AssetBundleGraph Savedata on disk is too new(our version:{0} config version:{1}). Saving project may cause data loss.", 
								ABG_FILE_VERSION, data.m_version);
						}

						data.Validate();
						return data;
					}
				}
			} catch(Exception e) {
				LogUtility.Logger.LogWarning(LogUtility.kTag, e);
			}

			// If there is no asset found, try load from v1.
			try {
				V1.SaveData v1 = V1.SaveData.Data;

				var graph = CreateNewSaveData ();
				AssetDatabase.CreateAsset(graph, DefaultSaveDataAssetPath);
				graph.Import(v1);
				return graph;

			} catch (Exception e) {
				LogUtility.Logger.LogError(LogUtility.kTag, "Failed to import settings from version 1." + e );
			}

			var newgraph = CreateNewSaveData ();
			AssetDatabase.CreateAsset(newgraph, DefaultSaveDataAssetPath);
			return newgraph;
		}

		/*
		 * Checks deserialized SaveData, and make some changes if necessary
		 * return false if any changes are perfomed.
		 */
		private bool Validate () {
			var changed = false;

			if(m_allNodes != null) {
				List<NodeData> removingNodes = new List<NodeData>();
				foreach (var n in m_allNodes) {
					if(!n.Validate()) {
						removingNodes.Add(n);
						changed = true;
					}
				}
				m_allNodes.RemoveAll(n => removingNodes.Contains(n));
			}

			if(m_allConnections != null) {
				List<ConnectionData> removingConnections = new List<ConnectionData>();
				foreach (var c in m_allConnections) {
					if(!c.Validate(m_allNodes, m_allConnections)) {
						removingConnections.Add(c);
						changed = true;
					}
				}
				m_allConnections.RemoveAll(c => removingConnections.Contains(c));
			}

			if(changed) {
				m_lastModified = GetFileTimeUtcString();
			}

			return !changed;
		}
	}
}