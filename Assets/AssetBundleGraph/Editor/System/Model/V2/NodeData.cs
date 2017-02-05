using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

using UnityEngine.AssetBundles.GraphTool;

namespace UnityEngine.AssetBundles.GraphTool.DataModel.Version2 {

	[Serializable]
	public class FilterEntry {
		[SerializeField] private string m_filterKeyword;
		[SerializeField] private string m_filterKeytype;
		[SerializeField] private string m_pointId;

		public FilterEntry(string keyword, string keytype, ConnectionPointData point) {
			m_filterKeyword = keyword;
			m_filterKeytype = keytype;
			m_pointId = point.Id;
		}

		public FilterEntry(FilterEntry e) {
			m_filterKeyword = e.m_filterKeyword;
			m_filterKeytype = e.m_filterKeytype;
			m_pointId = e.m_pointId;
		}

		public string FilterKeyword {
			get {
				return m_filterKeyword;
			}
			set {
				m_filterKeyword = value;
			}
		}
		public string FilterKeytype {
			get {
				return m_filterKeytype; 
			}
			set {
				m_filterKeytype = value;
			}
		}
		public string ConnectionPointId {
			get {
				return m_pointId; 
			}
		}
		public string Hash {
			get {
				return m_filterKeyword+m_filterKeytype;
			}
		}
	}

	/*
	 * node data saved in/to Json
	 */
	[Serializable]
	public class NodeData {

		[SerializeField] private string m_name;
		[SerializeField] private string m_id;
		[SerializeField] private float m_x;
		[SerializeField] private float m_y;
		[SerializeField] private SerializedInstance<Node> m_nodeInstance;
		[SerializeField] private List<ConnectionPointData> m_inputPoints; 
		[SerializeField] private List<ConnectionPointData> m_outputPoints;

		private bool m_nodeNeedsRevisit;

		/*
		 * Properties
		 */ 

		public bool NeedsRevisit {
			get {
				return m_nodeNeedsRevisit;
			}
			set {
				m_nodeNeedsRevisit = value;
			}
		}

		public string Name {
			get {
				return m_name;
			}
			set {
				m_name = value;
			}
		}
		public string Id {
			get {
				return m_id;
			}
		}
		public SerializedInstance<Node> Operation {
			get {
				return m_nodeInstance;
			}
		}

		public float X {
			get {
				return m_x;
			}
			set {
				m_x = value;
			}
		}

		public float Y {
			get {
				return m_y;
			}
			set {
				m_y = value;
			}
		}

		public List<ConnectionPointData> InputPoints {
			get {
				return m_inputPoints;
			}
		}

		public List<ConnectionPointData> OutputPoints {
			get {
				return m_outputPoints;
			}
		}


		/*
		 * Constructor used to create new node from GUI
		 */ 
		public NodeData(string name, Node node, float x, float y) {

			m_id = Guid.NewGuid().ToString();
			m_name = name;
			m_x = x;
			m_y = y;
			m_nodeInstance = new SerializedInstance<Node>(node);
			m_nodeNeedsRevisit = false;

			m_inputPoints  = new List<ConnectionPointData>();
			m_outputPoints = new List<ConnectionPointData>();

			m_nodeInstance.Object.Initialize(this);
		}

		/**
		 * Duplicate this node with new guid.
		 */ 
		public NodeData Duplicate (bool keepId = false) {

			var newData = new NodeData(m_name, m_nodeInstance.Clone(), m_x, m_y);
			newData.m_nodeNeedsRevisit = false;

			if(keepId) {
				newData.m_id = m_id;
			}

			return newData;
		}

		public ConnectionPointData AddInputPoint(string label) {
			var p = new ConnectionPointData(label, this, true);
			m_inputPoints.Add(p);
			return p;
		}

		public ConnectionPointData AddOutputPoint(string label) {
			var p = new ConnectionPointData(label, this, false);
			m_outputPoints.Add(p);
			return p;
		}

		public ConnectionPointData FindInputPoint(string id) {
			return m_inputPoints.Find(p => p.Id == id);
		}

		public ConnectionPointData FindOutputPoint(string id) {
			return m_outputPoints.Find(p => p.Id == id);
		}

		public ConnectionPointData FindConnectionPoint(string id) {
			var v = FindInputPoint(id);
			if(v != null) {
				return v;
			}
			return FindOutputPoint(id);
		}

		public bool Validate (List<NodeData> allNodes, List<ConnectionData> allConnections) {

			bool allGood = true;

			foreach(var n in allNodes) {
				allGood &= n.Validate(allNodes, allConnections);
			}

			return allGood;
		}

		public bool CompareIgnoreGUIChanges (NodeData rhs) {

			if(m_nodeInstance != rhs.m_nodeInstance) {
				LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Node Type");
				return false;
			}

			if(m_inputPoints.Count != rhs.m_inputPoints.Count) {
				LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Input Count");
				return false;
			}

			if(m_outputPoints.Count != rhs.m_outputPoints.Count) {
				LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Output Count");
				return false;
			}

			foreach(var pin in m_inputPoints) {
				if(rhs.m_inputPoints.Find(x => pin.Id == x.Id) == null) {
					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Input point not found");
					return false;
				}
			}

			foreach(var pout in m_outputPoints) {
				if(rhs.m_outputPoints.Find(x => pout.Id == x.Id) == null) {
					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Output point not found");
					return false;
				}
			}


			return true;
		}
	}
}
