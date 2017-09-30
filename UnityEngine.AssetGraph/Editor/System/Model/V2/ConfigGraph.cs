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
		[SerializeField] private string m_graphDescription;
		[SerializeField] private bool m_useAsAssetPostprocessor;

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
				m_graphDescription = String.Empty;
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

		public bool UseAsAssetPostprocessor {
			get {  
				return m_useAsAssetPostprocessor;
			}
			set {
				m_useAsAssetPostprocessor = value;
				SetGraphDirty();
			}
		}

		public DateTime LastModified {
			get {
				long utcFileTime = long.Parse(m_lastModified);
				DateTime d = DateTime.FromFileTimeUtc(utcFileTime);

				return d;
			}
		}

		public string Descrption {
			get{
				return m_graphDescription;
			}
			set {
				m_graphDescription = value;
				SetGraphDirty();
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

		public static bool IsImportableDataAvailableAtDisk() {
			return V1.SaveData.IsSaveDataAvailableAtDisk();
		}

		public static ConfigGraph CreateNewGraph(string pathToSave) {
			var data = ScriptableObject.CreateInstance<ConfigGraph>();
			AssetDatabase.CreateAsset(data, pathToSave);
			return data;
		}

		public static ConfigGraph CreateNewGraphFromImport(string pathToSave) {

			// try load from v1.
			try {
				V1.SaveData v1 = V1.SaveData.Data;
				ConfigGraph newGraph = CreateNewGraph(pathToSave);
				newGraph.Import(v1);

				return newGraph;

			} catch (Exception e) {
				LogUtility.Logger.LogError(LogUtility.kTag, "Failed to import graph from previous version." + e );
			}

			return null;
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