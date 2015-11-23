using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;


namespace AssetGraph {
	[Serializable] public class Node {
		public static Action<OnNodeEvent> Emit;

		public static Texture2D inputPointTex;
		public static Texture2D outputPointTex;


		public static Texture2D enablePointMarkTex;

		public static Texture2D inputPointMarkTex;
		public static Texture2D outputPointMarkTex;
		public static Texture2D outputPointMarkConnectedTex;
		public static Texture2D[] platformButtonTextures;
		public static string[] platformStrings;
			
		[SerializeField] private List<ConnectionPoint> connectionPoints = new List<ConnectionPoint>();

		[SerializeField] private int nodeWindowId;
		[SerializeField] private Rect baseRect;

		[SerializeField] public string name;
		[SerializeField] public string nodeId;
		[SerializeField] public AssetGraphSettings.NodeKind kind;

		[SerializeField] public string scriptType;
		[SerializeField] public string scriptPath;
		[SerializeField] public Dictionary<string, string> loadPath;
		[SerializeField] public Dictionary<string, string> exportPath;
		[SerializeField] public List<string> filterContainsKeywords;
		[SerializeField] public Dictionary<string, string> importerPackages;
		[SerializeField] public Dictionary<string, string> groupingKeyword;
		[SerializeField] public Dictionary<string, string> bundleNameTemplate;
		[SerializeField] public Dictionary<string, List<string>> enabledBundleOptions;
		
		// for platform-package specified parameter.
		[SerializeField] public string currentPlatform = AssetGraphSettings.PLATFORM_DEFAULT_NAME;
		[SerializeField] public string currentPackage = string.Empty;
		[SerializeField] public List<string> packages = new List<string>();

		[SerializeField] private string nodeInterfaceTypeStr;
		[SerializeField] private BuildTarget currentBuildTarget;

		[SerializeField] private NodeInspector nodeInsp;



		private float progress;
		private bool running;

		public static Node LoaderNode (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, Dictionary<string, string> loadPath, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				loadPath: loadPath,
				packages: PackagesFromPlatformPackageDict(loadPath)
			);
		}

