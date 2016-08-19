using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;


namespace AssetBundleGraph {
	[Serializable] 
	public class Node {

		private class NodeSingleton {
			public Action<OnNodeEvent> emitAction;

			public Texture2D inputPointTex;
			public Texture2D outputPointTex;

			public Texture2D enablePointMarkTex;

			public Texture2D inputPointMarkTex;
			public Texture2D outputPointMarkTex;
			public Texture2D outputPointMarkConnectedTex;
			public Texture2D[] platformButtonTextures;
			public string[] platformStrings;

			public List<string> allNodeNames;

			private static NodeSingleton s_singleton;

			public static NodeSingleton s {
				get {
					if( s_singleton == null ) {
						s_singleton = new NodeSingleton();
					}

					return s_singleton;
				}
			}

			public void SetupPlatformIcons () {
				var assetBundleGraphPlatformSettings = AssetBundleGraphPlatformSettings.platforms;

				var platformTexList = new List<Texture2D>();

				platformTexList.Add(GetPlatformIcon("BuildSettings.Web"));//dummy.

				if (assetBundleGraphPlatformSettings.Contains("Web")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.Web"));
				}
				if (assetBundleGraphPlatformSettings.Contains("Standalone")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.Standalone"));
				}
				if (assetBundleGraphPlatformSettings.Contains("iPhone") || assetBundleGraphPlatformSettings.Contains("iOS")) {// iPhone or iOS converted to iOS.
					platformTexList.Add(GetPlatformIcon("BuildSettings.iPhone"));
				}
				if (assetBundleGraphPlatformSettings.Contains("Android")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.Android"));
				}
				if (assetBundleGraphPlatformSettings.Contains("BlackBerry")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.BlackBerry"));
				}
				if (assetBundleGraphPlatformSettings.Contains("Tizen")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.Tizen"));
				}
				if (assetBundleGraphPlatformSettings.Contains("XBox360")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.XBox360"));
				}
				if (assetBundleGraphPlatformSettings.Contains("XboxOne")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.XboxOne"));
				}
				if (assetBundleGraphPlatformSettings.Contains("PS3")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.PS3"));
				}
				if (assetBundleGraphPlatformSettings.Contains("PSP2")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.PSP2"));
				}
				if (assetBundleGraphPlatformSettings.Contains("PS4")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.PS4"));
				}
				if (assetBundleGraphPlatformSettings.Contains("StandaloneGLESEmu")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.StandaloneGLESEmu"));
				}
				if (assetBundleGraphPlatformSettings.Contains("Metro")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.Metro"));
				}
				if (assetBundleGraphPlatformSettings.Contains("WP8")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.WP8"));
				}
				if (assetBundleGraphPlatformSettings.Contains("WebGL")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.WebGL"));
				}
				if (assetBundleGraphPlatformSettings.Contains("SamsungTV")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.SamsungTV"));
				}

				platformButtonTextures = platformTexList.ToArray();
			}


			public void SetupPlatformStrings () {
				var assetBundleGraphPlatformSettings = AssetBundleGraphPlatformSettings.platforms;

				var platformStringList = new List<string>();

				platformStringList.Add("Default");

				if (assetBundleGraphPlatformSettings.Contains("Web")) {
					platformStringList.Add("Web");
				}
				if (assetBundleGraphPlatformSettings.Contains("Standalone")) {
					platformStringList.Add("Standalone");
				}
				if (assetBundleGraphPlatformSettings.Contains("iPhone") || assetBundleGraphPlatformSettings.Contains("iOS")) {// iPhone or iOS converted to iOS.
					platformStringList.Add("iOS");
				}
				if (assetBundleGraphPlatformSettings.Contains("Android")) {
					platformStringList.Add("Android");
				}
				if (assetBundleGraphPlatformSettings.Contains("BlackBerry")) {
					platformStringList.Add("BlackBerry");
				}
				if (assetBundleGraphPlatformSettings.Contains("Tizen")) {
					platformStringList.Add("Tizen");
				}
				if (assetBundleGraphPlatformSettings.Contains("XBox360")) {
					platformStringList.Add("XBox360");
				}
				if (assetBundleGraphPlatformSettings.Contains("XboxOne")) {
					platformStringList.Add("XboxOne");
				}
				if (assetBundleGraphPlatformSettings.Contains("PS3")) {
					platformStringList.Add("PS3");
				}
				if (assetBundleGraphPlatformSettings.Contains("PSP2")) {
					platformStringList.Add("PSP2");
				}
				if (assetBundleGraphPlatformSettings.Contains("PS4")) {
					platformStringList.Add("PS4");
				}
				if (assetBundleGraphPlatformSettings.Contains("StandaloneGLESEmu")) {
					platformStringList.Add("StandaloneGLESEmu");
				}
				if (assetBundleGraphPlatformSettings.Contains("Metro")) {
					platformStringList.Add("Metro");
				}
				if (assetBundleGraphPlatformSettings.Contains("WP8")) {
					platformStringList.Add("WP8");
				}
				if (assetBundleGraphPlatformSettings.Contains("WebGL")) {
					platformStringList.Add("WebGL");
				}
				if (assetBundleGraphPlatformSettings.Contains("SamsungTV")) {
					platformStringList.Add("SamsungTV");
				}

				platformStrings = platformStringList.ToArray();
			}

