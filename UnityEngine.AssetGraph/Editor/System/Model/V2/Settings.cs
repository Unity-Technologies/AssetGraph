using System;
using System.Collections.Generic;
using UnityEditor;
using System.IO;

using UnityEngine.AssetBundles.GraphTool;

namespace UnityEngine.AssetBundles.GraphTool.DataModel.Version2 {
	public class Settings {
		/*
			if true, ignore .meta files inside AssetBundleGraph.
		*/
		public const bool IGNORE_META = true;

		public const string GUI_TEXT_MENU_OPEN = "Window/AssetBundleGraph/Open Graph Editor";
        public const string GUI_TEXT_MENU_BATCHWINDOW_OPEN = "Window/AssetBundleGraph/Open Batch Build Window";
        public const string GUI_TEXT_MENU_PROJECTWINDOW_OPEN = "Window/AssetBundleGraph/Open Project Window";
		public const string GUI_TEXT_MENU_BUILD = "Window/AssetBundleGraph/Build Bundles for Current Platform";
		public const string GUI_TEXT_MENU_BATCHBUILD = "Window/AssetBundleGraph/Build Current Graph Selections";
		public const string GUI_TEXT_MENU_GENERATE = "Window/AssetBundleGraph/Create Node Script";
		public const string GUI_TEXT_MENU_GENERATE_MODIFIER = GUI_TEXT_MENU_GENERATE + "/Modifier Script";
        public const string GUI_TEXT_MENU_GENERATE_PREFABBUILDER = GUI_TEXT_MENU_GENERATE + "/PrefabBuilder Script";
        public const string GUI_TEXT_MENU_GENERATE_ASSETGENERATOR = GUI_TEXT_MENU_GENERATE + "/AssetGenerator Script";
		public const string GUI_TEXT_MENU_GENERATE_CUITOOL = "Window/AssetBundleGraph/Create CUI Tool";

		public const string GUI_TEXT_MENU_GENERATE_POSTPROCESS = GUI_TEXT_MENU_GENERATE + "/Postprocess Script";
		public const string GUI_TEXT_MENU_GENERATE_FILTER = GUI_TEXT_MENU_GENERATE + "/Filter Script";
		public const string GUI_TEXT_MENU_GENERATE_NODE = GUI_TEXT_MENU_GENERATE + "/Custom Node Script";
		public const string GUI_TEXT_MENU_DELETE_CACHE = "Window/AssetBundleGraph/Clear Build Cache";
		
		public const string GUI_TEXT_MENU_DELETE_IMPORTSETTING_SETTINGS = "Window/AssetBundleGraph/Clear Saved ImportSettings";

		public const string GRAPH_SEARCH_CONDITION = "t:UnityEngine.AssetBundles.GraphTool.DataModel.Version2.ConfigGraph";

		public const string GUI_TEXT_SETTINGTEMPLATE_MODEL	= "Model";
		public const string GUI_TEXT_SETTINGTEMPLATE_AUDIO	= "Audio";
		public const string GUI_TEXT_SETTINGTEMPLATE_TEXTURE= "Texture";
		public const string GUI_TEXT_SETTINGTEMPLATE_VIDEO	= "Video";

		public const string UNITY_METAFILE_EXTENSION = ".meta";
		public const string DOTSTART_HIDDEN_FILE_HEADSTRING = ".";
		public const string MANIFEST_FOOTER = ".manifest";
		public const char UNITY_FOLDER_SEPARATOR = '/';// Mac/Windows/Linux can use '/' in Unity.

		public const string BASE64_IDENTIFIER = "B64|";

		public const char KEYWORD_WILDCARD = '*';

        public class UserSettings {
            private static readonly string PREFKEY_AB_BUILD_CACHE_DIR = "AssetBundles.GraphTool.Cache.AssetBundle";

            private static readonly string PREFKEY_BATCHBUILD_LASTSELECTEDCOLLECTION = "AssetBundles.GraphTool.LastSelectedCollection";
            private static readonly string PREFKEY_BATCHBUILD__USECOLLECTIONSTATE    = "AssetBundles.GraphTool.UseCollection";

