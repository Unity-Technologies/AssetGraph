using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
	/**
		GUI Inspector to NodeGUI (Through NodeGUIInspectorHelper)
	*/
	[CustomEditor(typeof(NodeGUIInspectorHelper))]
	public class NodeGUIEditor : Editor {

		private List<Action> messageActions;

		private bool packageEditMode = false;

		public override bool RequiresConstantRepaint() {
			return true;
		}

		private void DoInspectorLoaderGUI (NodeGUI node) {
			if (node.loadPath == null) return;

			EditorGUILayout.HelpBox("Loader: Load assets in given directory path.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			/*
				platform & package
			*/
			if (packageEditMode) EditorGUI.BeginDisabledGroup(true);

			// update platform & package.
			node.currentPlatform = UpdateCurrentPlatform(node.currentPlatform);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				EditorGUILayout.LabelField("Load Path:");
				var newLoadPath = EditorGUILayout.TextField(
					SystemDataUtility.GetProjectName() + AssetBundleGraphSettings.ASSETS_PATH, 
					SystemDataUtility.GetPlatformValue(
						node.loadPath.ReadonlyDict(), 
						node.currentPlatform
					).ToString()
				);
				var loaderNodePath = FileUtility.GetPathWithAssetsPath(newLoadPath);
				IntegratedGUILoader.ValidateLoadPath(
					newLoadPath,
					loaderNodePath,
					() => {
						//EditorGUILayout.HelpBox("load path is empty.", MessageType.Error);
					},
					() => {
						//EditorGUILayout.HelpBox("Directory not found:" + loaderNodePath, MessageType.Error);
					}
				);

				if (newLoadPath !=	SystemDataUtility.GetPlatformValue(
					node.loadPath.ReadonlyDict(),
					node.currentPlatform
				).ToString()
				) {
					node.BeforeSave();
					node.loadPath.Add(SystemDataUtility.CreateKeyNameFromString(node.currentPlatform), newLoadPath);
					node.Save();
				}
			}

			if (packageEditMode) EditorGUI.EndDisabledGroup();
			UpdateDeleteSetting(node);
		}

		private void DoInspectorFilterScriptGUI (NodeGUI node) {
			EditorGUILayout.HelpBox("Filter(Script): Filter given assets by script.", MessageType.Info);
			UpdateNodeName(node);

			EditorGUILayout.LabelField("Script Path", node.scriptPath);

			var outputPointLabels = node.OutputPointLabels();
			EditorGUILayout.LabelField("connectionPoints Count", outputPointLabels.Count.ToString());

			foreach (var label in outputPointLabels) {
				EditorGUILayout.LabelField("label", label);
			}
		}

		private void DoInspectorFilterGUI (NodeGUI node) {
			EditorGUILayout.HelpBox("Filter: Filter incoming assets by keywords and types. You can use regular expressions for keyword field.", MessageType.Info);
			UpdateNodeName(node);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				GUILayout.Label("Filter Settings:");
				for (int i = 0; i < node.filterContainsKeywords.Count; i++) {

					Action messageAction = null;

					using (new GUILayout.HorizontalScope()) {
						if (GUILayout.Button("-", GUILayout.Width(30))) {
							node.BeforeSave();
							node.filterContainsKeywords.RemoveAt(i);
							node.filterContainsKeytypes.RemoveAt(i);
							node.DeleteFilterOutputPoint(i);
						}
						else {
							var newContainsKeyword = node.filterContainsKeywords[i];

							/*
												generate keyword + keytype string for compare exists setting vs new modifying setting at once.
											*/
							var currentKeywordsSource = new List<string>(node.filterContainsKeywords);
							var currentKeytypesSource = new List<string>(node.filterContainsKeytypes);

							var currentKeytype = currentKeytypesSource[i];

							for (var j = 0; j < currentKeywordsSource.Count; j++) {
								currentKeywordsSource[j] = currentKeywordsSource[j] + currentKeytypesSource[j];
							}

							// remove current choosing one from compare target.
							currentKeywordsSource.RemoveAt(i);
							var currentKeywords = new List<string>(currentKeywordsSource);

							GUIStyle s = new GUIStyle((GUIStyle)"TextFieldDropDownText");

							IntegratedGUIFilter.ValidateFilter(
								newContainsKeyword + currentKeytype,
								currentKeywords,
								() => {
									s.fontStyle = FontStyle.Bold;
									s.fontSize = 12;
								},
								() => {
									s.fontStyle = FontStyle.Bold;
									s.fontSize = 12;
								}
							);

							using (new EditorGUILayout.HorizontalScope()) {
								newContainsKeyword = EditorGUILayout.TextField(node.filterContainsKeywords[i], s, GUILayout.Width(120));
								var currentIndex = i;
								if (GUILayout.Button(node.filterContainsKeytypes[i], "Popup")) {
									NodeGUI.ShowFilterKeyTypeMenu(
										node.filterContainsKeytypes[currentIndex],
										(string selectedTypeStr) => {
											node.BeforeSave();
											node.filterContainsKeytypes[currentIndex] = selectedTypeStr;
											node.Save();
										} 
									);
								}
							}

							if (newContainsKeyword != node.filterContainsKeywords[i]) {
								node.BeforeSave();
								node.filterContainsKeywords[i] = newContainsKeyword;
								node.RenameFilterOutputPointLabel(i, node.filterContainsKeywords[i]);
							}
						}
					}

					if(messageAction != null) {
						using (new GUILayout.HorizontalScope()) {
							messageAction.Invoke();
						}
					}
				}

				// add contains keyword interface.
				if (GUILayout.Button("+")) {
					node.BeforeSave();
					var addingIndex = node.filterContainsKeywords.Count;
					var newKeyword = AssetBundleGraphSettings.DEFAULT_FILTER_KEYWORD;

					node.filterContainsKeywords.Add(newKeyword);
					node.filterContainsKeytypes.Add(AssetBundleGraphSettings.DEFAULT_FILTER_KEYTYPE);

					node.AddFilterOutputPoint(addingIndex, AssetBundleGraphSettings.DEFAULT_FILTER_KEYWORD);
				}
			}
		}

		private void DoInspectorImportSettingGUI (NodeGUI node) {
			EditorGUILayout.HelpBox("ImportSetting: Force apply import settings to given assets.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			if (packageEditMode) {
				EditorGUI.BeginDisabledGroup(true);
			}
			/*
							importer node has no platform key. 
							platform key is contained by Unity's importer inspector itself.
						*/

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var nodeId = node.nodeId;

				var samplingPath = FileUtility.PathCombine(AssetBundleGraphSettings.IMPORTER_SETTINGS_PLACE, nodeId);

				IntegratedGUIImportSetting.ValidateImportSample(samplingPath,
					(string noFolderFound) => {
						EditorGUILayout.LabelField("Sampling Asset", "No sample asset found. please Reload first.");
					},
					(string noFilesFound) => {
						EditorGUILayout.LabelField("Sampling Asset", "No sample asset found. please Reload first.");
					},
					(string samplingAssetPath) => {
						EditorGUILayout.LabelField("Sampling Asset Path", samplingAssetPath);
						if (GUILayout.Button("Setup Import Setting")) {
							var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(samplingAssetPath);
							Selection.activeObject = obj;
						}
						if (GUILayout.Button("Reset Import Setting")) {
							// delete all import setting files.
							FileUtility.RemakeDirectory(samplingPath);
							node.Save();
						}
					},
					(string tooManyFilesFoundMessage) => {
						if (GUILayout.Button("Reset Import Setting")) {
							// delete all import setting files.
							FileUtility.RemakeDirectory(samplingPath);
							node.Save();
						}
					}
				);
			}

			if (packageEditMode) {
				EditorGUI.EndDisabledGroup();
			}
			UpdateDeleteSetting(node);
		}

		private void DoInspectorModifierGUI (NodeGUI node) {
			//Debug.LogError("modifier node insp.");
		}
		private void DoInspectorGroupingGUI (NodeGUI node) {
			if (node.groupingKeyword == null) return;

			EditorGUILayout.HelpBox("Grouping: Create group of assets.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			node.currentPlatform = UpdateCurrentPlatform(node.currentPlatform);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var newGroupingKeyword = EditorGUILayout.TextField(
					"Grouping Keyword",
					SystemDataUtility.GetPlatformValue(
						node.groupingKeyword.ReadonlyDict(), 
						node.currentPlatform
					).ToString()
				);
				EditorGUILayout.HelpBox("Grouping Keyword requires \"*\" in itself. It assumes there is a pattern such as \"ID_0\" in incoming paths when configured as \"ID_*\" ", MessageType.Info);


				IntegratedGUIGrouping.ValidateGroupingKeyword(
					newGroupingKeyword,
					() => {
//						EditorGUILayout.HelpBox("groupingKeyword is empty.", MessageType.Error);
					},
					() => {
//						EditorGUILayout.HelpBox("grouping keyword does not contain " + AssetBundleGraphSettings.KEYWORD_WILDCARD + " groupingKeyword:" + newGroupingKeyword, MessageType.Error);
					}
				);

				if (newGroupingKeyword != SystemDataUtility.GetPlatformValue(
					node.groupingKeyword.ReadonlyDict(), 
					node.currentPlatform
				).ToString()
				) {
					node.BeforeSave();
					node.groupingKeyword.Add(SystemDataUtility.CreateKeyNameFromString(node.currentPlatform), newGroupingKeyword);
					node.Save();
				}
			}

			UpdateDeleteSetting(node);
		}

		private void DoInspectorPrefabricatorScriptGUI (NodeGUI node) {
			EditorGUILayout.HelpBox("Prefabricator: Create prefab with given assets and script.", MessageType.Info);
			UpdateNodeName(node);

			EditorGUILayout.LabelField("Script Path", node.scriptPath);
		}

		private void DoInspectorPrefabricatorGUI (NodeGUI node) {
			EditorGUILayout.HelpBox("Prefabricator: Create prefab with given assets and script.", MessageType.Info);
			UpdateNodeName(node);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {

				GUIStyle s = new GUIStyle("TextFieldDropDownText");

				/*
					check prefabricator script-type string.
				*/
				if (string.IsNullOrEmpty(node.scriptClassName)) {
					s.fontStyle = FontStyle.Bold;
					s.fontSize  = 12;
				} else {
					var loadedType = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(node.scriptClassName);

					if (loadedType == null) {
						s.fontStyle = FontStyle.Bold;
						s.fontSize  = 12;
					}
				}


				var newScriptClass = EditorGUILayout.TextField("Classname", node.scriptClassName, s);

				if (newScriptClass != node.scriptClassName) {
					node.BeforeSave();
					node.scriptClassName = newScriptClass;
					node.Save();
				}
			}
		}
		
		private void DoInspectorBundlizerGUI (NodeGUI node) {
			if (node.bundleNameTemplate == null) return;

			EditorGUILayout.HelpBox("Bundlizer: Create asset bundle settings with given group of assets.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			node.currentPlatform = UpdateCurrentPlatform(node.currentPlatform);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var bundleNameTemplate = EditorGUILayout.TextField(
					"Bundle Name Template", 
					SystemDataUtility.GetPlatformValue(
						node.bundleNameTemplate.ReadonlyDict(), 
						node.currentPlatform
					).ToString()
				).ToLower();

				IntegratedGUIBundlizer.ValidateBundleNameTemplate(
					bundleNameTemplate,
					() => {
//						EditorGUILayout.HelpBox("No Bundle Name Template set.", MessageType.Error);
					}
				);

				if (bundleNameTemplate != SystemDataUtility.GetPlatformValue(
					node.bundleNameTemplate.ReadonlyDict(), 
					node.currentPlatform
				).ToString()
				) {
					node.BeforeSave();
					node.bundleNameTemplate.Add(SystemDataUtility.CreateKeyNameFromString(node.currentPlatform), bundleNameTemplate);
					node.Save();
				}

				var isUseOutputResoruces = SystemDataUtility.GetPlatformValue(
					node.bundleUseOutput.ReadonlyDict(), 
					node.currentPlatform
				).ToString().ToLower();

				var useOrNot = false;
				switch (isUseOutputResoruces) {
				case "true": {
						useOrNot = true;
						break;
					}
				}
				
				var result = EditorGUILayout.ToggleLeft("Asset Output for Dependency", useOrNot);

				if (result != useOrNot) {
					node.BeforeSave();

					if (result) node.AddBundlizerDependencyOutput();
					else node.RemoveBundlizerDependencyOutput(); 

					node.bundleUseOutput.Add(SystemDataUtility.CreateKeyNameFromString(node.currentPlatform), result.ToString());
					node.Save();
				}

				EditorGUILayout.HelpBox("Check this to enable asset output slot to create asset bundle which has dependency to asset bundle of this node.", MessageType.Info);
			}

			UpdateDeleteSetting(node);
		}

		private void DoInspectorBundleBuilderGUI (NodeGUI node) {
			if (node.enabledBundleOptions == null) return;

			EditorGUILayout.HelpBox("BundleBuilder: Build asset bundles with given asset bundle settings.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			node.currentPlatform = UpdateCurrentPlatform(node.currentPlatform);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var bundleOptions = SystemDataUtility.GetPlatformValue(
					node.enabledBundleOptions.ReadonlyDict(),
					node.currentPlatform
				);

				var plartform_pakcage_key = SystemDataUtility.CreateKeyNameFromString(node.currentPlatform);

				for (var i = 0; i < AssetBundleGraphSettings.DefaultBundleOptionSettings.Count; i++) {
					var enablablekey = AssetBundleGraphSettings.DefaultBundleOptionSettings[i];

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
									SystemDataUtility.CreateKeyNameFromString(node.currentPlatform),
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
									SystemDataUtility.CreateKeyNameFromString(node.currentPlatform),
									currentEnableds
								);
							}
						}

						/*
										Cannot use options DisableWriteTypeTree and IgnoreTypeTreeChanges at the same time.
									*/
						if (enablablekey == "Disable Write TypeTree" && result &&
							node.enabledBundleOptions.ReadonlyDict()[SystemDataUtility.CreateKeyNameFromString(node.currentPlatform)].Contains("Ignore TypeTree Changes")) {

							var newEnableds = node.enabledBundleOptions.ReadonlyDict()[SystemDataUtility.CreateKeyNameFromString(node.currentPlatform)];
							newEnableds.Remove("Ignore TypeTree Changes");

							node.enabledBundleOptions.Add(
								SystemDataUtility.CreateKeyNameFromString(node.currentPlatform),
								newEnableds
							);
						}

						if (enablablekey == "Ignore TypeTree Changes" && result &&
							node.enabledBundleOptions.ReadonlyDict()[SystemDataUtility.CreateKeyNameFromString(node.currentPlatform)].Contains("Disable Write TypeTree")) {

							var newEnableds = node.enabledBundleOptions.ReadonlyDict()[SystemDataUtility.CreateKeyNameFromString(node.currentPlatform)];
							newEnableds.Remove("Disable Write TypeTree");

							node.enabledBundleOptions.Add(
								SystemDataUtility.CreateKeyNameFromString(node.currentPlatform),
								newEnableds
							);
						}

						node.Save();
						return;
					}
				}
			}

			UpdateDeleteSetting(node);

		}


		private void DoInspectorExporterGUI (NodeGUI node) {
			if (node.exportTo == null) return;

			EditorGUILayout.HelpBox("Exporter: Export given files to output directory.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			node.currentPlatform = UpdateCurrentPlatform(node.currentPlatform);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				EditorGUILayout.LabelField("Export Path:");
				var newExportPath = EditorGUILayout.TextField(
					SystemDataUtility.GetProjectName(), 
					SystemDataUtility.GetPlatformValue(
						node.exportTo.ReadonlyDict(), 
						node.currentPlatform
					).ToString()
				);

				var exporterrNodePath = FileUtility.GetPathWithProjectPath(newExportPath);
				if(IntegratedGUIExporter.ValidateExportPath(
					newExportPath,
					exporterrNodePath,
					() => {
						// TODO Make text field bold
					},
					() => {
						using (new EditorGUILayout.HorizontalScope()) {
							EditorGUILayout.LabelField(exporterrNodePath + " does not exist.");
							if(GUILayout.Button("Create directory")) {
								Directory.CreateDirectory(exporterrNodePath);
								node.Save();
							}
						}
						EditorGUILayout.Space();

						EditorGUILayout.LabelField("Available Directories:");
						string[] dirs = Directory.GetDirectories(Path.GetDirectoryName(exporterrNodePath));
						foreach(string s in dirs) {
							EditorGUILayout.LabelField(s);
						}
					}
				)) {
					using (new EditorGUILayout.HorizontalScope()) {
						GUILayout.FlexibleSpace();
						#if UNITY_EDITOR_OSX
						string buttonName = "Reveal in Finder";
						#else
						string buttonName = "Show in Explorer";
						#endif 
						if(GUILayout.Button(buttonName)) {
							EditorUtility.RevealInFinder(exporterrNodePath);
						}
					}
				}


				if (newExportPath != SystemDataUtility.GetPlatformValue(
					node.exportTo.ReadonlyDict(),
					node.currentPlatform
				).ToString()
				) {
					node.BeforeSave();
					node.exportTo.Add(SystemDataUtility.CreateKeyNameFromString(node.currentPlatform), newExportPath);
					node.Save();
				}
			}

			UpdateDeleteSetting(node);
		}


		public override void OnInspectorGUI () {
			var currentTarget = (NodeGUIInspectorHelper)target;
			var node = currentTarget.node;
			if (node == null) return;

			if(messageActions == null) {
				messageActions = new List<Action>();
			}

			messageActions.Clear();

			switch (node.kind) {
			case AssetBundleGraphSettings.NodeKind.LOADER_GUI:
				DoInspectorLoaderGUI(node);
				break;
			case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT:
				DoInspectorFilterScriptGUI(node);
				break;
			case AssetBundleGraphSettings.NodeKind.FILTER_GUI:
				DoInspectorFilterGUI(node);
				break;
			case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI :
				DoInspectorImportSettingGUI(node);
				break;
			case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI :
				DoInspectorModifierGUI(node);
				break;
			case AssetBundleGraphSettings.NodeKind.GROUPING_GUI:
				DoInspectorGroupingGUI(node);
				break;
			case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				DoInspectorPrefabricatorScriptGUI(node);
				break;
			case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI:
				DoInspectorPrefabricatorGUI(node);
				break;
			case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI:
				DoInspectorBundlizerGUI(node);
				break;
			case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI:
				DoInspectorBundleBuilderGUI(node);
				break;
			case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: 
				DoInspectorExporterGUI(node);
				break;
			default: 
				Debug.LogError(node.name + " is defined as unknown kind of node. value:" + node.kind);
				break;
			}

			var errors = currentTarget.errors;
			if (errors != null && errors.Any()) {
				foreach (var error in errors) {
					EditorGUILayout.HelpBox(error, MessageType.Error);
				}
			}
			using (new EditorGUILayout.VerticalScope()) {
				foreach(Action a in messageActions) {
					a.Invoke();
				}
			}
		}

		private void UpdateNodeName (NodeGUI node) {
			var newName = EditorGUILayout.TextField("Node Name", node.name);

			if( NodeGUIUtility.allNodeNames != null ) {
				var overlapping = NodeGUIUtility.allNodeNames.GroupBy(x => x)
					.Where(group => group.Count() > 1)
					.Select(group => group.Key);
				if (overlapping.Any() && overlapping.Contains(newName)) {
					EditorGUILayout.HelpBox("This node name already exist. Please put other name:" + newName, MessageType.Error);
					AssetBundleGraphEditorWindow.AddNodeException(new NodeException("Node name " + newName + " already exist.", node.nodeId ));
				}
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
				for (var i = 0; i < NodeGUIUtility.platformButtonTextures.Length; i++) {
					var onOffBefore = NodeGUIUtility.platformStrings[i] == basePlatfrom;
					var onOffAfter = onOffBefore;

					// index 0 is Default.
					switch (i) {
					case 0: {
							onOffAfter = GUILayout.Toggle(onOffBefore, "Default", "toolbarbutton");
							break;
						}
					default: {
							// for each platform texture.
							var platformButtonTexture = NodeGUIUtility.platformButtonTextures[i];
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
					newPlatform = NodeGUIUtility.platformStrings[choosenIndex];
				}
			}

			if (newPlatform != basePlatfrom) GUI.FocusControl(string.Empty);
			return newPlatform;
		}


		private void UpdateDeleteSetting (NodeGUI currentNode) {
			var currentNodePlatformPackageKey = SystemDataUtility.CreateKeyNameFromString(currentNode.currentPlatform);

			if (currentNodePlatformPackageKey == AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME) return;

			using (new EditorGUILayout.HorizontalScope()) {
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Use Default Setting", GUILayout.Width(150))) {
					currentNode.BeforeSave();
					currentNode.DeleteCurrentPackagePlatformKey(currentNodePlatformPackageKey);
					GUI.FocusControl(string.Empty);
					currentNode.Save();
				}
			}
		}
	}
}