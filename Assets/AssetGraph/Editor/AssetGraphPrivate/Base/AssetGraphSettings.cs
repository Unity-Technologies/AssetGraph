using System;
using System.Collections.Generic;
using System.IO;

namespace AssetGraph {
	public class AssetGraphSettings {
		public const string GUI_TEXT_MENU_OPEN = "AssetGraph/Open...";
		
		public const string ASSETGRAPH_DATA_PATH = "AssetGraph/SettingFiles";
		public const string ASSETGRAPH_DATA_NAME = "AssetGraph.json";

		public const string ASSETGRAPH_PATH = "Assets/AssetGraph/";
		public const string APPLICATIONDATAPATH_TEMP_PATH = ASSETGRAPH_PATH + "Cache/";
		
		public const string IMPORTER_TEMP_PLACE			= APPLICATIONDATAPATH_TEMP_PATH + "Imported";
		public const string PREFABRICATOR_TEMP_PLACE	= APPLICATIONDATAPATH_TEMP_PATH + "Prefabricated";
		public const string BUNDLIZER_TEMP_PLACE		= APPLICATIONDATAPATH_TEMP_PATH + "Bundlized";
		public const string BUNDLEBUILDER_TEMP_PLACE	= APPLICATIONDATAPATH_TEMP_PATH + "BundleBuilt";

		public const string IMPORTER_SAMPLING_PLACE		= ASSETGRAPH_PATH + "Settings";

		public const string UNITY_METAFILE_EXTENSION = ".meta";
		public const string UNITY_LOCAL_DATAPATH = "Assets";
		public const string DOTSTART_HIDDEN_FILE_HEADSTRING = ".";
		public const char UNITY_FOLDER_SEPARATOR = '/';// Mac/Windows/Linux can use '/' in Unity.

		public const char KEYWORD_WILDCARD = '*';

		public static List<string> DefaultBundleOptionSettings = new List<string> {
			"Uncompressed AssetBundle",
			"Disable Write TypeTree",
			"Deterministic AssetBundle",
			"Force Rebuild AssetBundle",
			"Ignore TypeTree Changes",
			"Append Hash To AssetBundle Name"
		};

		public const float WINDOW_SPAN = 20f;

		/*
			node generation from GUI
		*/
		public const string MENU_LOADER_NAME = "Loader";
		public const string MENU_FILTER_NAME = "Filter";
		public const string MENU_IMPORTER_NAME = "Importer";
		public const string MENU_GROUPING_NAME = "Grouping";
		public const string MENU_PREFABRICATOR_NAME = "Prefabricator";
		public const string MENU_BUNDLIZER_NAME = "Bundlizer";
		public const string MENU_BUNDLEBUILDER_NAME = "BundleBuilder";
		public const string MENU_EXPORTER_NAME = "Exporter";

		public static Dictionary<string, NodeKind> GUI_Menu_Item_TargetGUINodeDict = new Dictionary<string, NodeKind>{
			{"Create " + MENU_LOADER_NAME + " Node", NodeKind.LOADER_GUI},
			{"Create " + MENU_FILTER_NAME + " Node", NodeKind.FILTER_GUI},
			{"Create " + MENU_IMPORTER_NAME + " Node", NodeKind.IMPORTER_GUI},
			{"Create " + MENU_GROUPING_NAME + " Node", NodeKind.GROUPING_GUI},
			{"Create " + MENU_PREFABRICATOR_NAME + " Node", NodeKind.PREFABRICATOR_GUI},
			{"Create " + MENU_BUNDLIZER_NAME + " Node", NodeKind.BUNDLIZER_GUI},
			{"Create " + MENU_BUNDLEBUILDER_NAME + " Node", NodeKind.BUNDLEBUILDER_GUI},
			{"Create " + MENU_EXPORTER_NAME + " Node", NodeKind.EXPORTER_GUI}
		};

		public static Dictionary<NodeKind, string> DEFAULT_NODE_NAME = new Dictionary<NodeKind, string>{
			{NodeKind.LOADER_GUI, "Loader"},
			{NodeKind.FILTER_GUI, "Filter"},
			{NodeKind.IMPORTER_GUI, "Importer"},
			{NodeKind.GROUPING_GUI, "Grouping"},
			{NodeKind.PREFABRICATOR_GUI, "Prefabricator"},
			{NodeKind.BUNDLIZER_GUI, "Bundlizer"},
			{NodeKind.BUNDLEBUILDER_GUI, "BundleBuilder"},
			{NodeKind.EXPORTER_GUI, "Exporter"}
		};

		/*
			data key for AssetGraph.json
		*/
		public const string ASSETGRAPH_DATA_LASTMODIFIED = "lastModified";
		public const string ASSETGRAPH_DATA_NODES = "nodes";
		public const string ASSETGRAPH_DATA_CONNECTIONS = "connections";

		// node const
		public const string NODE_NAME = "name";
		public const string NODE_ID = "id";
		public const string NODE_KIND = "kind";
		public const string LOADERNODE_LOAD_PATH = "loadPath";
		public const string EXPORTERNODE_EXPORT_PATH = "exportPath";
		public const string NODE_SCRIPT_TYPE = "scriptType";
		public const string NODE_SCRIPT_PATH = "scriptPath";
		public const string NODE_POS = "pos";
		public const string NODE_POS_X = "x";
		public const string NODE_POS_Y = "y";
		public const string NODE_OUTPUT_LABELS = "outputLabels";

		public const string NODE_FILTER_CONTAINS_KEYWORDS = "filterContainsKeywords";
		public const string NODE_GROUPING_KEYWORD = "groupingKeyword";
		public const string NODE_BUNDLIZER_BUNDLENAME_TEMPLATE = "bundleNameTemplate";
		public const string NODE_BUNDLEBUILDER_ENABLEDBUNDLEOPTIONS = "enabledBundleOptions";

		// connection const
		public const string CONNECTION_LABEL = "label";
		public const string CONNECTION_ID = "connectionId";
		public const string CONNECTION_FROMNODE = "fromNode";
		public const string CONNECTION_TONODE = "toNode";
		
		// by default, AssetGraph's node has only 1 InputPoint. and 
		// this is only one definition of it's label.
		public const string DEFAULT_INPUTPOINT_LABEL = "-";
		public const string DEFAULT_OUTPUTPOINT_LABEL = "+";
		public const string DUMMY_IMPORTER_LABELTONEXT = "importer_dummy_label";

		public const string DEFAULT_FILTER_KEYWORD = "set keyword.";

		public enum NodeKind : int {
			FILTER_SCRIPT,
			IMPORTER_SCRIPT,
			PREFABRICATOR_SCRIPT,
			BUNDLIZER_SCRIPT,

			LOADER_GUI,
			FILTER_GUI,
			IMPORTER_GUI,
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