            public static string AssetBundleBuildCacheDir {
                get {
                    var cacheDir = EditorUserSettings.GetConfigValue (PREFKEY_AB_BUILD_CACHE_DIR);
                    if (string.IsNullOrEmpty (cacheDir)) {
                        return System.IO.Path.Combine(Path.CachePath, "AssetBundles");
                    }
                    return cacheDir;
                }

                set {
                    
                    EditorUserSettings.SetConfigValue (PREFKEY_AB_BUILD_CACHE_DIR, value);
                }
            }

            public static string BatchBuildLastSelectedCollection {
                get {
                    return EditorUserSettings.GetConfigValue (PREFKEY_BATCHBUILD_LASTSELECTEDCOLLECTION);
                }
                set {
                    EditorUserSettings.SetConfigValue (PREFKEY_BATCHBUILD_LASTSELECTEDCOLLECTION, value);
                }
            }

            public static bool BatchBuildUseCollectionState {
                get {
                    return EditorUserSettings.GetConfigValue (PREFKEY_BATCHBUILD__USECOLLECTIONSTATE) == "True";
                }
                set {
                    EditorUserSettings.SetConfigValue (PREFKEY_BATCHBUILD__USECOLLECTIONSTATE, value.ToString());
                }
            }
        }

        public class Path {
            private static string s_basePath;

