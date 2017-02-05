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
		[SerializeField] private SerializedInstance<INode> m_nodeInstance;
		[SerializeField] private List<ConnectionPointData> 	m_inputPoints; 
		[SerializeField] private List<ConnectionPointData> 	m_outputPoints;

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
		public SerializedInstance<INode> Operation {
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
		public NodeData(string name, INode node, float x, float y) {

			m_id = Guid.NewGuid().ToString();
			m_name = name;
			m_x = x;
			m_y = y;
			m_nodeInstance = new SerializedInstance<INode>(node);
			m_nodeNeedsRevisit = false;

			m_inputPoints  = new List<ConnectionPointData>();
			m_outputPoints = new List<ConnectionPointData>();

			m_nodeInstance.Object.Initialize(this);

			//Take care of this with Initialize(NodeData)

//			// adding defalut input point.
//			// Loader does not take input
//			if(kind != NodeKind.LOADER_GUI) {
//				m_inputPoints.Add(new ConnectionPointData(Settings.DEFAULT_INPUTPOINT_LABEL, this, true));
//			}
//
//			// adding default output point.
//			// Filter and Exporter does not have output.
//			if(kind != NodeKind.FILTER_GUI && kind != NodeKind.EXPORTER_GUI) {
//				m_outputPoints.Add(new ConnectionPointData(Settings.DEFAULT_OUTPUTPOINT_LABEL, this, false));
//			}
//
//			switch(m_kind) {
//			case NodeKind.PREFABBUILDER_GUI:
//				m_prefabBuilderReplacePrefabOptions = (int)UnityEditor.ReplacePrefabOptions.Default;
//				m_scriptInstanceData = new SerializableMultiTargetString();
//				break;
//
//			case NodeKind.MODIFIER_GUI:
//				m_scriptInstanceData = new SerializableMultiTargetString();
//				break;
//			
//			case NodeKind.IMPORTSETTING_GUI:
//				break;
//
//			case NodeKind.FILTER_GUI:
//				m_filter = new List<FilterEntry>();
//				break;
//
//			case NodeKind.LOADER_GUI:
//				m_loaderLoadPath = new SerializableMultiTargetString();
//				break;
//
//			case NodeKind.GROUPING_GUI:
//				m_groupingKeyword = new SerializableMultiTargetString(Settings.GROUPING_KEYWORD_DEFAULT);
//				break;
//
//			case NodeKind.BUNDLECONFIG_GUI:
//				m_bundleConfigBundleNameTemplate = new SerializableMultiTargetString(Settings.BUNDLECONFIG_BUNDLENAME_TEMPLATE_DEFAULT);
//				m_bundleConfigUseGroupAsVariants = false;
//				m_variants = new List<Variant>();
//				break;
//
//			case NodeKind.BUNDLEBUILDER_GUI:
//				m_bundleBuilderEnabledBundleOptions = new SerializableMultiTargetInt();
//				break;
//
//			case NodeKind.EXPORTER_GUI:
//				m_exporterExportPath = new SerializableMultiTargetString();
//				m_exporterExportOption = new SerializableMultiTargetInt();
//				break;
//
//			default:
//				throw new AssetBundleGraphException("[FATAL]Unhandled nodekind. unimplmented:"+ m_kind);
//			}
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

//			switch (m_kind) {
//			case NodeKind.PREFABBUILDER_GUI:
//				if(m_prefabBuilderReplacePrefabOptions != rhs.m_prefabBuilderReplacePrefabOptions) {
//					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "ReplacePrefabOptions different");
//					return false;
//				}
//				if(m_scriptInstanceData != rhs.m_scriptInstanceData) {
//					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Script instance data different");
//					return false;
//				}
//				break;
//
//			case NodeKind.MODIFIER_GUI:
//				if(m_scriptInstanceData != rhs.m_scriptInstanceData) {
//					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Script instance data different");
//					return false;
//				}
//				break;
//
//			case NodeKind.LOADER_GUI:
//				if(m_loaderLoadPath != rhs.m_loaderLoadPath) {
//					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Loader load path different");
//					return false;
//				}
//				break;
//
//			case NodeKind.FILTER_GUI:
//				foreach(var f in m_filter) {
//					if(null == rhs.m_filter.Find(x => x.FilterKeytype == f.FilterKeytype && x.FilterKeyword == f.FilterKeyword)) {
//						LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Filter entry not found");
//						return false;
//					}
//				}
//				break;
//
//			case NodeKind.GROUPING_GUI:
//				if(m_groupingKeyword != rhs.m_groupingKeyword) {
//					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Grouping keyword different");
//					return false;
//				}
//				break;
//
//			case NodeKind.BUNDLECONFIG_GUI:
//				if(m_bundleConfigBundleNameTemplate != rhs.m_bundleConfigBundleNameTemplate) {
//					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "BundleNameTemplate different");
//					return false;
//				}
//				if(m_bundleConfigUseGroupAsVariants != rhs.m_bundleConfigUseGroupAsVariants) {
//					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "UseGroupAsVariants different");
//					return false;
//				}
//				foreach(var v in m_variants) {
//					if(null == rhs.m_variants.Find(x => x.Name == v.Name && x.ConnectionPointId == v.ConnectionPointId)) {
//						LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Variants not found");
//						return false;
//					}
//				}
//				break;
//
//			case NodeKind.BUNDLEBUILDER_GUI:
//				if(m_bundleBuilderEnabledBundleOptions != rhs.m_bundleBuilderEnabledBundleOptions) {
//					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "EnabledBundleOptions different");
//					return false;
//				}
//				break;
//
//			case NodeKind.EXPORTER_GUI:
//				if(m_exporterExportPath != rhs.m_exporterExportPath) {
//					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "ExporterPath different");
//					return false;
//				}
//				if(m_exporterExportOption != rhs.m_exporterExportOption) {
//					LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "ExporterOption different");
//					return false;
//				}
//				break;
//
//			case NodeKind.IMPORTSETTING_GUI:
//				// nothing to do
//				break;
//
//			default:
//				throw new ArgumentOutOfRangeException ();
//			}

			return true;
		}
	}
}
