using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using System.IO;

using UnityEngine.AssetBundles.GraphTool;

namespace AssetBundleGraph {
	public class Settings {
		/*
			if true, ignore .meta files inside AssetBundleGraph.
		*/
		public const bool IGNORE_META = true;

		public const string GUI_TEXT_MENU_OPEN = "Window/AssetBundleGraph/Open Graph Editor";
		public const string GUI_TEXT_MENU_BUILD = "Window/AssetBundleGraph/Build Bundles for Current Platform";
		public const string GUI_TEXT_MENU_GENERATE = "Window/AssetBundleGraph/Create Node Script";
		public const string GUI_TEXT_MENU_GENERATE_MODIFIER = GUI_TEXT_MENU_GENERATE + "/Modifier Script";
		public const string GUI_TEXT_MENU_GENERATE_PREFABBUILDER = GUI_TEXT_MENU_GENERATE + "/PrefabBuilder Script";
		public const string GUI_TEXT_MENU_GENERATE_CUITOOL = "Window/AssetBundleGraph/Create CUI Tool";

		public const string GUI_TEXT_MENU_GENERATE_POSTPROCESS = GUI_TEXT_MENU_GENERATE + "/Postprocess Script";
		public const string GUI_TEXT_MENU_DELETE_CACHE = "Window/AssetBundleGraph/Clear Build Cache";
		
		public const string GUI_TEXT_MENU_DELETE_IMPORTSETTING_SETTINGS = "Window/AssetBundleGraph/Clear Saved ImportSettings";

		public const string UNITY_METAFILE_EXTENSION = ".meta";
		public const string UNITY_LOCAL_DATAPATH = "Assets";
		public const string DOTSTART_HIDDEN_FILE_HEADSTRING = ".";
		public const string MANIFEST_FOOTER = ".manifest";
		public const string IMPORTER_RECORDFILE = ".importedRecord";
		public const char UNITY_FOLDER_SEPARATOR = '/';// Mac/Windows/Linux can use '/' in Unity.

		public const string BASE64_IDENTIFIER = "B64|";


		public const char KEYWORD_WILDCARD = '*';

		public struct BuildAssetBundleOption {
			public readonly BuildAssetBundleOptions option;
			public readonly string description;
			public BuildAssetBundleOption(string desc, BuildAssetBundleOptions opt) {
				option = opt;
				description = desc;
			}
		}

        public class Path {
            private static string s_basePath;

            public static string BasePath {
                get {
                    if (string.IsNullOrEmpty (s_basePath)) {
                        var obj = ScriptableObject.CreateInstance<SaveData> ();
                        MonoScript s = MonoScript.FromScriptableObject (obj);
                        var configGuiPath = AssetDatabase.GetAssetPath( s );
                        UnityEngine.Object.DestroyImmediate (obj);

                        var fileInfo = new FileInfo(configGuiPath);
                        var baseDir = fileInfo.Directory.Parent.Parent.Parent.Parent;

                        Assert.AreEqual (ToolDirName, baseDir.Name);

                        s_basePath = baseDir.ToString().Replace( '\\', '/');
                    }
                    return s_basePath;
                }
            }

            public const string ASSETS_PATH = "Assets/";

            /// <summary>
            /// Name of the base directory containing the asset graph tool files.
            /// Customize this to match your project's setup if you need to change.
            /// </summary>
            /// <value>The name of the base directory.</value>
            public static string ToolDirName            { get { return "UnityEngine.AssetBundleGraph"; } }
            public static string ScriptTemplatePath     { get { return BasePath + "Editor/ScriptTemplate/"; } }
            public static string SettingTemplatePath    { get { return BasePath + "Editor/SettingTemplate/"; } }
            public static string UserSpacePath          { get { return BasePath + "Generated/Editor/"; } }
            public static string CUISpacePath           { get { return BasePath + "Generated/CUI/"; } }
            public static string ImporterSettingsPath   { get { return BasePath + "SavedSettings/ImportSettings"; } }

            public static string CachePath              { get { return BasePath + "Cache/"; } }
            public static string PrefabBuilderCachePath { get { return CachePath + "Prefabs"; } }
            public static string BundleBuilderCachePath { get { return CachePath + "AssetBundles"; } }

            public static string SettingFilePath        { get { return BasePath + "SettingFiles"; } }
            public static string JSONPath               { get { return SettingFilePath + "AssetBundleGraph.json"; } }
            public static string AssetBundleGraphPath   { get { return SettingFilePath + "AssetBundleGraph.asset"; } }
            public static string DatabasePath           { get { return SettingFilePath + "AssetReferenceDB.asset"; } }

            public static string SettingTemplateModel   { get { return SettingTemplatePath + "setting.fbx"; } }
            public static string SettingTemplateAudio   { get { return SettingTemplatePath + "setting.wav"; } }
            public static string SettingTemplateTexture { get { return SettingTemplatePath + "setting.png"; } }

            public static string GUIResourceBasePath    { get { return BasePath + "Editor/GUI/GraphicResources/"; } }
        }