			private Texture2D GetPlatformIcon(string locTitle) {
				return EditorGUIUtility.IconContent(locTitle + ".Small").image as Texture2D;
			}
		}

		public static bool EnsureInitialized() {
			return NodeSingleton.s != null;
		}


		public static Action<OnNodeEvent> Emit {
			get {
				return NodeSingleton.s.emitAction;
			}
			set {
				NodeSingleton.s.emitAction = value;
			}
		}

		public static Texture2D inputPointTex {
			get {
				if(NodeSingleton.s.inputPointTex == null) {
					NodeSingleton.s.inputPointTex = AssetBundleGraph.LoadTextureFromFile(AssetBundleGraphGUISettings.RESOURCE_INPUT_BG);
				}
//				Debug.Log("NodeSingleton.s.inputPointTex : " + NodeSingleton.s.inputPointTex);
				return NodeSingleton.s.inputPointTex;
			}
		}

		public static Texture2D outputPointTex {
			get {
				if(NodeSingleton.s.outputPointTex == null) {
					NodeSingleton.s.outputPointTex = AssetBundleGraph.LoadTextureFromFile(AssetBundleGraphGUISettings.RESOURCE_OUTPUT_BG);
				}
//				Debug.Log("NodeSingleton.s.outputPointTex : " + NodeSingleton.s.outputPointTex);
				return NodeSingleton.s.outputPointTex;
			}
		}

		public static Texture2D enablePointMarkTex {
			get {
				if(NodeSingleton.s.enablePointMarkTex == null) {
					NodeSingleton.s.enablePointMarkTex = AssetBundleGraph.LoadTextureFromFile(AssetBundleGraphGUISettings.RESOURCE_CONNECTIONPOINT_ENABLE);
				}
//				Debug.Log("NodeSingleton.s.enablePointMarkTex : " + NodeSingleton.s.enablePointMarkTex);
				return NodeSingleton.s.enablePointMarkTex;
			}
		}

		public static Texture2D inputPointMarkTex {
			get {
				if(NodeSingleton.s.inputPointMarkTex == null) {
					NodeSingleton.s.inputPointMarkTex = AssetBundleGraph.LoadTextureFromFile(AssetBundleGraphGUISettings.RESOURCE_CONNECTIONPOINT_INPUT);
				}

//				Debug.Log("NodeSingleton.s.inputPointMarkTex : " + NodeSingleton.s.inputPointMarkTex);
				return NodeSingleton.s.inputPointMarkTex;
			}
		}

		public static Texture2D outputPointMarkTex {
			get {
				if(NodeSingleton.s.outputPointMarkTex == null) {
					NodeSingleton.s.outputPointMarkTex = AssetBundleGraph.LoadTextureFromFile(AssetBundleGraphGUISettings.RESOURCE_CONNECTIONPOINT_OUTPUT);
				}
				return NodeSingleton.s.outputPointMarkTex;
			}
		}

		public static Texture2D outputPointMarkConnectedTex {
			get {
				if(NodeSingleton.s.outputPointMarkConnectedTex == null) {
					NodeSingleton.s.outputPointMarkConnectedTex = AssetBundleGraph.LoadTextureFromFile(AssetBundleGraphGUISettings.RESOURCE_CONNECTIONPOINT_OUTPUT_CONNECTED);
				}
				return NodeSingleton.s.outputPointMarkConnectedTex;
			}
		}

		public static Texture2D[] platformButtonTextures {
			get {
				if(NodeSingleton.s.platformButtonTextures == null) {
					NodeSingleton.s.SetupPlatformIcons();
				}
				return NodeSingleton.s.platformButtonTextures;
			}
		}

