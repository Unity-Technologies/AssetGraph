using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

using AssetBundleGraph;

namespace AssetBundleGraph.V2 {

	[Serializable]
	public class FilterEntry {
		[SerializeField] private string m_filterKeyword;
		[SerializeField] private string m_filterKeytype;
		[SerializeField] private ConnectionPointData m_point; // deprecated. it is here for compatibility
		[SerializeField] private string m_pointId;

		public FilterEntry(string keyword, string keytype, ConnectionPointData point) {
			m_filterKeyword = keyword;
			m_filterKeytype = keytype;
			m_pointId = point.Id;
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
				if(m_pointId == null && m_point != null) {
					m_pointId = m_point.Id;
				}
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
//				m_inputPoints.Add(new ConnectionPointData(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL, this, true));
//			}
//
//			// adding default output point.
//			// Filter and Exporter does not have output.
//			if(kind != NodeKind.FILTER_GUI && kind != NodeKind.EXPORTER_GUI) {
//				m_outputPoints.Add(new ConnectionPointData(AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL, this, false));
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
//				m_groupingKeyword = new SerializableMultiTargetString(AssetBundleGraphSettings.GROUPING_KEYWORD_DEFAULT);
//				break;
//
//			case NodeKind.BUNDLECONFIG_GUI:
//				m_bundleConfigBundleNameTemplate = new SerializableMultiTargetString(AssetBundleGraphSettings.BUNDLECONFIG_BUNDLENAME_TEMPLATE_DEFAULT);
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

//			switch(m_kind) {
//			case NodeKind.IMPORTSETTING_GUI:
//				break;
//			case NodeKind.PREFABBUILDER_GUI:
//				newData.m_prefabBuilderReplacePrefabOptions = m_prefabBuilderReplacePrefabOptions;
//				newData.m_scriptInstanceData = new SerializableMultiTargetString(m_scriptInstanceData);
//				break;
//
//			case NodeKind.MODIFIER_GUI:
//				newData.m_scriptInstanceData = new SerializableMultiTargetString(m_scriptInstanceData);
//				break;
//
//			case NodeKind.FILTER_GUI:
//				foreach(var f in m_filter) {
//					newData.AddFilterCondition(f.FilterKeyword, f.FilterKeytype);
//				}
//				break;
//
//			case NodeKind.LOADER_GUI:
//				newData.m_loaderLoadPath = new SerializableMultiTargetString(m_loaderLoadPath);
//				break;
//
//			case NodeKind.GROUPING_GUI:
//				newData.m_groupingKeyword = new SerializableMultiTargetString(m_groupingKeyword);
//				break;
//
//			case NodeKind.BUNDLECONFIG_GUI:
//				newData.m_bundleConfigBundleNameTemplate = new SerializableMultiTargetString(m_bundleConfigBundleNameTemplate);
//				newData.m_bundleConfigUseGroupAsVariants = m_bundleConfigUseGroupAsVariants;
//				foreach(var v in m_variants) {
//					newData.AddVariant(v.Name);
//				}
//				break;
//
//			case NodeKind.BUNDLEBUILDER_GUI:
//				newData.m_bundleBuilderEnabledBundleOptions = new SerializableMultiTargetInt(m_bundleBuilderEnabledBundleOptions);
//				break;
//
//			case NodeKind.EXPORTER_GUI:
//				newData.m_exporterExportPath = new SerializableMultiTargetString(m_exporterExportPath);
//				newData.m_exporterExportOption = new SerializableMultiTargetInt(m_exporterExportOption);
//				break;
//
//			default:
//				throw new AssetBundleGraphException("[FATAL]Unhandled nodekind. unimplmented:"+ m_kind);
//			}

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

//		public string GetLoaderFullLoadPath(BuildTarget g) {
//			return FileUtility.PathCombine(Application.dataPath, LoaderLoadPath[g]);
//		}
//
//		public bool ValidateOverlappingFilterCondition(bool throwException) {
//			ValidateAccess(NodeKind.FILTER_GUI);
//
//			var conditionGroup = FilterConditions.Select(v => v).GroupBy(v => v.Hash).ToList();
//			var overlap = conditionGroup.Find(v => v.Count() > 1);
//
//			if( overlap != null && throwException ) {
//				var element = overlap.First();
//				throw new NodeException(String.Format("Duplicated filter condition found for [Keyword:{0} Type:{1}]", element.FilterKeyword, element.FilterKeytype), Id);
//			}
//			return overlap != null;
//		}
//
//		public void AddFilterCondition(string keyword, string keytype) {
//			ValidateAccess(
//				NodeKind.FILTER_GUI
//			);
//
//			var point = new ConnectionPointData(keyword, this, false);
//			m_outputPoints.Add(point);
//			var newEntry = new FilterEntry(keyword, keytype, point);
//			m_filter.Add(newEntry);
//			UpdateFilterEntry(newEntry);
//		}
//
//		public void RemoveFilterCondition(FilterEntry f) {
//			ValidateAccess(
//				NodeKind.FILTER_GUI
//			);
//
//			m_filter.Remove(f);
//			m_outputPoints.Remove(GetConnectionPoint(f));
//		}
//
//		public ConnectionPointData GetConnectionPoint(FilterEntry f) {
//			ConnectionPointData p = m_outputPoints.Find(v => v.Id == f.ConnectionPointId);
//			UnityEngine.Assertions.Assert.IsNotNull(p);
//			return p;
//		}
//
//		public void UpdateFilterEntry(FilterEntry f) {
//
//			ConnectionPointData p = m_outputPoints.Find(v => v.Id == f.ConnectionPointId);
//			UnityEngine.Assertions.Assert.IsNotNull(p);
//
//			if(f.FilterKeytype == AssetBundleGraphSettings.DEFAULT_FILTER_KEYTYPE) {
//				p.Label = f.FilterKeyword;
//			} else {
//				var pointIndex = f.FilterKeytype.LastIndexOf('.');
//				var keytypeName = (pointIndex > 0)? f.FilterKeytype.Substring(pointIndex+1):f.FilterKeytype;
//				p.Label = string.Format("{0}[{1}]", f.FilterKeyword, keytypeName);
//			}
//		}
//
//		public void AddVariant(string name) {
//			ValidateAccess(
//				NodeKind.BUNDLECONFIG_GUI
//			);
//
//			var point = new ConnectionPointData(name, this, true);
//			m_inputPoints.Add(point);
//			var newEntry = new Variant(name, point);
//			m_variants.Add(newEntry);
//			UpdateVariant(newEntry);
//		}
//
//		public void RemoveVariant(Variant v) {
//			ValidateAccess(
//				NodeKind.BUNDLECONFIG_GUI
//			);
//
//			m_variants.Remove(v);
//			m_inputPoints.Remove(GetConnectionPoint(v));
//		}
//
//		public ConnectionPointData GetConnectionPoint(Variant v) {
//			ConnectionPointData p = m_inputPoints.Find(point => point.Id == v.ConnectionPointId);
//			UnityEngine.Assertions.Assert.IsNotNull(p);
//			return p;
//		}
//
//		public void UpdateVariant(Variant variant) {
//
//			ConnectionPointData p = m_inputPoints.Find(v => v.Id == variant.ConnectionPointId);
//			UnityEngine.Assertions.Assert.IsNotNull(p);
//
//			p.Label = variant.Name;
//		}
//
//		private void ValidateAccess(params NodeKind[] allowedKind) {
//			foreach(var k in allowedKind) {
//				if (k == m_kind) {
//					return;
//				}
//			}
//			throw new AssetBundleGraphException(m_name + ": Tried to access invalid method or property.");
//		}
//
		public bool Validate (List<NodeData> allNodes, List<ConnectionData> allConnections) {

			bool allGood = true;

			foreach(var n in allNodes) {
				allGood &= n.Validate(allNodes, allConnections);
			}

			return allGood;

//			switch(m_kind) {
//			case NodeKind.BUNDLEBUILDER_GUI:
//				{
//					foreach(var v in m_bundleBuilderEnabledBundleOptions.Values) {
//						bool isDisableWriteTypeTreeEnabled  = 0 < (v.value & (int)BuildAssetBundleOptions.DisableWriteTypeTree);
//						bool isIgnoreTypeTreeChangesEnabled = 0 < (v.value & (int)BuildAssetBundleOptions.IgnoreTypeTreeChanges);
//
//						// If both are marked something is wrong. Clear both flag and save.
//						if(isDisableWriteTypeTreeEnabled && isIgnoreTypeTreeChangesEnabled) {
//							int flag = ~((int)BuildAssetBundleOptions.DisableWriteTypeTree + (int)BuildAssetBundleOptions.IgnoreTypeTreeChanges);
//							v.value = v.value & flag;
//							LogUtility.Logger.LogWarning(LogUtility.kTag, m_name + ": DisableWriteTypeTree and IgnoreTypeTreeChanges can not be used together. Settings overwritten.");
//						}
//					}
//				}
//				break;
//			}
//
//			return true;
		}

//		private bool TestCreateScriptInstance() {
//			if(string.IsNullOrEmpty(ScriptClassName)) {
//				return false;
//			}
//			var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(m_scriptClassName);
//			return nodeScriptInstance != null;
//		}
//
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
