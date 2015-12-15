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

		public static List<string> allNodeNames;

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
		[SerializeField] public AssetGraphSettings.NodeKind kind;

		[SerializeField] public string scriptType;
		[SerializeField] public string scriptPath;
		[SerializeField] public SerializablePseudoDictionary loadPath;
		[SerializeField] public SerializablePseudoDictionary exportPath;
		[SerializeField] public List<string> filterContainsKeywords;
		[SerializeField] public SerializablePseudoDictionary importerPackages;
		[SerializeField] public SerializablePseudoDictionary groupingKeyword;
		[SerializeField] public SerializablePseudoDictionary bundleNameTemplate;
		[SerializeField] public SerializablePseudoDictionary2 enabledBundleOptions;
		
		// for platform-package specified parameter.
		[SerializeField] public string currentPlatform = AssetGraphSettings.PLATFORM_DEFAULT_NAME;
		[SerializeField] public string currentPackage = string.Empty;
		public static List<string> NodeSharedPackages = new List<string>();

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
				loadPath: loadPath
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
				exportPath: exportPath
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
				importerPackages: importerPackages
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
				groupingKeyword: groupingKeyword
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
				bundleNameTemplate: bundleNameTemplate
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
				enabledBundleOptions: enabledBundleOptions
			);
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
				
				// EditorGUILayout.LabelField("nodeId:", node.nodeId);

				switch (node.kind) {
					case AssetGraphSettings.NodeKind.LOADER_GUI: {
						if (node.loadPath == null) return;
						
						EditorGUILayout.HelpBox("Loader: load files from path.", MessageType.Info);
						UpdateNodeName(node);

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
								var newLoadPath = EditorGUILayout.TextField(
									"Load Path", 
									GraphStackController.ValueFromPlatformAndPackage(
										node.loadPath.ReadonlyDict(), 
										node.currentPlatform, 
										node.currentPackage
									).ToString()
								);
								var loaderNodePath = GraphStackController.WithProjectPath(newLoadPath);
								IntegratedGUILoader.ValidateLoadPath(
									newLoadPath,
									loaderNodePath,
									() => {
										EditorGUILayout.HelpBox("load path is empty.", MessageType.Error);
									},
									() => {
										EditorGUILayout.HelpBox("directory not found:" + loaderNodePath, MessageType.Error);
									}
								);
								
								if (newLoadPath !=	GraphStackController.ValueFromPlatformAndPackage(
										node.loadPath.ReadonlyDict(),
										node.currentPlatform, 
										node.currentPackage
									).ToString()
								) {
									node.BeforeSave();
									node.loadPath.Add(GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage), newLoadPath);
									node.Save();
								}
							}

							if (packageEditMode) EditorGUI.EndDisabledGroup();
							UpdateDeleteSetting(node);
						}
						break;
					}

					case AssetGraphSettings.NodeKind.FILTER_SCRIPT: {
						EditorGUILayout.HelpBox("Filter: filtering files by script.", MessageType.Info);
						UpdateNodeName(node);

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
						UpdateNodeName(node);
						
						using (new EditorGUILayout.VerticalScope(GUI.skin.box, new GUILayoutOption[0])) {
							for (int i = 0; i < node.filterContainsKeywords.Count; i++) {
								GUILayout.BeginHorizontal();
								{
									if (GUILayout.Button("-")) {
										node.BeforeSave();
										node.filterContainsKeywords.RemoveAt(i);
										node.FilterOutputPointsDeleted(i);
									} else {
										var newContainsKeyword = EditorGUILayout.TextField("Contains", node.filterContainsKeywords[i]);
										var currentKeywordsSource = new List<string>(node.filterContainsKeywords);
										currentKeywordsSource.RemoveAt(i);
										var currentKeywords = new List<string>(currentKeywordsSource);
										IntegratedGUIFilter.ValidateFilter(
											newContainsKeyword,
											currentKeywords,
											() => {
												EditorGUILayout.HelpBox("filter is empty.", MessageType.Error);
											},
											() => {
												EditorGUILayout.HelpBox("already exist.", MessageType.Error);
											}
										);

										if (newContainsKeyword != node.filterContainsKeywords[i]) {
											node.BeforeSave();
											node.filterContainsKeywords[i] = newContainsKeyword;
											node.FilterOutputPointsLabelChanged(i, node.filterContainsKeywords[i]);
										}
									}
								}
								GUILayout.EndHorizontal();
							}


							// add contains keyword interface.
							if (GUILayout.Button("+")) {
								node.BeforeSave();
								var addingIndex = node.filterContainsKeywords.Count;
								var newKeyword = AssetGraphSettings.DEFAULT_FILTER_KEYWORD;
								node.filterContainsKeywords.Add(newKeyword);
								node.FilterOutputPointsAdded(addingIndex, AssetGraphSettings.DEFAULT_FILTER_KEYWORD);
							}
						}

						break;
					}

					case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT: {
						EditorGUILayout.HelpBox("Importer: import files by script.", MessageType.Info);
						UpdateNodeName(node);

						EditorGUILayout.LabelField("Script Path", node.scriptPath);
						break;
					}

					case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
						EditorGUILayout.HelpBox("Importer: import files with applying settings from SamplingAssets.", MessageType.Info);
						UpdateNodeName(node);
						
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
									var samplingFiles = FileController.FilePathsInFolderOnly1Level(samplingPath)
										.Where(path => !GraphStackController.IsMetaFile(path))
										.ToList();
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
												// delete all import setting files.
												FileController.RemakeDirectory(samplingPath);
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
									EditorGUILayout.LabelField("Sampling Asset", "too many assets found. please delete files at:" + samplingPath);
								}
							}
						}

						if (packageEditMode) EditorGUI.EndDisabledGroup();
						UpdateDeleteSetting(node);

						break;
					}

					case AssetGraphSettings.NodeKind.GROUPING_GUI: {
						if (node.groupingKeyword == null) return;

						EditorGUILayout.HelpBox("Grouping: grouping files by one keyword.", MessageType.Info);
						UpdateNodeName(node);

						GUILayout.Space(10f);

						node.currentPlatform = UpdateCurrentPlatform(basePlatform);
						UpdateCurrentPackage(node);

						using (new EditorGUILayout.VerticalScope(GUI.skin.box, new GUILayoutOption[0])) {
							var newGroupingKeyword = EditorGUILayout.TextField(
								"Grouping Keyword",
								GraphStackController.ValueFromPlatformAndPackage(
									node.groupingKeyword.ReadonlyDict(), 
									node.currentPlatform,
									node.currentPackage
								).ToString()
							);
							IntegratedGUIGrouping.ValidateGroupingKeyword(
								newGroupingKeyword,
								() => {
									EditorGUILayout.HelpBox("groupingKeyword is empty.", MessageType.Error);
								},
								() => {
									EditorGUILayout.HelpBox("grouping keyword does not contain " + AssetGraphSettings.KEYWORD_WILDCARD + " groupingKeyword:" + newGroupingKeyword, MessageType.Error);
								}
							);

							if (newGroupingKeyword != GraphStackController.ValueFromPlatformAndPackage(
									node.groupingKeyword.ReadonlyDict(), 
									node.currentPlatform, 
									node.currentPackage
								).ToString()
							) {
								node.BeforeSave();
								node.groupingKeyword.Add(GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage), newGroupingKeyword);
								node.Save();
							}
						}

						UpdateDeleteSetting(node);
						break;
					}
					
					case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT: {
						EditorGUILayout.HelpBox("Prefabricator: generate prefab by PrefabricatorBase extended script.", MessageType.Info);
						UpdateNodeName(node);

						EditorGUILayout.LabelField("Script Path", node.scriptPath);
						break;
					}

					case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:{
						EditorGUILayout.HelpBox("Prefabricator: generate prefab by PrefabricatorBase extended script.", MessageType.Info);
						UpdateNodeName(node);

						using (new EditorGUILayout.VerticalScope(GUI.skin.box, new GUILayoutOption[0])) {
							var newScriptType = EditorGUILayout.TextField("Script Type", node.scriptType);

							/*
								check prefabricator script-type string.
							*/
							if (string.IsNullOrEmpty(newScriptType)) {
								EditorGUILayout.HelpBox("PrefabricatorBase extended class name is empty.", MessageType.Error);
							}

							var loadedType = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(newScriptType);
							
							if (loadedType == null) {
								EditorGUILayout.HelpBox("PrefabricatorBase extended class not found:" + newScriptType, MessageType.Error);
							}

							if (newScriptType != node.scriptType) {
								node.BeforeSave();
								node.scriptType = newScriptType;
								node.Save();
							}
						}
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
						EditorGUILayout.HelpBox("Bundlizer: generate AssetBundle by script.", MessageType.Info);
						UpdateNodeName(node);

						EditorGUILayout.LabelField("Script Path", node.scriptPath);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						if (node.bundleNameTemplate == null) return;

						EditorGUILayout.HelpBox("Bundlizer: bundle resources to AssetBundle by template.", MessageType.Info);
						UpdateNodeName(node);

						GUILayout.Space(10f);

						node.currentPlatform = UpdateCurrentPlatform(basePlatform);
						UpdateCurrentPackage(node);
						
						using (new EditorGUILayout.VerticalScope(GUI.skin.box, new GUILayoutOption[0])) {
							var bundleNameTemplate = EditorGUILayout.TextField(
								"Bundle Name Template", 
								GraphStackController.ValueFromPlatformAndPackage(
									node.bundleNameTemplate.ReadonlyDict(), 
									node.currentPlatform, 
									node.currentPackage
								).ToString()
							).ToLower();

							IntegratedGUIBundlizer.ValidateBundleNameTemplate(
								bundleNameTemplate,
								() => {
									EditorGUILayout.HelpBox("no Bundle Name Template set.", MessageType.Error);
								}
							);

							if (bundleNameTemplate != GraphStackController.ValueFromPlatformAndPackage(
									node.bundleNameTemplate.ReadonlyDict(), 
									node.currentPlatform, 
									node.currentPackage
								).ToString()
							) {
								node.BeforeSave();
								node.bundleNameTemplate.Add(GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage), bundleNameTemplate);
								node.Save();
							}
						}

						UpdateDeleteSetting(node);

						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
						if (node.enabledBundleOptions == null) return;

						EditorGUILayout.HelpBox("BundleBuilder: generate AssetBundle by AssetBundle name settings.", MessageType.Info);
						UpdateNodeName(node);

						GUILayout.Space(10f);

						node.currentPlatform = UpdateCurrentPlatform(basePlatform);
						UpdateCurrentPackage(node);

						using (new EditorGUILayout.VerticalScope(GUI.skin.box, new GUILayoutOption[0])) {
							var bundleOptions = GraphStackController.ValueFromPlatformAndPackage(
								node.enabledBundleOptions.ReadonlyDict(),
								node.currentPlatform, 
								node.currentPackage
							);

							var plartform_pakcage_key = GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage);

							for (var i = 0; i < AssetGraphSettings.DefaultBundleOptionSettings.Count; i++) {
								var enablablekey = AssetGraphSettings.DefaultBundleOptionSettings[i];

								// contains keyword == enabled. if not, disabled.
								var isEnabled = bundleOptions.Contains(enablablekey);

								var result = EditorGUILayout.ToggleLeft(enablablekey, isEnabled);
								if (result != isEnabled) {
									node.BeforeSave();

									var resultsDict = node.enabledBundleOptions.ReadonlyDict();
									var resultList = new List<string>();
									if (resultsDict.ContainsKey(plartform_pakcage_key)) resultList = resultsDict[plartform_pakcage_key];

									if (result) {
										if (!resultList.Contains(enablablekey)) {
											var currentEnableds = new List<string>();
											if (resultsDict.ContainsKey(plartform_pakcage_key)) currentEnableds = resultsDict[plartform_pakcage_key];
											currentEnableds.Add(enablablekey);

											node.enabledBundleOptions.Add(
												GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage),
												currentEnableds
											);
										}
									}

									if (!result) {
										if (resultList.Contains(enablablekey)) {
											var currentEnableds = new List<string>();
											if (resultsDict.ContainsKey(plartform_pakcage_key)) currentEnableds = resultsDict[plartform_pakcage_key];
											currentEnableds.Remove(enablablekey);
											
											node.enabledBundleOptions.Add(
												GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage),
												currentEnableds
											);
										}
									}

									/*
										Cannot use options DisableWriteTypeTree and IgnoreTypeTreeChanges at the same time.
									*/
									if (enablablekey == "Disable Write TypeTree" && result &&
										node.enabledBundleOptions.ReadonlyDict()[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)].Contains("Ignore TypeTree Changes")) {

										var newEnableds = node.enabledBundleOptions.ReadonlyDict()[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)];
										newEnableds.Remove("Ignore TypeTree Changes");
										
										node.enabledBundleOptions.Add(
											GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage),
											newEnableds
										);
									}

									if (enablablekey == "Ignore TypeTree Changes" && result &&
										node.enabledBundleOptions.ReadonlyDict()[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)].Contains("Disable Write TypeTree")) {
										
										var newEnableds = node.enabledBundleOptions.ReadonlyDict()[GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage)];
										newEnableds.Remove("Disable Write TypeTree");
										
										node.enabledBundleOptions.Add(
											GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage),
											newEnableds
										);
									}

									node.Save();
									return;
								}
							}
						}

						UpdateDeleteSetting(node);

						break;
					}

					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						if (node.exportPath == null) return;

						EditorGUILayout.HelpBox("Exporter: export files to path.", MessageType.Info);
						UpdateNodeName(node);

						GUILayout.Space(10f);

						node.currentPlatform = UpdateCurrentPlatform(basePlatform);
						UpdateCurrentPackage(node);

						using (new EditorGUILayout.VerticalScope(GUI.skin.box, new GUILayoutOption[0])) {
							var newExportPath = EditorGUILayout.TextField(
								"Export Path", 
								GraphStackController.ValueFromPlatformAndPackage(
									node.exportPath.ReadonlyDict(), 
									node.currentPlatform, 
									node.currentPackage
								).ToString()
							);

							var exporterrNodePath = GraphStackController.WithProjectPath(newExportPath);
							IntegratedGUIExporter.ValidateExportPath(
								newExportPath,
								exporterrNodePath,
								() => {
									EditorGUILayout.HelpBox("export path is empty.", MessageType.Error);
								},
								() => {
									EditorGUILayout.HelpBox("directory not found:" + exporterrNodePath, MessageType.Error);
								}
							);

							if (newExportPath != GraphStackController.ValueFromPlatformAndPackage(
									node.exportPath.ReadonlyDict(),
									node.currentPlatform, 
									node.currentPackage
								).ToString()
							) {
								node.BeforeSave();
								node.exportPath.Add(GraphStackController.Platform_Package_Key(node.currentPlatform, node.currentPackage), newExportPath);
								node.Save();
							}
						}
						
						UpdateDeleteSetting(node);

						break;
					}

					default: {
						Debug.LogError("failed to match:" + node.kind);
						break;
					}
				}
			}

			private void UpdateNodeName (Node node) {
				var newName = EditorGUILayout.TextField("Node Name", node.name);
				
				var overlapping = Node.allNodeNames.GroupBy(x => x)
					.Where(group => group.Count() > 1)
					.Select(group => group.Key);
				if (overlapping.Any() && overlapping.Contains(newName)) {
					EditorGUILayout.HelpBox("node name is overlapping:" + newName, MessageType.Error);
				}

				if (newName != node.name) {
					node.BeforeSave();
					node.name = newName;
					node.UpdateNodeRect();
					node.Save();
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
					var currentPackageStr = packagesParentNode.currentPackage;

					// if package is empty => package is default one. use (None).
					if (string.IsNullOrEmpty(currentPackageStr)) currentPackageStr = AssetGraphSettings.PLATFORM_NONE_PACKAGE;

					if (GUILayout.Button(currentPackageStr, "Popup")) {
						Action DefaultSelected = () => {
							packagesParentNode.PackageChanged(string.Empty);
						};

						Action<string> ExistSelected = (string package) => {
							packagesParentNode.PackageChanged(package);
						};

						ShowPackageMenu(packagesParentNode.currentPackage, DefaultSelected, ExistSelected);
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
					ConfigureSharedPackages(packagesParentNode);

					EditorGUI.BeginDisabledGroup(true);
					GUILayout.Space(10f);
				}
			}

			private void ConfigureSharedPackages (Node packagesParentNode) {
				for (int i = 0; i < NodeSharedPackages.Count; i++) {
					GUILayout.BeginHorizontal();
					{
						if (GUILayout.Button("-")) {
							NodeSharedPackages.RemoveAt(i);
							packagesParentNode.UpdatePackages();
							break;
						} else {
							var newPackage = EditorGUILayout.TextField("Package", NodeSharedPackages[i]);
							if (newPackage != NodeSharedPackages[i]) {
								NodeSharedPackages[i] = newPackage;
								packagesParentNode.UpdatePackages();
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
						NodeSharedPackages.Add(AssetGraphSettings.PLATFORM_NEW_PACKAGE + "_" + NodeSharedPackages.Count);
						packagesParentNode.UpdatePackages();
					}
					if (GUILayout.Button("Done", GUILayout.Width(50))) {
						packageEditMode = false;
					}
				}
				GUILayout.EndHorizontal();
			}

			private void UpdateDeleteSetting (Node currentNode) {
				var currentNodePlatformPackageKey = GraphStackController.Platform_Package_Key(currentNode.currentPlatform, currentNode.currentPackage);

				if (currentNodePlatformPackageKey == AssetGraphSettings.PLATFORM_DEFAULT_NAME) return;

				using (new EditorGUILayout.HorizontalScope()) {
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Discard And Use Default", GUILayout.Width(150))) {
						currentNode.BeforeSave();
						currentNode.DeleteCurrentPackagePlatformKey(currentNodePlatformPackageKey);
						GUI.FocusControl(string.Empty);
						currentNode.Save();
					}
				}
			}
		}

		public void FilterOutputPointsAdded (int addedIndex, string keyword) {
			connectionPoints.Insert(addedIndex, new OutputPoint(keyword));
			UpdateNodeRect();
			Save();
		}

		public void FilterOutputPointsDeleted (int deletedIndex) {
			var deletedConnectionPoint = connectionPoints[deletedIndex];
			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED, this, Vector2.zero, deletedConnectionPoint));
			connectionPoints.RemoveAt(deletedIndex);
			UpdateNodeRect();
			Save();
		}

		public void FilterOutputPointsLabelChanged (int changedIndex, string latestLabel) {
			connectionPoints[changedIndex].label = latestLabel;
			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_LABELCHANGED, this, Vector2.zero, connectionPoints[changedIndex]));
			UpdateNodeRect();
			Save();
		}

		public void BeforeSave () {
			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_BEFORESAVE, this, Vector2.zero, null));
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
			Dictionary<string, List<string>> enabledBundleOptions = null
		) {
			nodeInsp = ScriptableObject.CreateInstance<NodeInspector>();
			nodeInsp.hideFlags = HideFlags.DontSave;
			this.nodeWindowId = index;
			this.name = name;
			this.nodeId = nodeId;
			this.kind = kind;
			this.scriptType = scriptType;
			this.scriptPath = scriptPath;
			if (loadPath != null) this.loadPath = new SerializablePseudoDictionary(loadPath);
			if (exportPath != null) this.exportPath = new SerializablePseudoDictionary(exportPath);
			this.filterContainsKeywords = filterContainsKeywords;
			if (importerPackages != null) this.importerPackages = new SerializablePseudoDictionary(importerPackages);
			if (groupingKeyword != null) this.groupingKeyword = new SerializablePseudoDictionary(groupingKeyword);
			if (bundleNameTemplate != null) this.bundleNameTemplate = new SerializablePseudoDictionary(bundleNameTemplate);
			if (enabledBundleOptions != null) this.enabledBundleOptions = new SerializablePseudoDictionary2(enabledBundleOptions);
			
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

		public Node DuplicatedNode (int newIndex, float newX, float newY) {
			var duplicatedNode = new Node(
				newIndex,
				this.name,
				Guid.NewGuid().ToString(),
				this.kind, 
				newX,
				newY,
				this.scriptType,
				this.scriptPath,
				(this.loadPath != null) ? loadPath.ReadonlyDict() : null,
				(this.exportPath != null) ? this.exportPath.ReadonlyDict() : null,
				this.filterContainsKeywords,
				(this.importerPackages != null) ? this.importerPackages.ReadonlyDict() : null,
				(this.groupingKeyword != null) ? this.groupingKeyword.ReadonlyDict() : null,
				(this.bundleNameTemplate != null) ? this.bundleNameTemplate.ReadonlyDict() : null,
				(this.enabledBundleOptions != null) ? this.enabledBundleOptions.ReadonlyDict() : null
			);
			return duplicatedNode;
		}

		public void UpdatePackages () {
			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_UPDATEPACKAGE, this, Vector2.zero, null));
		}

		public void PackageChanged (string newCurrentPackage) {
			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_BEFORESAVE, this, Vector2.zero, null));
			currentPackage = newCurrentPackage;

			/*
				if changed node is importer, should run [new package import] for setting.
			*/
			if (kind == AssetGraphSettings.NodeKind.IMPORTER_GUI) {
				// importer node's platform is absolutely PLATFORM_DEFAULT_NAME.
				var platformPackageKey = GraphStackController.Platform_Package_Key(AssetGraphSettings.PLATFORM_DEFAULT_NAME, currentPackage);
				if (!importerPackages.ContainsKey(platformPackageKey)) importerPackages.Add(platformPackageKey, string.Empty);
			}
			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_SETUPWITHPACKAGE, this, Vector2.zero, null));
			Save();
		}

		public void DeleteCurrentPackagePlatformKey (string platformPackageKey) {
			switch (this.kind) {
				case AssetGraphSettings.NodeKind.LOADER_GUI: {
					loadPath.Remove(platformPackageKey);
					break;
				}
				
				case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
					importerPackages.Remove(platformPackageKey);
					break;
				}

				case AssetGraphSettings.NodeKind.GROUPING_GUI: {
					groupingKeyword.Remove(platformPackageKey);
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
					bundleNameTemplate.Remove(platformPackageKey);
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					enabledBundleOptions.Remove(platformPackageKey);
					break;
				}

				case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
					exportPath.Remove(platformPackageKey);
					break;
				}
				
				default: {
					Debug.LogError("failed to match:" + this.kind);
					break;
				}
			}
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
			var scaledBaseRect = ScaleEffect(baseRect);

			var movedRect = GUI.Window(nodeWindowId, scaledBaseRect, UpdateNodeEvent, string.Empty, nodeInterfaceTypeStr);

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

		/**
			retrieve mouse events for this node in this AssetGraoh window.
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
							Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECTION_RAISED, this, Event.current.mousePosition, connectionPoint));
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


			DrawNodeContents();

			GUI.DragWindow();
		}

		public void DrawConnectionInputPointMark (OnNodeEvent eventSource, bool justConnecting) {
			if (scaleFactor != SCALE_MAX) return;

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
			if (scaleFactor != SCALE_MAX) return;

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
			// if ( != SCALE_MAX) return;

			var style = EditorStyles.label;
			var defaultAlignment = style.alignment;
			style.alignment = TextAnchor.MiddleCenter;
			

			var nodeTitleRect = new Rect(0, 0, baseRect.width * scaleFactor, baseRect.height * scaleFactor);
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

				// update node height by number of output connectionPoint.
				var outputPointCount = connectionPoints.Where(connectionPoint => connectionPoint.isOutput).ToList().Count;
				if (1 < outputPointCount) {
					this.baseRect = new Rect(baseRect.x, baseRect.y, baseRect.width, AssetGraphGUISettings.NODE_BASE_HEIGHT + (AssetGraphGUISettings.FILTER_OUTPUT_SPAN * (outputPointCount - 1)));
				} else {
					this.baseRect = new Rect(baseRect.x, baseRect.y, baseRect.width, AssetGraphGUISettings.NODE_BASE_HEIGHT);
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

		public static void ShowPackageMenu (string currentPackage, Action NoneSelected, Action<string> ExistSelected) {
			List<string> packageList = new List<string>();
			var selection = 0;

			// first is None.
			packageList.Add(AssetGraphSettings.PLATFORM_NONE_PACKAGE);//0

			// delim
			packageList.Add(string.Empty);//1

			packageList.AddRange(NodeSharedPackages);//2
			if (NodeSharedPackages.Contains(currentPackage)) selection = 2 + NodeSharedPackages.FindIndex(package => package == currentPackage);

			// delim
			packageList.Add(string.Empty);

			var menu = new GenericMenu();
			for (var i = 0; i < packageList.Count; i++) {
				var packageName = packageList[i];
				switch (i) {
					case 0: {
						menu.AddItem(
							new GUIContent(packageName), 
							(i == selection),
							() => NoneSelected()
						);
						continue;
					}
					default: {
						menu.AddItem(
							new GUIContent(packageName), 
							(i == selection),
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