		public static List<BuildAssetBundleOption> BundleOptionSettings = new List<BuildAssetBundleOption> {
			new BuildAssetBundleOption("Uncompressed AssetBundle", BuildAssetBundleOptions.UncompressedAssetBundle),
			new BuildAssetBundleOption("Disable Write TypeTree", BuildAssetBundleOptions.DisableWriteTypeTree),
			new BuildAssetBundleOption("Deterministic AssetBundle", BuildAssetBundleOptions.DeterministicAssetBundle),
			new BuildAssetBundleOption("Force Rebuild AssetBundle", BuildAssetBundleOptions.ForceRebuildAssetBundle),
			new BuildAssetBundleOption("Ignore TypeTree Changes", BuildAssetBundleOptions.IgnoreTypeTreeChanges),
			new BuildAssetBundleOption("Append Hash To AssetBundle Name", BuildAssetBundleOptions.AppendHashToAssetBundleName),
			new BuildAssetBundleOption("ChunkBased Compression", BuildAssetBundleOptions.ChunkBasedCompression),
			new BuildAssetBundleOption("Strict Mode", BuildAssetBundleOptions.StrictMode)
			#if !UNITY_5_5_OR_NEWER
			,
      		// UnityEditor.BuildAssetBundleOptions does no longer have OmitClassVersions available
			new BuildAssetBundleOption("Omit Class Versions", BuildAssetBundleOptions.OmitClassVersions)
			#endif
		};

		//public const string PLATFORM_DEFAULT_NAME = "Default";
		//public const string PLATFORM_STANDALONE = "Standalone";

		public const float WINDOW_SPAN = 20f;

		/*
			node generation from GUI
		*/
		public const string MENU_LOADER_NAME = "Loader";
		public const string MENU_FILTER_NAME = "Filter";
		public const string MENU_IMPORTSETTING_NAME = "ImportSetting";
		public const string MENU_MODIFIER_NAME = "Modifier";
		public const string MENU_GROUPING_NAME = "Grouping";
		public const string MENU_PREFABBUILDER_NAME = "PrefabBuilder";
		public const string MENU_BUNDLECONFIG_NAME = "BundleConfig";
		public const string MENU_BUNDLEBUILDER_NAME = "BundleBuilder";
		public const string MENU_EXPORTER_NAME = "Exporter";

		public static Dictionary<NodeKind, string> DEFAULT_NODE_NAME = new Dictionary<NodeKind, string>{
			{NodeKind.LOADER_GUI, "Loader"},
			{NodeKind.FILTER_GUI, "Filter"},
			{NodeKind.IMPORTSETTING_GUI, "ImportSetting"},
			{NodeKind.MODIFIER_GUI, "Modifier"},
			{NodeKind.GROUPING_GUI, "Grouping"},
			{NodeKind.PREFABBUILDER_GUI, "PrefabBuilder"},
			{NodeKind.BUNDLECONFIG_GUI, "BundleConfig"},
			{NodeKind.BUNDLEBUILDER_GUI, "BundleBuilder"},
			{NodeKind.EXPORTER_GUI, "Exporter"}
		};

		/*
			data key for AssetBundleGraph.json
		*/

		public const string GROUPING_KEYWORD_DEFAULT = "/Group_*/";
		public const string BUNDLECONFIG_BUNDLENAME_TEMPLATE_DEFAULT = "bundle_*";

		// by default, AssetBundleGraph's node has only 1 InputPoint. and 
		// this is only one definition of it's label.
		public const string DEFAULT_INPUTPOINT_LABEL = "-";
		public const string DEFAULT_OUTPUTPOINT_LABEL = "+";
		public const string BUNDLECONFIG_BUNDLE_OUTPUTPOINT_LABEL = "bundles";
		public const string BUNDLECONFIG_VARIANTNAME_DEFAULT = "";

		public const string DEFAULT_FILTER_KEYWORD = "";
		public const string DEFAULT_FILTER_KEYTYPE = "Any";

		public const string FILTER_KEYWORD_WILDCARD = "*";

		public const string NODE_INPUTPOINT_FIXED_LABEL = "FIXED_INPUTPOINT_ID";

		public static NodeKind NodeKindFromString (string val) {
			return (NodeKind)Enum.Parse(typeof(NodeKind), val);
		}

		public class GUI {

			public const float NODE_BASE_WIDTH = 120f;
			public const float NODE_BASE_HEIGHT = 40f;
			public const float NODE_WIDTH_MARGIN = 48f;
			public const float NODE_TITLE_HEIGHT_MARGIN = 8f;

			public const float CONNECTION_ARROW_WIDTH = 12f;
			public const float CONNECTION_ARROW_HEIGHT = 15f;

			public const float INPUT_POINT_WIDTH = 21f;
			public const float INPUT_POINT_HEIGHT = 29f;

			public const float OUTPUT_POINT_WIDTH = 10f;
			public const float OUTPUT_POINT_HEIGHT = 23f;

			public const float FILTER_OUTPUT_SPAN = 32f;

			public const float CONNECTION_POINT_MARK_SIZE = 19f;

			public const float CONNECTION_CURVE_LENGTH = 10f;

			public const float TOOLBAR_HEIGHT = 20f;

            public static string Arrow                          { get { return Path.GUIResourceBasePath + "AssetGraph_Arrow.png"; } }
            public static string ConnectionPointEnable          { get { return Path.GUIResourceBasePath + "AssetGraph_ConnectionPoint_EnableMark.png"; } }
            public static string ConnectionPointInput           { get { return Path.GUIResourceBasePath + "AssetGraph_ConnectionPoint_InputMark.png"; } }
            public static string ConnectionPointOutput          { get { return Path.GUIResourceBasePath + "AssetGraph_ConnectionPoint_OutputMark.png"; } }
            public static string ConnectionPointOutputConnected { get { return Path.GUIResourceBasePath + "AssetGraph_ConnectionPoint_OutputMark_Connected.png"; } }
            public static string InputBG                        { get { return Path.GUIResourceBasePath + "AssetGraph_InputBG.png"; } }
            public static string OutputBG                       { get { return Path.GUIResourceBasePath + "AssetGraph_OutputBG.png"; } }
            public static string Selection                      { get { return Path.GUIResourceBasePath + "AssetGraph_Selection.png"; } }
		}
	}
}
