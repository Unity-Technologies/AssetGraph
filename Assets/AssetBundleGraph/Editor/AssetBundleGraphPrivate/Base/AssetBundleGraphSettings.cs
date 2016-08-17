using System;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public class AssetBundleGraphSettings {
		/*
			if true, ignore .meta files inside AssetBundleGraph.
		*/
		public const bool IGNORE_META = true;

		public const string GUI_TEXT_MENU_OPEN = "Window/AssetBundleGraph/Open Graph Editor";
		public const string GUI_TEXT_MENU_BUILD = "Window/AssetBundleGraph/Build Bundles for Current Platform";
		public const string GUI_TEXT_MENU_GENERATE = "Window/AssetBundleGraph/Generate Node Script";
		public const string GUI_TEXT_MENU_GENERATE_PREFABRICATOR = GUI_TEXT_MENU_GENERATE + "/Prefabricator Script";
		
		public const string GUI_TEXT_MENU_GENERATE_FINALLY = GUI_TEXT_MENU_GENERATE + "/Finally Script";
		public const string GUI_TEXT_MENU_DELETE_CACHE = "Window/AssetBundleGraph/Clear Build Cache";
		
		public const string GUI_TEXT_MENU_DELETE_IMPORTSETTING_SETTINGS = "Window/AssetBundleGraph/Clear Saved ImportSettings";

		public const string ASSETNBUNDLEGRAPH_DATA_PATH = "AssetBundleGraph/SettingFiles";
		public const string ASSETBUNDLEGRAPH_DATA_NAME = "AssetBundleGraph.json";
		
		public const string ASSETS_PATH = "Assets/";
		public const string ASSETBUNDLEGRAPH_PATH = ASSETS_PATH + "AssetBundleGraph/";
		public const string APPLICATIONDATAPATH_CACHE_PATH = ASSETBUNDLEGRAPH_PATH + "Cache/";
		public const string SCRIPT_TEMPLATE_PATH = ASSETBUNDLEGRAPH_PATH + "Editor/ScriptTemplate/";
		public const string USERSPACE_PATH = ASSETBUNDLEGRAPH_PATH + "Generated/Editor/";
		
		public const string PREFABRICATOR_CACHE_PLACE	= APPLICATIONDATAPATH_CACHE_PATH + "Prefabricated";
		public const string BUNDLIZER_CACHE_PLACE		= APPLICATIONDATAPATH_CACHE_PATH + "Bundlized";
		public const string BUNDLEBUILDER_CACHE_PLACE	= APPLICATIONDATAPATH_CACHE_PATH + "BundleBuilt";

		public const string IMPORTER_SETTINGS_PLACE		= ASSETBUNDLEGRAPH_PATH + "SavedSettings/ImportSettings";

		public const string UNITY_METAFILE_EXTENSION = ".meta";
		public const string UNITY_LOCAL_DATAPATH = "Assets";
		public const string DOTSTART_HIDDEN_FILE_HEADSTRING = ".";
		public const string MANIFEST_FOOTER = ".manifest";
		public const string IMPORTER_RECORDFILE = ".importedRecord";
		public const char UNITY_FOLDER_SEPARATOR = '/';// Mac/Windows/Linux can use '/' in Unity.

		public const char KEYWORD_WILDCARD = '*';

		public static List<string> DefaultBundleOptionSettings = new List<string> {
			"Uncompressed AssetBundle",
			"Disable Write TypeTree",
			"Deterministic AssetBundle",
			"Force Rebuild AssetBundle",
			"Ignore TypeTree Changes",
			"Append Hash To AssetBundle Name",
        #if UNITY_5_3
            "ChunkBased Compression"
        #endif			
		};
		
		public const string PLATFORM_DEFAULT_NAME = "Default";
		public const string PLATFORM_STANDALONE = "Standalone";

		public const float WINDOW_SPAN = 20f;

		/*
			node generation from GUI
		*/
		public const string MENU_LOADER_NAME = "Loader";
		public const string MENU_FILTER_NAME = "Filter";
		// public const string MENU_IMPORTER_NAME = "Importer";
		public const string MENU_IMPORTSETTING_NAME = "ImportSetting";
		public const string MENU_MODIFIER_NAME = "Modifier";
		public const string MENU_GROUPING_NAME = "Grouping";
		public const string MENU_PREFABRICATOR_NAME = "Prefabricator";
		public const string MENU_BUNDLIZER_NAME = "Bundlizer";
		public const string MENU_BUNDLEBUILDER_NAME = "BundleBuilder";
		public const string MENU_EXPORTER_NAME = "Exporter";

		public static Dictionary<string, NodeKind> GUI_Menu_Item_TargetGUINodeDict = new Dictionary<string, NodeKind>{
			{"Create " + MENU_LOADER_NAME + " Node", NodeKind.LOADER_GUI},
			{"Create " + MENU_FILTER_NAME + " Node", NodeKind.FILTER_GUI},
			{"Create " + MENU_IMPORTSETTING_NAME + " Node", NodeKind.IMPORTSETTING_GUI},
			{"Create " + MENU_GROUPING_NAME + " Node", NodeKind.GROUPING_GUI},
			{"Create " + MENU_PREFABRICATOR_NAME + " Node", NodeKind.PREFABRICATOR_GUI},
			{"Create " + MENU_BUNDLIZER_NAME + " Node", NodeKind.BUNDLIZER_GUI},
			{"Create " + MENU_BUNDLEBUILDER_NAME + " Node", NodeKind.BUNDLEBUILDER_GUI},
			{"Create " + MENU_EXPORTER_NAME + " Node", NodeKind.EXPORTER_GUI}
		};

		public static Dictionary<NodeKind, string> DEFAULT_NODE_NAME = new Dictionary<NodeKind, string>{
			{NodeKind.LOADER_GUI, "Loader"},
			{NodeKind.FILTER_GUI, "Filter"},
			{NodeKind.IMPORTSETTING_GUI, "ImportSetting"},
			{NodeKind.GROUPING_GUI, "Grouping"},
			{NodeKind.PREFABRICATOR_GUI, "Prefabricator"},
			{NodeKind.BUNDLIZER_GUI, "Bundlizer"},
			{NodeKind.BUNDLEBUILDER_GUI, "BundleBuilder"},
			{NodeKind.EXPORTER_GUI, "Exporter"}
		};

		/*
			data key for AssetBundleGraph.json
		*/
		public const string ASSETBUNDLEGRAPH_DATA_LASTMODIFIED = "lastModified";
		public const string ASSETBUNDLEGRAPH_DATA_NODES = "nodes";
		public const string ASSETBUNDLEGRAPH_DATA_CONNECTIONS = "connections";

		// node const.
		public const string NODE_NAME = "name";
		public const string NODE_ID = "id";
		public const string NODE_KIND = "kind";
		public const string NODE_SCRIPT_CLASSNAME = "scriptClassName";
		public const string NODE_SCRIPT_PATH = "scriptPath";
		public const string NODE_POS = "pos";
		public const string NODE_POS_X = "x";
		public const string NODE_POS_Y = "y";
		public const string NODE_OUTPUTPOINT_LABELS = "outputPointLabels";
		public const string NODE_OUTPUTPOINT_IDS = "outputPointIds";

		// node dependent settings.
		public const string NODE_LOADER_LOAD_PATH = "loadPath";
		public const string NODE_EXPORTER_EXPORT_PATH = "exportPath";
		public const string NODE_FILTER_CONTAINS_KEYWORDS = "filterContainsKeywords";
		public const string NODE_FILTER_CONTAINS_KEYTYPES = "filterContainsKeytypes";
		public const string NODE_IMPORTER_PACKAGES = "importerPackages";
		public const string NODE_GROUPING_KEYWORD = "groupingKeyword";
		public const string NODE_BUNDLIZER_BUNDLENAME_TEMPLATE = "bundleNameTemplate";
		public const string NODE_BUNDLIZER_USE_OUTPUT = "bundleUseOutput";
		public const string NODE_BUNDLEBUILDER_ENABLEDBUNDLEOPTIONS = "enabledBundleOptions";

		public const string GROUPING_KEYWORD_DEFAULT = "/Group_*/";
		public const string BUNDLIZER_BUNDLENAME_TEMPLATE_DEFAULT = "bundle_*.assetbundle";
		public const string BUNDLIZER_USEOUTPUT_DEFAULT = "false";
		
		// connection const.
		public const string CONNECTION_LABEL = "label";
		public const string CONNECTION_ID = "connectionId";
		public const string CONNECTION_FROMNODE = "fromNode";
		public const string CONNECTION_FROMNODE_CONPOINT_ID = "fromNodeConPointId";
		public const string CONNECTION_TONODE = "toNode";
		
		// by default, AssetBundleGraph's node has only 1 InputPoint. and 
		// this is only one definition of it's label.
		public const string DEFAULT_INPUTPOINT_LABEL = "-";
		public const string DEFAULT_OUTPUTPOINT_LABEL = "+";
		public const string BUNDLIZER_BUNDLE_OUTPUTPOINT_LABEL = "bundles";
		public const string BUNDLIZER_DEPENDENCY_OUTPUTPOINT_LABEL = "bundled assets";

		public const string BUNDLIZER_FAKE_CONNECTION_ID = "b_______-____-____-____-____________";

		public const string DEFAULT_FILTER_KEYWORD = "keyword";
		public const string DEFAULT_FILTER_KEYTYPE = "Any";
		
		public const string FILTER_KEYWORD_WILDCARD = "*";
		public const string FILTER_FAKE_CONNECTION_ID = "f_______-____-____-____-____________";
		

		public enum NodeKind : int {
			FILTER_SCRIPT,
			PREFABRICATOR_SCRIPT,

			LOADER_GUI,
			FILTER_GUI,
			IMPORTSETTING_GUI,
			
			GROUPING_GUI,
			PREFABRICATOR_GUI,
			BUNDLIZER_GUI,
			BUNDLEBUILDER_GUI,
			
			EXPORTER_GUI
		}

		public static NodeKind NodeKindFromString (string val) {
			return (NodeKind)Enum.Parse(typeof(NodeKind), val);
		}
	}
}