		public static string[] platformStrings {
			get {
				if(NodeSingleton.s.platformStrings == null) {
					NodeSingleton.s.SetupPlatformStrings();
				}
				return NodeSingleton.s.platformStrings;
			}
		}

		public static List<string> allNodeNames {
			get {
				return NodeSingleton.s.allNodeNames;
			}
			set {
				NodeSingleton.s.allNodeNames = value;
			}
		}

		public static float scaleFactor = 1.0f;// 1.0f. 0.7f, 0.4f, 0.3f
		public const float SCALE_MIN = 0.3f;
		public const float SCALE_MAX = 1.0f;
		public const int SCALE_WIDTH = 30;
		public const float SCALE_RATIO = 0.3f;

		[SerializeField] private List<ConnectionPoint> connectionPoints = new List<ConnectionPoint>();

        [SerializeField] private int nodeWindowId;
		[SerializeField] private Rect baseRect;

		[SerializeField] public string name;
		[SerializeField] public string nodeId;
		[SerializeField] public AssetBundleGraphSettings.NodeKind kind;

		[SerializeField] public string scriptClassName;
		[SerializeField] public string scriptPath;
		[SerializeField] public SerializablePseudoDictionary loadPath;
		[SerializeField] public SerializablePseudoDictionary exportPath;
		[SerializeField] public List<string> filterContainsKeywords;
		[SerializeField] public List<string> filterContainsKeytypes;
		[SerializeField] public SerializablePseudoDictionary importerPackages;
		[SerializeField] public SerializablePseudoDictionary modifierPackages;
		[SerializeField] public SerializablePseudoDictionary groupingKeyword;
		[SerializeField] public SerializablePseudoDictionary bundleNameTemplate;
		[SerializeField] public SerializablePseudoDictionary bundleUseOutput;
		[SerializeField] public SerializablePseudoDictionary2 enabledBundleOptions;
		
		// for platform-package specified parameter.
		[SerializeField] public string currentPlatform = AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME;
		
		public static List<string> NodeSharedPackages = new List<string>();

		[SerializeField] private string nodeInterfaceTypeStr;
		[SerializeField] private BuildTarget currentBuildTarget;

		[SerializeField] private NodeGUIInfo nodeInsp;
		
		/*
			show error on node functions.
		*/
		private bool hasErrors = false;

        public void RenewErrorSource () {
            hasErrors = false;
			this.nodeInsp.UpdateNode(this);
			this.nodeInsp.UpdateErrors(new List<string>());
        }
		public void AppendErrorSources (List<string> errors) {
			this.hasErrors = true;
			this.nodeInsp.UpdateNode(this);
			this.nodeInsp.UpdateErrors(errors);
		}

		/*
			show progress on node functions(unused. due to mainthread synchronization problem.)
			can not update any visual on Editor while building AssetBundles through AssetBundleGraph.
		*/
		private float progress;
		private bool running;

		public static Node CreateLoaderNode (int index, string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, Dictionary<string, string> loadPath, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				loadPath: loadPath
			);
		}