		public static Node ExporterNode (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, Dictionary<string, string> exportPath, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				exportPath: exportPath,
				packages: PackagesFromPlatformPackageDict(exportPath)
			);
		}

		public static Node ScriptNode (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, string scriptType, string scriptPath, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				scriptType: scriptType,
				scriptPath: scriptPath
			);
		}

		public static Node GUINodeForFilter (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, List<string> filterContainsKeywords, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				filterContainsKeywords: filterContainsKeywords
			);
		}

		public static Node GUINodeForImport (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, Dictionary<string, string> importerPackages, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				importerPackages: importerPackages,
				packages: PackagesFromPlatformPackageDict(importerPackages)
			);
		}

		public static Node GUINodeForGrouping (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, Dictionary<string, string> groupingKeyword, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				groupingKeyword: groupingKeyword,
				packages: PackagesFromPlatformPackageDict(groupingKeyword)
			);
		}

		public static Node GUINodeForPrefabricator (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y
			);
		}

		public static Node GUINodeForBundlizer (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, Dictionary<string, string> bundleNameTemplate, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				bundleNameTemplate: bundleNameTemplate,
				packages: PackagesFromPlatformPackageDict(bundleNameTemplate)
			);
		}

		public static Node GUINodeForBundleBuilder (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, Dictionary<string, List<string>> enabledBundleOptions, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				enabledBundleOptions: enabledBundleOptions,
				packages: PackagesFromPlatformPackageDict(enabledBundleOptions)
			);
		}

		private static List<string> PackagesFromPlatformPackageDict (Dictionary<string, string> source) {
			if (0 < source.Count) {
				return source.Keys
					.Where(platFormPackageKey => platFormPackageKey.Contains(AssetGraphSettings.package_SEPARATOR))
					.Select(platFormPackageKey => platFormPackageKey.Split(AssetGraphSettings.package_SEPARATOR.ToArray())[1])
					.ToList();
			}
			return new List<string>();
		}

		private static List<string> PackagesFromPlatformPackageDict (Dictionary<string, List<string>> source) {
			if (0 < source.Count) {
				return source.Keys
					.Where(platFormPackageKey => platFormPackageKey.Contains(AssetGraphSettings.package_SEPARATOR))
					.Select(platFormPackageKey => platFormPackageKey.Split(AssetGraphSettings.package_SEPARATOR.ToArray())[1])
					.ToList();
			}
			return new List<string>();
		}

		/**
			Inspector GUI for this node.
		*/
		[CustomEditor(typeof(NodeInspector))]
		public class NodeObj : Editor {

			private bool packageEditMode = false;

			public override void OnInspectorGUI () {
				var currentTarget = (NodeInspector)target;
				var node = currentTarget.node;
				if (node == null) return;

				var basePlatform = node.currentPlatform;
				var basePackage = node.currentPackage;

				EditorGUILayout.LabelField("nodeId:", node.nodeId);

				switch (node.kind) {
					case AssetGraphSettings.NodeKind.LOADER_GUI: {
						if (node.loadPath == null) return;
						
						EditorGUILayout.HelpBox("Loader: load files from path.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						GUILayout.Space(10f);

						/*
							platform & package
						*/
						{
							if (packageEditMode) EditorGUI.BeginDisabledGroup(true);

							// update platform & package.
							node.currentPlatform = UpdateCurrentPlatform(basePlatform);
							UpdateCurrentPackage(node);

							using (new EditorGUILayout.VerticalScope(GUI.skin.box, new GUILayoutOption[0])) {
								var newLoadPath = EditorGUILayout.TextField("Load Path", GraphStackController.ValueFromPlatformAndPackage(node.loadPath, node.currentPlatform, node.currentPackage).ToString());
								
								if (newLoadPath != GraphStackController.ValueFromPlatformAndPackage(node.loadPath, node.currentPlatform, node.currentPackage).ToString()) {
									node.loadPath[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)] = newLoadPath;
									node.Save();
								}
							}

							if (packageEditMode) EditorGUI.EndDisabledGroup();
						}
						break;
					}

					case AssetGraphSettings.NodeKind.FILTER_SCRIPT: {
						EditorGUILayout.HelpBox("Filter: filtering files by script.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						EditorGUILayout.LabelField("Script Path", node.scriptPath);

						var outputPointLabels = node.OutputPointLabels();
						EditorGUILayout.LabelField("connectionPoints Count", outputPointLabels.Count.ToString());
						
						foreach (var label in outputPointLabels) {
							EditorGUILayout.LabelField("label", label);
						}
						break;
					}

					case AssetGraphSettings.NodeKind.FILTER_GUI: {
						EditorGUILayout.HelpBox("Filter: filtering files by keywords.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}
						
						for (int i = 0; i < node.filterContainsKeywords.Count; i++) {
							GUILayout.BeginHorizontal();
							{
								if (GUILayout.Button("-")) {
									node.filterContainsKeywords.RemoveAt(i);
									node.UpdateOutputPoints();
									node.Save();
								} else {
									var newContainsKeyword = EditorGUILayout.TextField("Contains", node.filterContainsKeywords[i]);
									if (newContainsKeyword != node.filterContainsKeywords[i]) {
										node.filterContainsKeywords[i] = newContainsKeyword;
										node.UpdateOutputPoints();
										node.UpdateNodeRect();
										node.Save();
									}
								}
							}
							GUILayout.EndHorizontal();
						}

						// add contains keyword interface.
						if (GUILayout.Button("+")) {
							node.filterContainsKeywords.Add(AssetGraphSettings.DEFAULT_FILTER_KEYWORD);
							node.UpdateOutputPoints();
							node.UpdateNodeRect();
							node.Save();
						}

						break;
					}

					case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT: {
						EditorGUILayout.HelpBox("Importer: import files by script.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						EditorGUILayout.LabelField("Script Path", node.scriptPath);
						break;
					}

					case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
						EditorGUILayout.HelpBox("Importer: import files with applying settings from SamplingAssets.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}
						
						GUILayout.Space(10f);

						if (packageEditMode) EditorGUI.BeginDisabledGroup(true);

						/*
							importer node has no platform key. 
							platform key is contained by Unity's importer inspector itself.
						*/
						UpdateCurrentPackage(node);

						{
							using (new EditorGUILayout.VerticalScope(GUI.skin.box, new GUILayoutOption[0])) {
								var nodeId = node.nodeId;
								var noFilesFound = false;
								var tooManyFilesFound = false;

								var currentImporterPackage = node.currentPackage;
								if (string.IsNullOrEmpty(currentImporterPackage)) currentImporterPackage = AssetGraphSettings.PLATFORM_DEFAULT_PACKAGE;
								
								var samplingPath = FileController.PathCombine(AssetGraphSettings.IMPORTER_SAMPLING_PLACE, nodeId, currentImporterPackage);
								
								if (Directory.Exists(samplingPath)) {
									var samplingFiles = FileController.FilePathsInFolderOnly1Level(samplingPath);
									switch (samplingFiles.Count) {
										case 0: {
											noFilesFound = true;
											break;
										}
										case 1: {
											var samplingAssetPath = samplingFiles[0];
											EditorGUILayout.LabelField("Sampling Asset Path", samplingAssetPath);
											if (GUILayout.Button("Modify Import Setting")) {
												var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(samplingAssetPath);
												Selection.activeObject = obj;
											}
											if (GUILayout.Button("Reset Import Setting")) {
												var result = AssetDatabase.DeleteAsset(samplingAssetPath);
												if (!result) Debug.LogError("failed to delete samplingAsset:" + samplingAssetPath);
												node.Save();
											}
											break;
										}
										default: {
											tooManyFilesFound = true;
											break;
										}
									}
								} else {
									noFilesFound = true;
								}

								if (noFilesFound) {
									EditorGUILayout.LabelField("Sampling Asset", "no asset found. please Reload first.");
								}

								if (tooManyFilesFound) {
									EditorGUILayout.LabelField("Sampling Asset", "too many assets found. please delete file at:" + samplingPath);
								}
							}
						}

						if (packageEditMode) EditorGUI.EndDisabledGroup();

						break;
					}

					case AssetGraphSettings.NodeKind.GROUPING_GUI: {
						if (node.groupingKeyword == null) return;

						EditorGUILayout.HelpBox("Grouping: grouping files by one keyword.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						GUILayout.Space(10f);

						node.currentPlatform = UpdateCurrentPlatform(basePlatform);
						UpdateCurrentPackage(node);
						
						var groupingKeyword = EditorGUILayout.TextField("Grouping Keyword", GraphStackController.ValueFromPlatformAndPackage(node.groupingKeyword, node.currentPlatform, node.currentPackage).ToString());
						if (groupingKeyword != GraphStackController.ValueFromPlatformAndPackage(node.groupingKeyword, node.currentPlatform, node.currentPackage).ToString()) {
							node.groupingKeyword[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)] = groupingKeyword;
							node.Save();
						}
						break;
					}
					
					case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT: {
						EditorGUILayout.HelpBox("Prefabricator: generate prefab by PrefabricatorBase extended script.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						EditorGUILayout.LabelField("Script Path", node.scriptPath);
						Debug.LogWarning("型指定をしたらScriptPathが決まる、っていうのがいいと思う。型指定の窓が欲しい。");
						break;
					}

					case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:{
						EditorGUILayout.HelpBox("Prefabricator: generate prefab by PrefabricatorBase extended script.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						var newScriptType = EditorGUILayout.TextField("Script Type", node.scriptType);
						if (newScriptType != node.scriptType) {
							Debug.LogWarning("Scriptなんで、 ScriptをAttachできて、勝手に決まった方が良い。");
							node.scriptType = newScriptType;
							node.Save();
						}
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
						EditorGUILayout.HelpBox("Bundlizer: generate AssetBundle by script.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						EditorGUILayout.LabelField("Script Path", node.scriptPath);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						if (node.bundleNameTemplate == null) return;

						EditorGUILayout.HelpBox("Bundlizer: bundle resources to AssetBundle by template.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						GUILayout.Space(10f);

						node.currentPlatform = UpdateCurrentPlatform(basePlatform);
						UpdateCurrentPackage(node);
						
						var bundleNameTemplate = EditorGUILayout.TextField("Bundle Name Template", GraphStackController.ValueFromPlatformAndPackage(node.bundleNameTemplate, node.currentPlatform, node.currentPackage).ToString());
						if (bundleNameTemplate != GraphStackController.ValueFromPlatformAndPackage(node.bundleNameTemplate, node.currentPlatform, node.currentPackage).ToString()) {
							node.bundleNameTemplate[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)] = bundleNameTemplate;
							node.Save();
						}
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
						if (node.enabledBundleOptions == null) return;

						EditorGUILayout.HelpBox("BundleBuilder: generate AssetBundle.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						GUILayout.Space(10f);

						node.currentPlatform = UpdateCurrentPlatform(basePlatform);
						UpdateCurrentPackage(node);

						var bundleOptions = node.enabledBundleOptions[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)];

						for (var i = 0; i < AssetGraphSettings.DefaultBundleOptionSettings.Count; i++) {
							var enablablekey = AssetGraphSettings.DefaultBundleOptionSettings[i];

							var isEnable = bundleOptions.Contains(enablablekey);

							var result = EditorGUILayout.ToggleLeft(enablablekey, isEnable);
							if (result != isEnable) {

								if (result) {
									if (!node.enabledBundleOptions[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)].Contains(enablablekey)) {
										node.enabledBundleOptions[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)].Add(enablablekey);
									}
								}

								if (!result) {
									if (node.enabledBundleOptions[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)].Contains(enablablekey)) {
										node.enabledBundleOptions[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)].Remove(enablablekey);
									}
								}

								/*
									Cannot use options DisableWriteTypeTree and IgnoreTypeTreeChanges at the same time.
								*/
								if (enablablekey == "Disable Write TypeTree" && result &&
									node.enabledBundleOptions[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)].Contains("Ignore TypeTree Changes")) {
									node.enabledBundleOptions[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)].Remove("Ignore TypeTree Changes");
								}

								if (enablablekey == "Ignore TypeTree Changes" && result &&
									node.enabledBundleOptions[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)].Contains("Disable Write TypeTree")) {
									node.enabledBundleOptions[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)].Remove("Disable Write TypeTree");
								}

								node.Save();
								return;
							}
						}
						break;
					}

					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						if (node.exportPath == null) return;

						EditorGUILayout.HelpBox("Exporter: export files to path.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						GUILayout.Space(10f);

						node.currentPlatform = UpdateCurrentPlatform(basePlatform);
						UpdateCurrentPackage(node);

						var newExportPath = EditorGUILayout.TextField("Export Path", node.exportPath[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)]);
						if (newExportPath != node.exportPath[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)]) {
							Debug.LogWarning("本当は打ち込み単位の更新ではなくて、Finderからパス、、とかがいいんだと思うけど、今はパス。");
							node.exportPath[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)] = newExportPath;
							node.Save();
						}
						break;
					}

					default: {
						Debug.LogError("failed to match:" + node.kind);
						break;
					}
				}
			}

			private string UpdateCurrentPlatform (string basePlatfrom) {
				var newPlatform = basePlatfrom;

				EditorGUI.BeginChangeCheck();
				using (new EditorGUILayout.HorizontalScope()) {
					var choosenIndex = -1;
					for (var i = 0; i < platformButtonTextures.Length; i++) {
						var onOffBefore = platformStrings[i] == basePlatfrom;
						var onOffAfter = onOffBefore;

						// index 0 is Default.
						switch (i) {
							case 0: {
								onOffAfter = GUILayout.Toggle(onOffBefore, "Default", "toolbarbutton");
								break;
							}
							default: {
								// for each platform texture.
								var platformButtonTexture = platformButtonTextures[i];
								onOffAfter = GUILayout.Toggle(onOffBefore, platformButtonTexture, "toolbarbutton");
								break;
							}
						}

						if (onOffBefore != onOffAfter) {
							choosenIndex = i;
							break;
						}
					}

					if (EditorGUI.EndChangeCheck()) {
						newPlatform = platformStrings[choosenIndex];
					}
				}

				if (newPlatform != basePlatfrom) GUI.FocusControl(string.Empty);
				return newPlatform;
			}

			private void UpdateCurrentPackage (Node packagesParentNode) {
				using (new EditorGUILayout.HorizontalScope()) {
					GUILayout.Label("Package:");

					if (GUILayout.Button(packagesParentNode.currentPackage, "Popup")) {
						
						Action DefaultSelected = () => {
							packagesParentNode.PackageUpdated(string.Empty);
						};

						Action<string> ExistSelected = (string package) => {
							packagesParentNode.PackageUpdated(package);
						};

						ShowPackageMenu(packagesParentNode.currentPackage, DefaultSelected, ExistSelected, packagesParentNode.packages);
						GUI.FocusControl(string.Empty);
					}

					if (GUILayout.Button("+", GUILayout.Width(30))) {
						packageEditMode = true;
						GUI.FocusControl(string.Empty);
						return;
					}
				}

				if (packageEditMode) {
					GUILayout.Space(10f);
					EditorGUI.EndDisabledGroup();
					
					// package added or deleted.
					var currentPackages = ConfigurePackages(packagesParentNode.packages);
					if (packagesParentNode.packages != currentPackages) {
						packagesParentNode.packages = currentPackages;
						packagesParentNode.Save();
					}

					EditorGUI.BeginDisabledGroup(true);
					GUILayout.Space(10f);
					return;
				}
			}

			private List<string> ConfigurePackages (List<string> packagesSource) {
				var newPackagesSource = new List<string>(packagesSource);
				for (int i = 0; i < packagesSource.Count; i++) {
					GUILayout.BeginHorizontal();
					{
						if (GUILayout.Button("-")) {
							newPackagesSource.RemoveAt(i);
							break;
						} else {
							var newPackage = EditorGUILayout.TextField("Package", packagesSource[i]);
							if (newPackage != packagesSource[i]) {
								newPackagesSource[i] = newPackage;
								break;
							}
						}
					}
					GUILayout.EndHorizontal();
				}

				GUILayout.BeginHorizontal();
				{
					// add contains keyword interface.
					if (GUILayout.Button("Add New Package")) {
						newPackagesSource.Add(AssetGraphSettings.PLATFORM_NEW_PACKAGE);
					}
					if (GUILayout.Button("Done", GUILayout.Width(50))) {
						packageEditMode = false;
					}
				}
				GUILayout.EndHorizontal();

				return newPackagesSource;
			}
		}

		public void UpdateOutputPoints () {
			connectionPoints = new List<ConnectionPoint>();

			foreach (var keyword in filterContainsKeywords) {
				var newPoint = new OutputPoint(keyword);
				AddConnectionPoint(newPoint);
			}

			// add input point
			AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));

			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_UPDATED, this, Vector2.zero, null));
		}

		public void Save () {
			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_SAVE, this, Vector2.zero, null));
		}

		public Node () {}

		private Node (
			int index, 
			string name, 
			string nodeId, 
			AssetGraphSettings.NodeKind kind, 
			float x, 
			float y,
			string scriptType = null, 
			string scriptPath = null, 
			Dictionary<string, string> loadPath = null, 
			Dictionary<string, string> exportPath = null, 
			List<string> filterContainsKeywords = null, 
			Dictionary<string, string> importerPackages = null,
			Dictionary<string, string> groupingKeyword = null,
			Dictionary<string, string> bundleNameTemplate = null,
			Dictionary<string, List<string>> enabledBundleOptions = null,
			List<string> packages = null
		) {
			nodeInsp = ScriptableObject.CreateInstance<NodeInspector>();
			nodeInsp.hideFlags = HideFlags.DontSave;

			this.nodeWindowId = index;
			this.name = name;
			this.nodeId = nodeId;
			this.kind = kind;
			this.scriptType = scriptType;
			this.scriptPath = scriptPath;
			this.loadPath = loadPath;
			this.exportPath = exportPath;
			this.filterContainsKeywords = filterContainsKeywords;
			this.importerPackages = importerPackages;
			this.groupingKeyword = groupingKeyword;
			this.bundleNameTemplate = bundleNameTemplate;
			this.enabledBundleOptions = enabledBundleOptions;

			this.packages = packages;
			
			this.baseRect = new Rect(x, y, AssetGraphGUISettings.NODE_BASE_WIDTH, AssetGraphGUISettings.NODE_BASE_HEIGHT);
			
			switch (this.kind) {
				case AssetGraphSettings.NodeKind.LOADER_GUI:
				case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 0";
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetGraphSettings.NodeKind.FILTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 1";
					break;
				}
				
				case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
				case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 2";
					break;
				}

				case AssetGraphSettings.NodeKind.GROUPING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 3";
					break;
				}

				case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:
				 {
					this.nodeInterfaceTypeStr = "flow node 4";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT:
				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 5";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6";
					break;
				}

				default: {
					Debug.LogError("failed to match:" + this.kind);
					break;
				}
			}
		}

		public void PackageUpdated (string newCurrentPackage) {
			if (kind == AssetGraphSettings.NodeKind.IMPORTER_GUI) {
				currentPackage = newCurrentPackage;
				var platformPackageKey = GraphStackController.Platform_Package_Key(AssetGraphSettings.PLATFORM_DEFAULT_NAME, currentPackage);

				if (!importerPackages.ContainsKey(platformPackageKey)) importerPackages[platformPackageKey] = string.Empty;
				Save();

				Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_SETUPWITHPACKAGE, this, Vector2.zero, null));
				return;
			}
			currentPackage = newCurrentPackage;
		}

		public void SetActive () {
			nodeInsp.UpdateNode(this);
			Selection.activeObject = nodeInsp;

			switch (this.kind) {
				case AssetGraphSettings.NodeKind.LOADER_GUI:
				case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 0 on";
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetGraphSettings.NodeKind.FILTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 1 on";
					break;
				}
				
				case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
				case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 2 on";
					break;
				}

				case AssetGraphSettings.NodeKind.GROUPING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 3 on";
					break;
				}

				case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:
				 {
					this.nodeInterfaceTypeStr = "flow node 4 on";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT:
				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 5 on";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6 on";
					break;
				}

				default: {
					Debug.LogError("failed to match:" + this.kind);
					break;
				}
			}
		}

		public void SetInactive () {
			switch (this.kind) {
				case AssetGraphSettings.NodeKind.LOADER_GUI:
				case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 0";
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetGraphSettings.NodeKind.FILTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 1";
					break;
				}
				
				case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
				case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 2";
					break;
				}

				case AssetGraphSettings.NodeKind.GROUPING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 3";
					break;
				}

				case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:
				 {
					this.nodeInterfaceTypeStr = "flow node 4";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT:
				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 5";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6";
					break;
				}

				default: {
					Debug.LogError("failed to match:" + this.kind);
					break;
				}
			}
		}

		public void AddConnectionPoint (ConnectionPoint adding) {
			connectionPoints.Add(adding);
			
			// update node size by number of output connectionPoint.
			var outputPointCount = connectionPoints.Where(connectionPoint => connectionPoint.isOutput).ToList().Count;
			if (1 < outputPointCount) {
				this.baseRect = new Rect(baseRect.x, baseRect.y, baseRect.width, AssetGraphGUISettings.NODE_BASE_HEIGHT + (AssetGraphGUISettings.FILTER_OUTPUT_SPAN * (outputPointCount - 1)));
			} else {
				this.baseRect = new Rect(baseRect.x, baseRect.y, baseRect.width, AssetGraphGUISettings.NODE_BASE_HEIGHT);
			}

			UpdateNodeRect();
		}

		public List<ConnectionPoint> DuplicateConnectionPoints () {
			var copiedConnectionList = new List<ConnectionPoint>();
			foreach (var connectionPoint in connectionPoints) {
				if (connectionPoint.isOutput) copiedConnectionList.Add(new OutputPoint(connectionPoint.label));
				if (connectionPoint.isInput) copiedConnectionList.Add(new InputPoint(connectionPoint.label));
			}
			return copiedConnectionList;
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

		public ConnectionPoint ConnectionPointFromLabel (string label) {
			var targetPoints = connectionPoints.Where(con => con.label == label).ToList();
			if (!targetPoints.Any()) {
				Debug.LogError("no connection label:" + label + " exists in node name:" + name);
				return null;
			}
			return targetPoints[0];
		}

		public void DrawNode () {
			baseRect = GUI.Window(nodeWindowId, baseRect, UpdateNodeEvent, string.Empty, nodeInterfaceTypeStr);
		}

		/**
			retrieve GUI events for this node.
		*/
		private void UpdateNodeEvent (int id) {
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
					handling release of mouse drag on this node.
				*/
				case EventType.MouseUp: {
					// if mouse position is on the connection point, emit mouse raised event over thr connection.
					foreach (var connectionPoint in connectionPoints) {
						var globalConnectonPointRect = new Rect(connectionPoint.buttonRect.x, connectionPoint.buttonRect.y, connectionPoint.buttonRect.width, connectionPoint.buttonRect.height);
						if (globalConnectonPointRect.Contains(Event.current.mousePosition)) {
							Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECTION_RAISED, this, Event.current.mousePosition, connectionPoint));
							return;
						}
					}

					Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_TOUCHED, this, Event.current.mousePosition, null));
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

					if (result != null) {
						Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECT_STARTED, this, Event.current.mousePosition, result));
						break;
					}
					break;
				}
			}

			// draw & update connectionPoint button interface.
			foreach (var point in connectionPoints) {
				switch (this.kind) {
					case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
					case AssetGraphSettings.NodeKind.FILTER_GUI: {
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
					// var activeFrameLabel = new GUIStyle("AnimationKeyframeBackground");// そのうちやる。
					// activeFrameLabel.backgroundColor = Color.clear;
					// Debug.LogError("contentOffset"+ activeFrameLabel.contentOffset);
					// Debug.LogError("contentOffset"+ activeFrameLabel.Button);

					GUI.backgroundColor = Color.clear;
					GUI.Button(point.buttonRect, inputPointTex, "AnimationKeyframeBackground");
				}

				if (point.isOutput) {
					GUI.backgroundColor = Color.clear;
					GUI.Button(point.buttonRect, outputPointTex, "AnimationKeyframeBackground");
				}
			}

			/*
				right click.
			*/
			if (
				Event.current.type == EventType.ContextClick
				 || (Event.current.type == EventType.MouseUp && Event.current.button == 1)
			) {
				var rightClickPos = Event.current.mousePosition;
				var menu = new GenericMenu();
				menu.AddItem(
					new GUIContent("Delete All Input Connections"),
					false, 
					() => {
						Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_DELETE_ALL_INPUT_CONNECTIONS, this, rightClickPos, null));
					}
				);
				menu.AddItem(
					new GUIContent("Delete All Output Connections"),
					false, 
					() => {
						Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_DELETE_ALL_OUTPUT_CONNECTIONS, this, rightClickPos, null));
					}
				);
				menu.AddItem(
					new GUIContent("Duplicate"),
					false, 
					() => {
						Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_DUPLICATE_TAPPED, this, rightClickPos, null));
					}
				);
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


			DrawNodeContents();

			GUI.DragWindow();
		}

		public void DrawConnectionInputPointMark (OnNodeEvent eventSource, bool justConnecting) {
			var defaultPointTex = inputPointMarkTex;

			if (justConnecting && eventSource != null) {
				if (eventSource.eventSourceNode.nodeId != this.nodeId) {
					if (eventSource.eventSourceConnectionPoint.isOutput) {
						defaultPointTex = enablePointMarkTex;
					}
				}
			}

			foreach (var point in connectionPoints) {
				if (point.isInput) {
					GUI.DrawTexture(
						new Rect(
							baseRect.x - 2f, 
							baseRect.y + (baseRect.height - AssetGraphGUISettings.CONNECTION_POINT_MARK_SIZE)/2f, 
							AssetGraphGUISettings.CONNECTION_POINT_MARK_SIZE, 
							AssetGraphGUISettings.CONNECTION_POINT_MARK_SIZE
						), 
						defaultPointTex
					);
				}
			}
		}

		public void DrawConnectionOutputPointMark (OnNodeEvent eventSource, bool justConnecting, Event current) {
			var defaultPointTex = outputPointMarkConnectedTex;
			
			if (justConnecting && eventSource != null) {
				if (eventSource.eventSourceNode.nodeId != this.nodeId) {
					if (eventSource.eventSourceConnectionPoint.isInput) {
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
							Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECT_STARTED, this, current.mousePosition, point));
						}
					}
				}
			}
		}

		private Rect OutputRect (ConnectionPoint outputPoint) {
			return new Rect(
				baseRect.x + baseRect.width - 8f, 
				baseRect.y + outputPoint.buttonRect.y + 1f, 
				AssetGraphGUISettings.CONNECTION_POINT_MARK_SIZE, 
				AssetGraphGUISettings.CONNECTION_POINT_MARK_SIZE
			);
		}

		private void DrawNodeContents () {
			var style = EditorStyles.label;
			var defaultAlignment = style.alignment;
			style.alignment = TextAnchor.MiddleCenter;
			

			var nodeTitleRect = new Rect(0, 0, baseRect.width, baseRect.height);
			if (this.kind == AssetGraphSettings.NodeKind.PREFABRICATOR_GUI) GUI.contentColor = Color.black;
			if (this.kind == AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT) GUI.contentColor = Color.black; 
			GUI.Label(nodeTitleRect, name, style);

			if (running) EditorGUI.ProgressBar(new Rect(10f, baseRect.height - 20f, baseRect.width - 20f, 10f), progress, string.Empty);

			style.alignment = defaultAlignment;
		}

		public void UpdateNodeRect () {
			var contentWidth = this.name.Length;
			if (this.kind == AssetGraphSettings.NodeKind.FILTER_GUI) {
				var longestFilterLengths = connectionPoints.OrderByDescending(con => con.label.Length).Select(con => con.label.Length).ToList();
				if (longestFilterLengths.Any()) {
					contentWidth = contentWidth + longestFilterLengths[0];
				}
			}

			var newWidth = contentWidth * 12f;
			if (newWidth < AssetGraphGUISettings.NODE_BASE_WIDTH) newWidth = AssetGraphGUISettings.NODE_BASE_WIDTH;
			baseRect = new Rect(baseRect.x, baseRect.y, newWidth, baseRect.height);

			RefreshConnectionPos();
		}

		private ConnectionPoint IsOverConnectionPoint (List<ConnectionPoint> points, Vector2 touchedPoint) {
			foreach (var p in points) {
				if (p.buttonRect.x <= touchedPoint.x && 
					touchedPoint.x <= p.buttonRect.x + p.buttonRect.width && 
					p.buttonRect.y <= touchedPoint.y && 
					touchedPoint.y <= p.buttonRect.y + p.buttonRect.height
				) {
					return p;
				}
			}
			
			return null;
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

		public Vector2 GlobalConnectionPointPosition(ConnectionPoint p) {
			var x = 0f;
			var y = 0f;

			if (p.isInput) {
				x = baseRect.x;
				y = baseRect.y + p.buttonRect.y + (p.buttonRect.height / 2f) - 1f;
			}

			if (p.isOutput) {
				x = baseRect.x + baseRect.width;
				y = baseRect.y + p.buttonRect.y + (p.buttonRect.height / 2f) - 1f;
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

		public static void ShowPackageMenu (string currentPackage, Action NoneSelected, Action<string> ExistSelected, List<string> packages) {
			List<string> packageList = new List<string>();
			
			// first is None.
			packageList.Add(AssetGraphSettings.PLATFORM_NONE_PACKAGE);

			// delim
			packageList.Add(string.Empty);

			packageList.AddRange(packages);

			// delim
			packageList.Add(string.Empty);

			var menu = new GenericMenu();

			for (var i = 0; i < packageList.Count; i++) {
				var packageName = packageList[i];
				switch (i) {
					case 0: {
						menu.AddItem(
							new GUIContent(packageName), 
							false,// check!
							() => NoneSelected()
						);
						continue;
					}
					default: {
						menu.AddItem(
							new GUIContent(packageName), 
							false, // check
							() => {
								ExistSelected(packageName);
							}
						);
						break;
					}
				}
			}
			
			menu.ShowAsContext();
		}
	}
}