            public static string BasePath {
                get {
                    //if (string.IsNullOrEmpty (s_basePath)) {
                        var obj = ScriptableObject.CreateInstance<ConfigGraph> ();
                        MonoScript s = MonoScript.FromScriptableObject (obj);
                        var configGuiPath = AssetDatabase.GetAssetPath( s );
                        UnityEngine.Object.DestroyImmediate (obj);

                        var fileInfo = new FileInfo(configGuiPath);
                        var baseDir = fileInfo.Directory.Parent.Parent.Parent.Parent;

                        Assertions.Assert.AreEqual (ToolDirName, baseDir.Name);

						string baseDirPath = baseDir.ToString ().Replace( '\\', '/');

                        int index = baseDirPath.LastIndexOf (ASSETS_PATH);
                        Assertions.Assert.IsTrue ( index >= 0 );

						s_basePath = baseDirPath.Substring (index);
                    //}
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

            public static string ScriptTemplatePath     { get { return System.IO.Path.Combine(BasePath, "Editor/ScriptTemplate"); } }
            public static string UserSpacePath          { get { return System.IO.Path.Combine(BasePath, "Generated/Editor"); } }
            public static string CUISpacePath           { get { return System.IO.Path.Combine(BasePath, "Generated/CUI"); } }
            public static string ImporterSettingsPath   { get { return System.IO.Path.Combine(BasePath, "SavedSettings/ImportSettings"); } }

            public static string CachePath              { get { return System.IO.Path.Combine(BasePath, "Cache"); } }
            public static string PrefabBuilderCachePath { get { return System.IO.Path.Combine(CachePath, "Prefabs"); } }
            public static string AssetGeneratorCachePath { get { return System.IO.Path.Combine(CachePath, "GeneratedAssets"); } }
            public static string GroupingCachePath      { get { return System.IO.Path.Combine(CachePath, "Grouping"); } }
            public static string BundleBuilderCachePath { get { return UserSettings.AssetBundleBuildCacheDir; } }

            public static string SettingFilePath        { get { return System.IO.Path.Combine(BasePath, "SettingFiles"); } }
            public static string DatabasePath           { get { return System.IO.Path.Combine(SettingFilePath, "AssetReferenceDB.asset"); } }
            public static string BuildMapPath           { get { return System.IO.Path.Combine(SettingFilePath, "AssetBundleBuildMap.asset"); } }
            public static string BatchBuildConfigPath   { get { return System.IO.Path.Combine(SettingFilePath, "BatchBuildConfig.asset"); } }

            public static string SettingTemplatePath    { get { return System.IO.Path.Combine(BasePath, "Editor/SettingTemplate"); } }
            public static string SettingTemplateModel   { get { return System.IO.Path.Combine(SettingTemplatePath, "setting.fbx"); } }
            public static string SettingTemplateAudio   { get { return System.IO.Path.Combine(SettingTemplatePath, "setting.wav"); } }
            public static string SettingTemplateTexture { get { return System.IO.Path.Combine(SettingTemplatePath, "setting.png"); } }
            public static string SettingTemplateVideo   { get { return System.IO.Path.Combine(SettingTemplatePath, "setting.m4v"); } }

                public static string GUIResourceBasePath { get { return System.IO.Path.Combine(BasePath, "Editor/GUI/GraphicResources"); } }
        }

		public struct BuildAssetBundleOption {
			public readonly BuildAssetBundleOptions option;
			public readonly string description;
			public BuildAssetBundleOption(string desc, BuildAssetBundleOptions opt) {
				option = opt;
				description = desc;
			}
		}

        public struct BuildPlayerOption {
            public readonly BuildOptions option;
            public readonly string description;
            public BuildPlayerOption(string desc, BuildOptions opt) {
                option = opt;
                description = desc;
            }
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

        public static List<BuildPlayerOption> BuildPlayerOptionsSettings = new List<BuildPlayerOption> {
            new BuildPlayerOption("Accept External Modification To Player", BuildOptions.AcceptExternalModificationsToPlayer),
            new BuildPlayerOption("Allow Debugging", BuildOptions.AllowDebugging),
            new BuildPlayerOption("Auto Run Player", BuildOptions.AutoRunPlayer),
            new BuildPlayerOption("Build Additional Streamed Scenes", BuildOptions.BuildAdditionalStreamedScenes),
            new BuildPlayerOption("Build Scripts Only", BuildOptions.BuildScriptsOnly),
            #if UNITY_5_6_OR_NEWER
            new BuildPlayerOption("Compress With LZ4", BuildOptions.CompressWithLz4),
            #endif
            new BuildPlayerOption("Compute CRC", BuildOptions.ComputeCRC),
            new BuildPlayerOption("Connect To Host", BuildOptions.ConnectToHost),
            new BuildPlayerOption("Connect With Profiler", BuildOptions.ConnectWithProfiler),
            new BuildPlayerOption("Development Build", BuildOptions.Development),
            new BuildPlayerOption("Enable Headless Mode", BuildOptions.EnableHeadlessMode),
            new BuildPlayerOption("Force Enable Assertions", BuildOptions.ForceEnableAssertions),
            #if !UNITY_2017_1_OR_NEWER
            new BuildPlayerOption("Force Optimize Script Compilation", BuildOptions.ForceOptimizeScriptCompilation),
            #endif
            new BuildPlayerOption("Use IL2CPP", BuildOptions.Il2CPP),
            new BuildPlayerOption("Install In Build Folder", BuildOptions.InstallInBuildFolder),
            new BuildPlayerOption("Show Built Player", BuildOptions.ShowBuiltPlayer),
            new BuildPlayerOption("Strict Mode", BuildOptions.StrictMode),
            new BuildPlayerOption("Symlink Libraries", BuildOptions.SymlinkLibraries),
            new BuildPlayerOption("Uncompressed AssetBundle", BuildOptions.UncompressedAssetBundle)
		};

		public const float WINDOW_SPAN = 20f;

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

			public const float CONNECTION_POINT_MARK_SIZE = 16f;

			public const float CONNECTION_CURVE_LENGTH = 20f;

			public const float TOOLBAR_HEIGHT = 20f;
			public const float TOOLBAR_GRAPHNAMEMENU_WIDTH = 150f;
			public const int TOOLBAR_GRAPHNAMEMENU_CHAR_LENGTH = 20;

			public static readonly Color COLOR_ENABLED = new Color(0.43f, 0.65f, 1.0f, 1.0f);
			public static readonly Color COLOR_CONNECTED = new Color(0.9f, 0.9f, 0.9f, 1.0f);
			public static readonly Color COLOR_NOT_CONNECTED = Color.grey;
			public static readonly Color COLOR_CAN_CONNECT = Color.white;//new Color(0.60f, 0.60f, 1.0f, 1.0f);
			public static readonly Color COLOR_CAN_NOT_CONNECT = new Color(0.33f, 0.33f, 0.33f, 1.0f);

            public static string Skin               { get { return System.IO.Path.Combine(Path.GUIResourceBasePath, "NodeStyle.guiskin"); } }
            public static string ConnectionPoint    { get { return System.IO.Path.Combine(Path.GUIResourceBasePath, "ConnectionPoint.png"); } }
            public static string InputBG            { get { return System.IO.Path.Combine(Path.GUIResourceBasePath, "InputBG.png"); } }
            public static string OutputBG           { get { return System.IO.Path.Combine(Path.GUIResourceBasePath, "OutputBG.png"); } }
		}
	}
}
