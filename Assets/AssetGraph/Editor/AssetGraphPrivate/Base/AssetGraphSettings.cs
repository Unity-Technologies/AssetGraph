using System;

namespace AssetGraph {
	public class AssetGraphSettings {
		public const string GUI_TEXT_MENU_OPEN = "AssetGraph/Open...";
		
		public const string ASSETGRAPH_TEMP_PATH = "AssetGraph/Temp";
		public const string ASSETGRAPH_DATA_NAME = "AssetGraph.json";

		public const string APPLICATIONDATAPATH_TEMP_PATH = "Assets/AssetGraph/Temp/";
		
		public const string IMPORTER_TEMP_PLACE = APPLICATIONDATAPATH_TEMP_PATH + "Imported/";
		public const string PREFABRICATOR_TEMP_PLACE = APPLICATIONDATAPATH_TEMP_PATH + "Prefabricated/";
		public const string BUNDLIZER_TEMP_PLACE = APPLICATIONDATAPATH_TEMP_PATH + "Bundlized/";

		public const string UNITY_METAFILE_EXTENSION = ".meta";
		public const string DOTSTART_HIDDEN_FILE_HEADSTRING = ".";
		public const char UNITY_FOLDER_SEPARATOR = '/';
		
		/*
			data key for AssetGraph.json
		*/
		public const string ASSETGRAPH_DATA_LASTMODIFIED = "lastModified";
		public const string ASSETGRAPH_DATA_NODES = "nodes";
		public const string ASSETGRAPH_DATA_CONNECTIONS = "connections";

		// node const
		public const string NODE_CLASSNAME = "className";
		public const string NODE_ID = "id";
		public const string NODE_KIND = "kind";
		public const string LOADERNODE_LOAD_PATH = "loadPath";
		public const string EXPORTERNODE_EXPORT_PATH = "exportPath";
		public const string NODE_SCRIPT_PATH = "scriptPath";
		public const string NODE_POS = "pos";
		public const string NODE_POS_X = "x";
		public const string NODE_POS_Y = "y";
		public const string NODE_OUTPUT_LABELS = "outputLabels";

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


		public enum NodeKind : int {
			LOADER,
			FILTER,
			IMPORTER,
			PREFABRICATOR,
			BUNDLIZER,
			EXPORTER,
		}

		public static NodeKind NodeKindFromString (string val) {
			return (NodeKind)Enum.Parse(typeof(NodeKind), val);
		}

	}
}