		public static Node CreateExporterNode (int index, string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, Dictionary<string, string> exportPath, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				exportPath: exportPath
			);
		}

		public static Node CreateScriptNode (int index, string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, string scriptClassName, string scriptPath, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				scriptClassName: scriptClassName,
				scriptPath: scriptPath
			);
		}

		public static Node CreateGUIFilterNode (int index, string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, List<string> filterContainsKeywords, List<string> filterContainsKeytypes, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				filterContainsKeywords: filterContainsKeywords,
				filterContainsKeytypes: filterContainsKeytypes
			);
		}

		public static Node CreateGUIImportNode (int index, string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, Dictionary<string, string> importerPackages, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				importerPackages: importerPackages
			);
		}
		
		public static Node CreateGUIGroupingNode (int index, string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, Dictionary<string, string> groupingKeyword, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				groupingKeyword: groupingKeyword
			);
		}

		public static Node CreatePrefabricatorNode (int index, string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y
			);
		}

		public static Node CreateBundlizerNode (int index, string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, Dictionary<string, string> bundleNameTemplate, Dictionary<string, string> bundleUseOutput, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				bundleNameTemplate: bundleNameTemplate,
				bundleUseOutput: bundleUseOutput
			);
		}

		public static Node CreateBundleBuilderNode (int index, string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, Dictionary<string, List<string>> enabledBundleOptions, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				enabledBundleOptions: enabledBundleOptions
			);
		}

		public void AddFilterOutputPoint (int addedIndex, string keyword) {
			connectionPoints.Insert(addedIndex, ConnectionPoint.OutputPoint(Guid.NewGuid().ToString(), keyword));
			Save();
			UpdateNodeRect();
		}

		public void DeleteFilterOutputPoint (int deletedIndex) {
			var deletedConnectionPoint = connectionPoints[deletedIndex];
			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED, this, Vector2.zero, deletedConnectionPoint.pointId));
			connectionPoints.RemoveAt(deletedIndex);
			Save();
			UpdateNodeRect();
		}

		public void RenameFilterOutputPointLabel (int changedIndex, string latestLabel) {
			connectionPoints[changedIndex].label = latestLabel;
			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_LABELCHANGED, this, Vector2.zero, connectionPoints[changedIndex].pointId));
			Save();
			UpdateNodeRect();
		}
		
		
		
		public void AddBundlizerDependencyOutput () {
			var outputResurceLabelIndex = connectionPoints.FindIndex(p => p.label == AssetBundleGraphSettings.BUNDLIZER_DEPENDENCY_OUTPUTPOINT_LABEL);
			if (outputResurceLabelIndex != -1) return;
			
			connectionPoints.Add(ConnectionPoint.OutputPoint(Guid.NewGuid().ToString(), AssetBundleGraphSettings.BUNDLIZER_DEPENDENCY_OUTPUTPOINT_LABEL));
			UpdateNodeRect();
		}
		
		public void RemoveBundlizerDependencyOutput () {
			Debug.LogError("RemoveBundlizerDependencyOutput これ発生したあとに、Undoするとポイントが増えてる気がする");
			var outputResurceLabelIndex = connectionPoints.FindIndex(p => p.label == AssetBundleGraphSettings.BUNDLIZER_DEPENDENCY_OUTPUTPOINT_LABEL);
			if (outputResurceLabelIndex == -1) return;
			
			var deletedConnectionPoint = connectionPoints[outputResurceLabelIndex];
			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED, this, Vector2.zero, deletedConnectionPoint.pointId));
			connectionPoints.RemoveAt(outputResurceLabelIndex);
			UpdateNodeRect();
			Debug.LogError("調整はできてる。");
		}

		public void BeforeSave () {
			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_BEFORESAVE, this, Vector2.zero, null));
		}

		/**
			node's setting is changed from Inspector.
		*/
		public void Save () {
			/*
				update as no errors.
			*/
			RenewErrorSource();

			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_SAVE, this, Vector2.zero, null));
		}

		public Node () {}

		private Node (
			int index, 
			string name, 
			string nodeId, 
			AssetBundleGraphSettings.NodeKind kind, 
			float x, 
			float y,
			string scriptClassName = null, 
			string scriptPath = null, 
			Dictionary<string, string> loadPath = null, 
			Dictionary<string, string> exportPath = null, 
			List<string> filterContainsKeywords = null, 
			List<string> filterContainsKeytypes = null, 
			Dictionary<string, string> importerPackages = null,
			Dictionary<string, string> modifierPackages = null,
			Dictionary<string, string> groupingKeyword = null,
			Dictionary<string, string> bundleNameTemplate = null,
			Dictionary<string, string> bundleUseOutput = null,
			Dictionary<string, List<string>> enabledBundleOptions = null
		) {
			this.nodeInsp = ScriptableObject.CreateInstance<NodeGUIInfo>();
			
			this.nodeWindowId = index;
			this.name = name;
			this.nodeId = nodeId;
			this.kind = kind;
			this.scriptClassName = scriptClassName;
			this.scriptPath = scriptPath;
			if (loadPath != null) this.loadPath = new SerializablePseudoDictionary(loadPath);
			if (exportPath != null) this.exportPath = new SerializablePseudoDictionary(exportPath);
			this.filterContainsKeywords = filterContainsKeywords;
			this.filterContainsKeytypes = filterContainsKeytypes;
			if (importerPackages != null) this.importerPackages = new SerializablePseudoDictionary(importerPackages);
			if (modifierPackages != null) this.modifierPackages = new SerializablePseudoDictionary(modifierPackages);
			if (groupingKeyword != null) this.groupingKeyword = new SerializablePseudoDictionary(groupingKeyword);
			if (bundleNameTemplate != null) this.bundleNameTemplate = new SerializablePseudoDictionary(bundleNameTemplate);
			if (bundleUseOutput != null) this.bundleUseOutput = new SerializablePseudoDictionary(bundleUseOutput);
			if (enabledBundleOptions != null) this.enabledBundleOptions = new SerializablePseudoDictionary2(enabledBundleOptions);
			
			this.baseRect = new Rect(x, y, AssetBundleGraphGUISettings.NODE_BASE_WIDTH, AssetBundleGraphGUISettings.NODE_BASE_HEIGHT);
			
			switch (this.kind) {
				case AssetBundleGraphSettings.NodeKind.LOADER_GUI:
				case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 0";
					break;
				}

				case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 1";
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 2";
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 3";
					break;
				}

				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI:
				 {
					this.nodeInterfaceTypeStr = "flow node 4";
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 5";
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6";
					break;
				}

				default: {
					Debug.LogError(this.name + " is defined as unknown kind of node. value:" + this.kind);
					break;
				}
			}
		}

		public Node DuplicatedNode (int newIndex, float newX, float newY) {
			var duplicatedNode = new Node(
				newIndex,
				this.name,
				Guid.NewGuid().ToString(),
				this.kind, 
				newX,
				newY,
				this.scriptClassName,
				this.scriptPath,
				(this.loadPath != null) ? loadPath.ReadonlyDict() : null,
				(this.exportPath != null) ? this.exportPath.ReadonlyDict() : null,
				this.filterContainsKeywords,
				this.filterContainsKeytypes,
				(this.importerPackages != null) ? this.importerPackages.ReadonlyDict() : null,
				(this.modifierPackages != null) ? this.modifierPackages.ReadonlyDict() : null,
				(this.groupingKeyword != null) ? this.groupingKeyword.ReadonlyDict() : null,
				(this.bundleNameTemplate != null) ? this.bundleNameTemplate.ReadonlyDict() : null,
				(this.bundleUseOutput != null) ? this.bundleUseOutput.ReadonlyDict() : null,
				(this.enabledBundleOptions != null) ? this.enabledBundleOptions.ReadonlyDict() : null
			);
			return duplicatedNode;
		}

		public void DeleteCurrentPackagePlatformKey (string platformPackageKey) {
			switch (this.kind) {
				case AssetBundleGraphSettings.NodeKind.LOADER_GUI: {
					loadPath.Remove(platformPackageKey);
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
					importerPackages.Remove(platformPackageKey);
					break;
				}

				case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
					groupingKeyword.Remove(platformPackageKey);
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					bundleNameTemplate.Remove(platformPackageKey);
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					enabledBundleOptions.Remove(platformPackageKey);
					break;
				}

				case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
					exportPath.Remove(platformPackageKey);
					break;
				}
				
				default: {
					Debug.LogError(this.name + " is defined as unknown kind of node. value:" + this.kind);
					break;
				}
			}
		}

		public void SetActive () {
			nodeInsp.UpdateNode(this);
			Selection.activeObject = nodeInsp;

			switch (this.kind) {
				case AssetBundleGraphSettings.NodeKind.LOADER_GUI:
				case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 0 on";
					break;
				}

				case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 1 on";
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 2 on";
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 3 on";
					break;
				}

				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI:
				 {
					this.nodeInterfaceTypeStr = "flow node 4 on";
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 5 on";
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6 on";
					break;
				}

				default: {
					Debug.LogError(this.name + " is defined as unknown kind of node. value:" + this.kind);
					break;
				}
			}
		}

		public void SetInactive () {
			switch (this.kind) {
				case AssetBundleGraphSettings.NodeKind.LOADER_GUI:
				case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 0";
					break;
				}

				case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 1";
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 2";
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 3";
					break;
				}

				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI:
				 {
					this.nodeInterfaceTypeStr = "flow node 4";
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 5";
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6";
					break;
				}

				default: {
					Debug.LogError(this.name + " is defined as unknown kind of node. value:" + this.kind);
					break;
				}
			}
		}

		public void AddConnectionPoint (ConnectionPoint adding) {
			connectionPoints.Add(adding);
			UpdateNodeRect();
		}

		private void RefreshConnectionPos () {
			var inputPoints = connectionPoints.Where(p => p.isInput).ToList();
			var outputPoints = connectionPoints.Where(p => p.isOutput).ToList();

			for (int i = 0; i < inputPoints.Count; i++) {
				var point = inputPoints[i];
				point.UpdatePos(i, inputPoints.Count, baseRect.width, baseRect.height);
			}

			for (int i = 0; i < outputPoints.Count; i++) {
				var point = outputPoints[i];
				point.UpdatePos(i, outputPoints.Count, baseRect.width, baseRect.height);
			}
		}

		public List<string> OutputPointLabels () {
			return connectionPoints
						.Where(p => p.isOutput)
						.Select(p => p.label)
						.ToList();
		}
		
		public List<string> OutputPointIds () {
			return connectionPoints
						.Where(p => p.isOutput)
						.Select(p => p.pointId)
						.ToList();
		}

		public ConnectionPoint ConnectionPointFromConPointId (string pointId) {
			var targetPoints = connectionPoints.Where(con => con.pointId == pointId).ToList();
			return targetPoints[0];
		}
		
		public ConnectionPoint ConnectionPointFromLabel (string label) {
			var targetPoints = connectionPoints.Where(con => con.label == label).ToList();
			return targetPoints[0];
		}

		public void DrawNode () {
			var scaledBaseRect = ScaleEffect(baseRect);

			var movedRect = GUI.Window(nodeWindowId, scaledBaseRect, DrawThisNode, string.Empty, nodeInterfaceTypeStr);

			baseRect.position = baseRect.position + (movedRect.position - scaledBaseRect.position);
		}

		public static Rect ScaleEffect (Rect nonScaledRect) {
			var scaledRect = new Rect(nonScaledRect);
			scaledRect.x = scaledRect.x * scaleFactor;
			scaledRect.y = scaledRect.y * scaleFactor;
			scaledRect.width = scaledRect.width * scaleFactor;
			scaledRect.height = scaledRect.height * scaleFactor;
			return scaledRect;
		}

		public static Vector2 ScaleEffect (Vector2 nonScaledVector2) {
			var scaledVector2 = new Vector2(nonScaledVector2.x, nonScaledVector2.y);
			scaledVector2.x = scaledVector2.x * scaleFactor;
			scaledVector2.y = scaledVector2.y * scaleFactor;
			return scaledVector2;
		}

		private void DrawThisNode(int id) {
			HandleNodeEvent ();
			DrawNodeContents();
			GUI.DragWindow();
		}

		/**
			retrieve mouse events for this node in this AssetGraoh window.
		*/
		private void HandleNodeEvent () {
			switch (Event.current.type) {

				/*
					handling release of mouse drag from this node to another node.
					this node doesn't know about where the other node is. the master only knows.
					only emit event.
				*/
				case EventType.Ignore: {
					Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECTION_OVERED, this, Event.current.mousePosition, null));
					break;
				}

				/*
					handling drag.
				*/
				case EventType.MouseDrag: {
					Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_MOVING, this, Event.current.mousePosition, null));
					break;
				}

				/*
					check if the mouse-down point is over one of the connectionPoint in this node.
					then emit event.
				*/
				case EventType.MouseDown: {
					var result = IsOverConnectionPoint(connectionPoints, Event.current.mousePosition);

					if (!string.IsNullOrEmpty(result)) {
						if (scaleFactor == SCALE_MAX) Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECT_STARTED, this, Event.current.mousePosition, result));
						break;
					}
					break;
				}
			}

			/*
				retrieve mouse events for this node in|out of this AssetGraoh window.
			*/
			switch (Event.current.rawType) {
				case EventType.MouseUp: {
					// if mouse position is on the connection point, emit mouse raised event.
					foreach (var connectionPoint in connectionPoints) {
						var globalConnectonPointRect = new Rect(connectionPoint.buttonRect.x, connectionPoint.buttonRect.y, connectionPoint.buttonRect.width, connectionPoint.buttonRect.height);
						if (globalConnectonPointRect.Contains(Event.current.mousePosition)) {
							Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECTION_RAISED, this, Event.current.mousePosition, connectionPoint.pointId));
							return;
						}
					}

					Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_TOUCHED, this, Event.current.mousePosition, null));
					break;
				}
			}

			// draw & update connectionPoint button interface.
			if (scaleFactor == SCALE_MAX) {
				foreach (var point in connectionPoints) {
					switch (this.kind) {
						case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT:
						case AssetBundleGraphSettings.NodeKind.FILTER_GUI:
						case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
							var label = point.label;
							var labelRect = new Rect(point.buttonRect.x - baseRect.width, point.buttonRect.y - (point.buttonRect.height/2), baseRect.width, point.buttonRect.height*2);

							var style = EditorStyles.label;
							var defaultAlignment = style.alignment;
							style.alignment = TextAnchor.MiddleRight;
							GUI.Label(labelRect, label, style);
							style.alignment = defaultAlignment;
							break;
						}
					}


					if (point.isInput) {
						GUI.backgroundColor = Color.clear;
						GUI.Button(point.buttonRect, inputPointTex, "AnimationKeyframeBackground");
					}

					if (point.isOutput) {
						GUI.backgroundColor = Color.clear;
						GUI.Button(point.buttonRect, outputPointTex, "AnimationKeyframeBackground");
					}
				}
			}

			/*
				right click.
			*/
			if (scaleFactor == SCALE_MAX) {
				if (
					Event.current.type == EventType.ContextClick
					 || (Event.current.type == EventType.MouseUp && Event.current.button == 1)
				) {
					var menu = new GenericMenu();
					menu.AddItem(
						new GUIContent("Delete"),
						false, 
						() => {
							Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CLOSE_TAPPED, this, Vector2.zero, null));
						}
					);
					menu.ShowAsContext();
					Event.current.Use();
				}
			}
		}

		public void DrawConnectionInputPointMark (OnNodeEvent eventSource, bool justConnecting) {
			if (scaleFactor != SCALE_MAX) return;

			var defaultPointTex = inputPointMarkTex;

			if (justConnecting && eventSource != null) {
				if (eventSource.eventSourceNode.nodeId != this.nodeId) {
					var connectionPoint = eventSource.eventSourceNode.ConnectionPointFromConPointId(eventSource.conPointId);
					if (connectionPoint.isOutput) {
						defaultPointTex = enablePointMarkTex;
					}
				}
			}

			foreach (var point in connectionPoints) {
				if (point.isInput) {
					GUI.DrawTexture(
						new Rect(
							baseRect.x - 2f, 
							baseRect.y + (baseRect.height - AssetBundleGraphGUISettings.CONNECTION_POINT_MARK_SIZE)/2f, 
							AssetBundleGraphGUISettings.CONNECTION_POINT_MARK_SIZE, 
							AssetBundleGraphGUISettings.CONNECTION_POINT_MARK_SIZE
						), 
						defaultPointTex
					);
				}
			}
		}

		public void DrawConnectionOutputPointMark (OnNodeEvent eventSource, bool justConnecting, Event current) {
			if (scaleFactor != SCALE_MAX) return;

			var defaultPointTex = outputPointMarkConnectedTex;

			if (justConnecting && eventSource != null) {
				if (eventSource.eventSourceNode.nodeId != this.nodeId) {
					var connectionPoint = eventSource.eventSourceNode.ConnectionPointFromConPointId(eventSource.conPointId);
					if (connectionPoint.isInput) {
						defaultPointTex = enablePointMarkTex;
					}
				}
			}

			var globalMousePosition = current.mousePosition;
			
			foreach (var point in connectionPoints) {
				if (point.isOutput) {
					var outputPointRect = OutputRect(point);

					GUI.DrawTexture(
						outputPointRect, 
						defaultPointTex
					);

					// eventPosition is contained by outputPointRect.
					if (outputPointRect.Contains(globalMousePosition)) {
						if (current.type == EventType.MouseDown) {
							Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECT_STARTED, this, current.mousePosition, point.pointId));
						}
					}
				}
			}
		}

		private Rect OutputRect (ConnectionPoint outputPoint) {
			return new Rect(
				baseRect.x + baseRect.width - 8f, 
				baseRect.y + outputPoint.buttonRect.y + 1f, 
				AssetBundleGraphGUISettings.CONNECTION_POINT_MARK_SIZE, 
				AssetBundleGraphGUISettings.CONNECTION_POINT_MARK_SIZE
			);
		}

		private void DrawNodeContents () {
			var style = EditorStyles.label;
			var defaultAlignment = style.alignment;
			style.alignment = TextAnchor.MiddleCenter;

			var nodeTitleRect = new Rect(0, 0, baseRect.width * scaleFactor, baseRect.height * scaleFactor);
			GUI.Label(nodeTitleRect, name, style);

			if (running) {
				EditorGUI.ProgressBar(new Rect(10f, baseRect.height - 20f, baseRect.width - 20f, 10f), progress, string.Empty);
			}

			style.alignment = defaultAlignment;

			if (hasErrors) { 
				EditorGUI.HelpBox(new Rect(4f, -6f, 100f, 100f), string.Empty, MessageType.Error);
			}
		}

		public void UpdateNodeRect () {
			var contentWidth = this.name.Length;
			switch (this.kind) {
				case AssetBundleGraphSettings.NodeKind.FILTER_GUI:
				case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					var longestFilterLengths = connectionPoints.OrderByDescending(con => con.label.Length).Select(con => con.label.Length).ToList();
					if (longestFilterLengths.Any()) {
						contentWidth = contentWidth + longestFilterLengths[0];
					}

					// update node height by number of output connectionPoint.
					var outputPointCount = connectionPoints.Where(connectionPoint => connectionPoint.isOutput).ToList().Count;
					if (1 < outputPointCount) {
						this.baseRect = new Rect(baseRect.x, baseRect.y, baseRect.width, AssetBundleGraphGUISettings.NODE_BASE_HEIGHT + (AssetBundleGraphGUISettings.FILTER_OUTPUT_SPAN * (outputPointCount - 1)));
					} else {
						this.baseRect = new Rect(baseRect.x, baseRect.y, baseRect.width, AssetBundleGraphGUISettings.NODE_BASE_HEIGHT);
					}
					break;
				}
			}

			var newWidth = contentWidth * 12f;
			if (newWidth < AssetBundleGraphGUISettings.NODE_BASE_WIDTH) newWidth = AssetBundleGraphGUISettings.NODE_BASE_WIDTH;
			baseRect = new Rect(baseRect.x, baseRect.y, newWidth, baseRect.height);

			RefreshConnectionPos();
		}

		private string IsOverConnectionPoint (List<ConnectionPoint> points, Vector2 touchedPoint) {
			foreach (var p in points) {
				if (p.buttonRect.x <= touchedPoint.x && 
					touchedPoint.x <= p.buttonRect.x + p.buttonRect.width && 
					p.buttonRect.y <= touchedPoint.y && 
					touchedPoint.y <= p.buttonRect.y + p.buttonRect.height
				) {
					return p.pointId;
				}
			}
			
			return string.Empty;
		}

		public Rect GetRect () {
			return baseRect;
		}

		public Vector2 GetPos () {
			return baseRect.position;
		}

		public int GetX () {
			return (int)baseRect.x;
		}

		public int GetY () {
			return (int)baseRect.y;
		}

		public int GetRightPos () {
			return (int)(baseRect.x + baseRect.width);
		}

		public int GetBottomPos () {
			return (int)(baseRect.y + baseRect.height);
		}

		public void SetPos (Vector2 position) {
			baseRect.position = position;
		}

		public void SetProgress (float val) {
			progress = val;
		}

		public void MoveRelative (Vector2 diff) {
			baseRect.position = baseRect.position - diff;
		}

		public void ShowProgress () {
			running = true;
		}

		public void HideProgress () {
			running = false;
		}

		public bool ConitainsGlobalPos (Vector2 globalPos) {
			if (baseRect.Contains(globalPos)) {
				return true;
			}

			foreach (var connectionPoint in connectionPoints) {
				if (connectionPoint.isOutput) {
					var outputRect = OutputRect(connectionPoint);
					if (outputRect.Contains(globalPos)) {
						return true;
					}
				}
			}

			return false;
		}

		public Vector2 GlobalConnectionPointPosition(string pointId) {
			var point = ConnectionPointFromConPointId(pointId);

			var x = 0f;
			var y = 0f;
			
			if (point.isInput) {
				x = baseRect.x;
				y = baseRect.y + point.buttonRect.y + (point.buttonRect.height / 2f) - 1f;
			}

			if (point.isOutput) {
				x = baseRect.x + baseRect.width;
				y = baseRect.y + point.buttonRect.y + (point.buttonRect.height / 2f) - 1f;
			}

			return new Vector2(x, y);
		}

		public List<ConnectionPoint> ConnectionPointUnderGlobalPos (Vector2 globalPos) {
			var containedPoints = new List<ConnectionPoint>();

			foreach (var connectionPoint in connectionPoints) {
				var grobalConnectionPointRect = new Rect(
					baseRect.x + connectionPoint.buttonRect.x,
					baseRect.y + connectionPoint.buttonRect.y,
					connectionPoint.buttonRect.width,
					connectionPoint.buttonRect.height
				);

				if (grobalConnectionPointRect.Contains(globalPos)) containedPoints.Add(connectionPoint);
				if (connectionPoint.isOutput) {
					var outputRect = OutputRect(connectionPoint);
					if (outputRect.Contains(globalPos)) containedPoints.Add(connectionPoint);
				}
			}
			
			return containedPoints;
		}
		
		
		public static void ShowFilterKeyTypeMenu (string current, Action<string> ExistSelected) {
			var menu = new GenericMenu();
			
			menu.AddDisabledItem(new GUIContent(current));
			
			menu.AddSeparator(string.Empty);
			
			for (var i = 0; i < TypeBinder.KeyTypes.Count; i++) {
				var type = TypeBinder.KeyTypes[i];
				if (type == current) continue;
				
				menu.AddItem(
					new GUIContent(type),
					false,
					() => {
						ExistSelected(type);
					}
				);
			}
			menu.ShowAsContext();
		}
	}
}
