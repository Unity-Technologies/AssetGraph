using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AssetBundleGraph {

	public enum NodeKind : int {
		FILTER_SCRIPT,
		PREFABRICATOR_SCRIPT,

		LOADER_GUI,
		FILTER_GUI,
		IMPORTSETTING_GUI,
		MODIFIER_GUI,

		GROUPING_GUI,
		PREFABRICATOR_GUI,
		BUNDLECONFIG_GUI,
		BUNDLEBUILDER_GUI,

		EXPORTER_GUI
	}

	[Serializable]
	public class FilterEntry {
		[SerializeField] private string m_filterKeyword;
		[SerializeField] private string m_filterKeytype;
		[SerializeField] private ConnectionPointData m_point;

		public FilterEntry(string keyword, string keytype, ConnectionPointData point) {
			m_filterKeyword = keyword;
			m_filterKeytype = keytype;
			m_point = point;
		}

		public string FilterKeyword {
			get {
				return m_filterKeyword;
			}
			set {
				m_filterKeyword = value;
				m_point.Label = value;
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
		public ConnectionPointData ConnectionPoint {
			get {
				return m_point; 
			}
		}
		public string Hash {
			get {
				return m_filterKeyword+m_filterKeytype;
			}
		}
	}

	[Serializable]
	public class Variant {
		[SerializeField] private string m_name;
		[SerializeField] private ConnectionPointData m_point;

		public Variant(string name, ConnectionPointData point) {
			m_name = name;
			m_point = point;
		}

		public string Name {
			get {
				return m_name;
			}
			set {
				m_name = value;
				m_point.Label = value;
			}
		}
		public ConnectionPointData ConnectionPoint {
			get {
				return m_point; 
			}
		}
	}

	/*
	 * node data saved in/to Json
	 */
	[Serializable]
	public class NodeData {

		private const string NODE_NAME = "name";
		private const string NODE_ID = "id";
		private const string NODE_KIND = "kind";
		private const string NODE_SCRIPT_CLASSNAME = "scriptClassName";
		private const string NODE_POS = "pos";
		private const string NODE_POS_X = "x";
		private const string NODE_POS_Y = "y";

		private const string NODE_INPUTPOINTS = "inputPoints";
		private const string NODE_OUTPUTPOINTS = "outputPoints";

		//loader settings
		private const string NODE_LOADER_LOAD_PATH = "loadPath";

		//exporter settings
		private const string NODE_EXPORTER_EXPORT_PATH = "exportTo";

		//filter settings
		private const string NODE_FILTER = "filter";
		private const string NODE_FILTER_KEYWORD = "keyword";
		private const string NODE_FILTER_KEYTYPE = "keytype";
		private const string NODE_FILTER_POINTID = "pointId";

		//group settings
		private const string NODE_GROUPING_KEYWORD = "groupingKeyword";

		//bundleconfig settings
		private const string NODE_BUNDLECONFIG_BUNDLENAME_TEMPLATE = "bundleNameTemplate";
		private const string NODE_BUNDLECONFIG_VARIANTS 		 = "variants";
		private const string NODE_BUNDLECONFIG_VARIANTS_NAME 	 = "name";
		private const string NODE_BUNDLECONFIG_VARIANTS_POINTID = "pointId";

		//bundlebuilder settings
		private const string NODE_BUNDLEBUILDER_ENABLEDBUNDLEOPTIONS = "enabledBundleOptions";

		[SerializeField] private string m_name;
		[SerializeField] private string m_id;
		[SerializeField] private NodeKind m_kind;
		[SerializeField] private float m_x;
		[SerializeField] private float m_y;
		[SerializeField] private string m_scriptClassName;
		[SerializeField] private List<FilterEntry> m_filter;
		[SerializeField] private List<ConnectionPointData> 	m_inputPoints; 
		[SerializeField] private List<ConnectionPointData> 	m_outputPoints; 
		[SerializeField] private SerializableMultiTargetString m_loaderLoadPath;
		[SerializeField] private SerializableMultiTargetString m_exporterExportPath;
		[SerializeField] private SerializableMultiTargetString m_groupingKeyword;
		[SerializeField] private SerializableMultiTargetString m_bundleConfigBundleNameTemplate;
		[SerializeField] private List<Variant> m_variants;
		[SerializeField] private SerializableMultiTargetInt m_bundleBuilderEnabledBundleOptions;

		[SerializeField] private bool m_isNodeOperationPerformed;


		/*
		 * Properties
		 */ 

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
		public NodeKind Kind {
			get {
				return m_kind;
			}
		}
		public string ScriptClassName {
			get {
				ValidateAccess(
					NodeKind.FILTER_SCRIPT, 
					NodeKind.PREFABRICATOR_SCRIPT,
					NodeKind.PREFABRICATOR_GUI,
					NodeKind.MODIFIER_GUI
				);
				return m_scriptClassName;
			}
			set {
				ValidateAccess(
					NodeKind.FILTER_SCRIPT, 
					NodeKind.PREFABRICATOR_SCRIPT,
					NodeKind.PREFABRICATOR_GUI,
					NodeKind.MODIFIER_GUI
				);
				m_scriptClassName = value;
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

//		public Rect Region {
//			get {
//				return m_rect;
//			}
//			set {
//				m_rect = value;
//			}
//		}

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

		public SerializableMultiTargetString LoaderLoadPath {
			get {
				ValidateAccess(
					NodeKind.LOADER_GUI 
				);
				return m_loaderLoadPath;
			}
		}

		public SerializableMultiTargetString ExporterExportPath {
			get {
				ValidateAccess(
					NodeKind.EXPORTER_GUI 
				);
				return m_exporterExportPath;
			}
		}

		public SerializableMultiTargetString GroupingKeywords {
			get {
				ValidateAccess(
					NodeKind.GROUPING_GUI 
				);
				return m_groupingKeyword;
			}
		}

		public SerializableMultiTargetString BundleNameTemplate {
			get {
				ValidateAccess(
					NodeKind.BUNDLECONFIG_GUI 
				);
				return m_bundleConfigBundleNameTemplate;
			}
		}

		public List<Variant> Variants {
			get {
				ValidateAccess(
					NodeKind.BUNDLECONFIG_GUI 
				);
				return m_variants;
			}
		}

		public SerializableMultiTargetInt BundleBuilderBundleOptions {
			get {
				ValidateAccess(
					NodeKind.BUNDLEBUILDER_GUI 
				);
				return m_bundleBuilderEnabledBundleOptions;
			}
		}

		public List<FilterEntry> FilterConditions {
			get {
				ValidateAccess(
					NodeKind.FILTER_GUI
				);
				return m_filter;
			}
		}

		/*
		 *  Create NodeData from JSON
		 */ 
		public NodeData(Dictionary<string, object> jsonData) {

			m_name = jsonData[NODE_NAME] as string;
			m_id = jsonData[NODE_ID]as string;
			m_kind = AssetBundleGraphSettings.NodeKindFromString(jsonData[NODE_KIND] as string);

			var pos = jsonData[NODE_POS] as Dictionary<string, object>;
			m_x = (float)Convert.ToDouble(pos[NODE_POS_X]);
			m_y = (float)Convert.ToDouble(pos[NODE_POS_Y]);

//			m_rect = new Rect(x, y, AssetBundleGraphGUISettings.NODE_BASE_WIDTH, AssetBundleGraphGUISettings.NODE_BASE_HEIGHT);

			var inputs  = jsonData[NODE_INPUTPOINTS] as List<object>;
			var outputs = jsonData[NODE_OUTPUTPOINTS] as List<object>;
			m_inputPoints  = new List<ConnectionPointData>();
			m_outputPoints = new List<ConnectionPointData>();

			foreach(var obj in inputs) {
				var pDic = obj as Dictionary<string, object>;
				m_inputPoints.Add(new ConnectionPointData(pDic, this, true));
			}

			foreach(var obj in outputs) {
				var pDic = obj as Dictionary<string, object>;
				m_outputPoints.Add(new ConnectionPointData(pDic, this, false));
			}

			switch (m_kind) {
			case NodeKind.IMPORTSETTING_GUI:
				// nothing to do
				break;
			case NodeKind.FILTER_SCRIPT:
			case NodeKind.PREFABRICATOR_SCRIPT:
			case NodeKind.PREFABRICATOR_GUI:
			case NodeKind.MODIFIER_GUI:
				{
					if(jsonData.ContainsKey(NODE_SCRIPT_CLASSNAME)) {
						m_scriptClassName = jsonData[NODE_SCRIPT_CLASSNAME] as string;
					}
				}
				break;
			case NodeKind.LOADER_GUI:
				{
					m_loaderLoadPath = new SerializableMultiTargetString(jsonData[NODE_LOADER_LOAD_PATH] as Dictionary<string, object>);
				}
				break;
			case NodeKind.FILTER_GUI:
				{
					var filters = jsonData[NODE_FILTER] as List<object>;

					m_filter = new List<FilterEntry>();

					for(int i=0; i<filters.Count; ++i) {
						var f = filters[i] as Dictionary<string, object>;

						var keyword = f[NODE_FILTER_KEYWORD] as string;
						var keytype = f[NODE_FILTER_KEYTYPE] as string;
						var pointId = f[NODE_FILTER_POINTID] as string;

						var point = m_outputPoints.Find(p => p.Id == pointId);
						UnityEngine.Assertions.Assert.IsNotNull(point, "Output point not found for " + keyword);
						m_filter.Add(new FilterEntry(keyword, keytype, point));
					}
				}
				break;
			case NodeKind.GROUPING_GUI:
				{
					m_groupingKeyword = new SerializableMultiTargetString(jsonData[NODE_GROUPING_KEYWORD] as Dictionary<string, object>);
				}
				break;
			case NodeKind.BUNDLECONFIG_GUI:
				{
					m_bundleConfigBundleNameTemplate = new SerializableMultiTargetString(jsonData[NODE_BUNDLECONFIG_BUNDLENAME_TEMPLATE] as Dictionary<string, object>);
					m_variants = new List<Variant>();
					if(jsonData.ContainsKey(NODE_BUNDLECONFIG_VARIANTS)){
						var variants = jsonData[NODE_BUNDLECONFIG_VARIANTS] as List<object>;

						for(int i=0; i<variants.Count; ++i) {
							var v = variants[i] as Dictionary<string, object>;

							var name    = v[NODE_BUNDLECONFIG_VARIANTS_NAME] as string;
							var pointId = v[NODE_BUNDLECONFIG_VARIANTS_POINTID] as string;

							var point = m_inputPoints.Find(p => p.Id == pointId);
							UnityEngine.Assertions.Assert.IsNotNull(point, "Input point not found for " + name);
							m_variants.Add(new Variant(name, point));
						}
					}
				}
				break;
			case NodeKind.BUNDLEBUILDER_GUI:
				{
					m_bundleBuilderEnabledBundleOptions = new SerializableMultiTargetInt(jsonData[NODE_BUNDLEBUILDER_ENABLEDBUNDLEOPTIONS] as Dictionary<string, object>);
				}
				break;
			case NodeKind.EXPORTER_GUI:
				{
					m_exporterExportPath = new SerializableMultiTargetString(jsonData[NODE_EXPORTER_EXPORT_PATH] as Dictionary<string, object>);
				}
				break;
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		/*
		 * Constructor used to create new node from GUI
		 */ 
		public NodeData(string name, NodeKind kind, float x, float y) {

			m_id = Guid.NewGuid().ToString();
			m_name = name;
			m_x = x;
			m_y = y;
			m_kind = kind;

			m_inputPoints  = new List<ConnectionPointData>();
			m_outputPoints = new List<ConnectionPointData>();


			// adding defalut input point.
			// Loader does not take input
			if(kind != NodeKind.LOADER_GUI) {
				m_inputPoints.Add(new ConnectionPointData(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL, this, true));
			}

			// adding default output point.
			// Filter and Exporter does not have output.
			if(kind != NodeKind.FILTER_GUI && kind != NodeKind.EXPORTER_GUI) {
				m_outputPoints.Add(new ConnectionPointData(AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL, this, false));
			}

			switch(m_kind) {
			case NodeKind.PREFABRICATOR_GUI:
			case NodeKind.IMPORTSETTING_GUI:
				break;
			case NodeKind.MODIFIER_GUI:
				m_scriptClassName 	= String.Empty;
				break;

			case NodeKind.FILTER_GUI:
				m_filter = new List<FilterEntry>();
				break;

			case NodeKind.LOADER_GUI:
				m_loaderLoadPath = new SerializableMultiTargetString();
				break;

			case NodeKind.GROUPING_GUI:
				m_groupingKeyword = new SerializableMultiTargetString(AssetBundleGraphSettings.GROUPING_KEYWORD_DEFAULT);
				break;

			case NodeKind.BUNDLECONFIG_GUI:
				m_bundleConfigBundleNameTemplate = new SerializableMultiTargetString(AssetBundleGraphSettings.BUNDLECONFIG_BUNDLENAME_TEMPLATE_DEFAULT);
				m_variants = new List<Variant>();
				break;

			case NodeKind.BUNDLEBUILDER_GUI:
				m_bundleBuilderEnabledBundleOptions = new SerializableMultiTargetInt();
				break;

			case NodeKind.EXPORTER_GUI:
				m_exporterExportPath = new SerializableMultiTargetString();
				break;

			case NodeKind.FILTER_SCRIPT:
			case NodeKind.PREFABRICATOR_SCRIPT:
				m_scriptClassName = string.Empty;
				break;

			default:
				throw new AssetBundleGraphException("[FATAL]Unhandled nodekind. unimplmented:"+ m_kind);
			}
		}

		/**
		 * Duplicate this node with new guid.
		 */ 
		public NodeData Duplicate () {

			var newData = new NodeData(m_name, m_kind, m_x, m_y);

			switch(m_kind) {
			case NodeKind.IMPORTSETTING_GUI:
			case NodeKind.PREFABRICATOR_GUI:
				break;
			case NodeKind.MODIFIER_GUI:
				newData.m_scriptClassName 	= m_scriptClassName;
				break;

			case NodeKind.FILTER_GUI:
				foreach(var f in m_filter) {
					newData.AddFilterCondition(f.FilterKeyword, f.FilterKeytype);
				}
				break;

			case NodeKind.LOADER_GUI:
				newData.m_loaderLoadPath = new SerializableMultiTargetString(m_loaderLoadPath);
				break;

			case NodeKind.GROUPING_GUI:
				newData.m_groupingKeyword = new SerializableMultiTargetString(m_groupingKeyword);
				break;

			case NodeKind.BUNDLECONFIG_GUI:
				foreach(var v in m_variants) {
					newData.AddVariant(v.Name);
				}
				break;

			case NodeKind.BUNDLEBUILDER_GUI:
				newData.m_bundleBuilderEnabledBundleOptions = new SerializableMultiTargetInt(m_bundleBuilderEnabledBundleOptions);
				break;

			case NodeKind.EXPORTER_GUI:
				newData.m_exporterExportPath = new SerializableMultiTargetString(m_exporterExportPath);
				break;

			case NodeKind.FILTER_SCRIPT:
			case NodeKind.PREFABRICATOR_SCRIPT:
				newData.m_scriptClassName = m_scriptClassName;
				break;

			default:
				throw new AssetBundleGraphException("[FATAL]Unhandled nodekind. unimplmented:"+ m_kind);
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

		public string GetLoaderFullLoadPath(BuildTarget g) {
			return FileUtility.PathCombine(Application.dataPath, LoaderLoadPath[g]);
		}

		public bool ValidateOverlappingFilterCondition(bool throwException) {
			ValidateAccess(NodeKind.FILTER_GUI);

			var conditionGroup = FilterConditions.Select(v => v).GroupBy(v => v.Hash).ToList();
			var overlap = conditionGroup.Find(v => v.Count() > 1);

			if( overlap != null && throwException ) {
				var element = overlap.First();
				throw new NodeException(String.Format("Duplicated filter condition found for [Keyword:{0} Type:{1}]", element.FilterKeyword, element.FilterKeytype), Id);
			}
			return overlap != null;
		}

		public void AddFilterCondition(string keyword, string keytype) {
			ValidateAccess(
				NodeKind.FILTER_GUI
			);

			var point = new ConnectionPointData(keyword, this, false);
			m_outputPoints.Add(point);
			var newEntry = new FilterEntry(keyword, keytype, point);
			m_filter.Add(newEntry);
		}

		public void RemoveFilterCondition(FilterEntry f) {
			ValidateAccess(
				NodeKind.FILTER_GUI
			);

			m_filter.Remove(f);
			m_outputPoints.Remove(f.ConnectionPoint);
		}

		public void AddVariant(string name) {
			ValidateAccess(
				NodeKind.BUNDLECONFIG_GUI
			);

			var point = new ConnectionPointData(name, this, true);
			m_inputPoints.Add(point);
			var newEntry = new Variant(name, point);
			m_variants.Add(newEntry);
		}

		public void RemoveVariant(Variant v) {
			ValidateAccess(
				NodeKind.BUNDLECONFIG_GUI
			);

			m_variants.Remove(v);
			m_inputPoints.Remove(v.ConnectionPoint);
		}

		private void ValidateAccess(params NodeKind[] allowedKind) {
			foreach(var k in allowedKind) {
				if (k == m_kind) {
					return;
				}
			}
			throw new AssetBundleGraphException(m_name + ": Tried to access invalid method or property.");
		}

		/*
		 * Checks deserialized NodeData, and make some changes if necessary
		 * return false if validation failed.
		 */
		public bool Validate (List<NodeData> allNodes, List<ConnectionData> allConnections) {

			switch (m_kind) {	
			case NodeKind.FILTER_SCRIPT:
				if(!TestCreateScriptInstance()) {
					Debug.LogWarning(m_name  + ": Node could not be created properly because AssetBundleGraph failed to create script instance for \"" + 
						m_scriptClassName + "\". No such class found in assembly.");
					return false;
				}

				// TODO: node のコネクションのラベル情報にFilterScriptの最新情報を反映させる

				break;
			case NodeKind.PREFABRICATOR_SCRIPT: 
				if(!TestCreateScriptInstance()) {
					Debug.LogWarning(m_name  + ": Node could not be created properly because AssetBundleGraph failed to create script instance for \"" + 
						m_scriptClassName + "\". No such class found in assembly.");
					return false;
				}
				break;
			}

			return true;
		}

		private bool TestCreateScriptInstance() {
			if(string.IsNullOrEmpty(ScriptClassName)) {
				return false;
			}
			var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(m_scriptClassName);
			return nodeScriptInstance != null;
		}

		/**
		 * Serialize to JSON dictionary
		 */ 
		public Dictionary<string, object> ToJsonDictionary() {
			var nodeDict = new Dictionary<string, object>();

			nodeDict[NODE_NAME] = m_name;
			nodeDict[NODE_ID] 	= m_id;
			nodeDict[NODE_KIND] = m_kind.ToString();

			var inputs  = new List<object>();
			var outputs = new List<object>();

			foreach(var p in m_inputPoints) {
				inputs.Add( p.ToJsonDictionary() );
			}

			foreach(var p in m_outputPoints) {
				outputs.Add( p.ToJsonDictionary() );
			}

			nodeDict[NODE_INPUTPOINTS]  = inputs;
			nodeDict[NODE_OUTPUTPOINTS] = outputs;

			nodeDict[NODE_POS] = new Dictionary<string, object>() {
				{NODE_POS_X, m_x},
				{NODE_POS_Y, m_y}
			};
				
			switch (m_kind) {
			case NodeKind.FILTER_SCRIPT:
			case NodeKind.PREFABRICATOR_SCRIPT:
			case NodeKind.MODIFIER_GUI:
				nodeDict[NODE_SCRIPT_CLASSNAME] = m_scriptClassName;
				break;
			case NodeKind.LOADER_GUI:
				nodeDict[NODE_LOADER_LOAD_PATH] = m_loaderLoadPath.ToJsonDictionary();
				break;
			case NodeKind.FILTER_GUI:
				var filterDict = new List<Dictionary<string, object>>();
				foreach(var f in m_filter) {
					var df = new Dictionary<string, object>();
					df[NODE_FILTER_KEYWORD] = f.FilterKeyword;
					df[NODE_FILTER_KEYTYPE] = f.FilterKeytype;
					df[NODE_FILTER_POINTID] = f.ConnectionPoint.Id;
					filterDict.Add(df);
				}
				nodeDict[NODE_FILTER] = filterDict;
				break;
			case NodeKind.GROUPING_GUI:
				nodeDict[NODE_GROUPING_KEYWORD] = m_groupingKeyword.ToJsonDictionary();
				break;
			case NodeKind.PREFABRICATOR_GUI:
				break;
			case NodeKind.BUNDLECONFIG_GUI:
				nodeDict[NODE_BUNDLECONFIG_BUNDLENAME_TEMPLATE] = m_bundleConfigBundleNameTemplate.ToJsonDictionary();
				var variantsDict = new List<Dictionary<string, object>>();
				foreach(var v in m_variants) {
					var dv = new Dictionary<string, object>();
					dv[NODE_BUNDLECONFIG_VARIANTS_NAME] 	= v.Name;
					dv[NODE_BUNDLECONFIG_VARIANTS_POINTID] = v.ConnectionPoint.Id;
					variantsDict.Add(dv);
				}
				nodeDict[NODE_BUNDLECONFIG_VARIANTS] = variantsDict;
				break;
			case NodeKind.BUNDLEBUILDER_GUI:
				nodeDict[NODE_BUNDLEBUILDER_ENABLEDBUNDLEOPTIONS] = m_bundleBuilderEnabledBundleOptions.ToJsonDictionary();
				break;
			case NodeKind.EXPORTER_GUI:
				nodeDict[NODE_EXPORTER_EXPORT_PATH] = m_exporterExportPath.ToJsonDictionary();
				break;
			case NodeKind.IMPORTSETTING_GUI:
				// nothing to do
				break;
			default:
				throw new ArgumentOutOfRangeException ();
			}

			return nodeDict;
		}

		/**
		 * Serialize to JSON string
		 */ 
		public string ToJsonString() {
			return AssetBundleGraph.Json.Serialize(ToJsonDictionary());
		}
	}
}
