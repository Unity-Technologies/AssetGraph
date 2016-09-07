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

		public static BuildTargetGroup currentEditingGroup = 
			BuildTargetUtility.DefaultTarget;

		private List<Action> messageActions;

		public override bool RequiresConstantRepaint() {
			return true;
		}

		private void DoInspectorLoaderGUI (NodeGUI node) {
			if (node.loadPath == null) {
				return;
			}

			EditorGUILayout.HelpBox("Loader: Load assets in given directory path.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = DrawOverrideTargetToggle(node, node.loadPath.ContainsValueOf(currentEditingGroup), (bool b) => {
					if(b) {
						node.loadPath[currentEditingGroup] = node.loadPath.DefaultValue;
					} else {
						node.loadPath.Remove(currentEditingGroup);
					}
				});

				using (disabledScope) {
					EditorGUILayout.LabelField("Load Path:");
					var newLoadPath = EditorGUILayout.TextField(
						SystemDataUtility.GetProjectName() + AssetBundleGraphSettings.ASSETS_PATH,
						node.loadPath[currentEditingGroup]
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

					if (newLoadPath !=	node.loadPath[currentEditingGroup]) {
						node.BeforeSave();
						node.loadPath[currentEditingGroup] = newLoadPath;
						node.Save();
					}
				}
			}
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
		}

		private void DoInspectorModifierGUI (NodeGUI node) {
			EditorGUILayout.HelpBox("Modifier: Force apply asset settings to given assets.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

//			var currentModifierTargetType = IntegratedGUIModifier.ModifierOperationTargetTypeName(node.nodeId);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				// show incoming type of Assets and reset interface.
				{
					var isOperationDataExist = false;
					IntegratedGUIModifier.ValidateModifiyOperationData(
						node.nodeId,
						currentEditingGroup,
						() => {
							GUILayout.Label("No modifier data found, please Reload first.");
						},
						() => {
							isOperationDataExist = true;
						}
					);
					
					if (!isOperationDataExist) {
						return;
					}

//					using (new EditorGUILayout.HorizontalScope()) {
//						GUILayout.Label("Target Type:");
//						GUILayout.Label(currentModifierTargetType);
//					}

					/*
						reset whole platform's data for this modifier.
					*/
					if (GUILayout.Button("Reset Modifier")) {
						var modifierFolderPath = FileUtility.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, node.nodeId);
						FileUtility.RemakeDirectory(modifierFolderPath);
						node.Save();
						modifierOperatorInstance = null;
						return;
					}
				}

				GUILayout.Space(10f);

				var usingScriptMode = !string.IsNullOrEmpty(node.scriptClassName);

				// use modifier script manually.
				{
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
					
					var before = !string.IsNullOrEmpty(node.scriptClassName);
					usingScriptMode = EditorGUILayout.ToggleLeft("Use ModifierOperator Script", !string.IsNullOrEmpty(node.scriptClassName));
					
					// detect mode changed.
					if (before != usingScriptMode) {
						// checked. initialize value of scriptClassName.
						if (usingScriptMode) {
							node.BeforeSave();
							node.scriptClassName = "MyModifier";
							node.Save();
						}

						// unchecked.
						if (!usingScriptMode) {
							node.BeforeSave();
							node.scriptClassName = string.Empty;
							node.Save();
						}
					}
					
					if (!usingScriptMode) {
						EditorGUI.BeginDisabledGroup(true);	
					}
					GUILayout.Label("ここをドロップダウンにする。2");
					var newScriptClass = EditorGUILayout.TextField("Classname", node.scriptClassName, s);
					if (newScriptClass != node.scriptClassName) {
						node.BeforeSave();
						node.scriptClassName = newScriptClass;
						node.Save();
					}
					if (!usingScriptMode) {
						EditorGUI.EndDisabledGroup();	
					}
				}

				GUILayout.Space(10f);

				if (usingScriptMode) {
					EditorGUI.BeginDisabledGroup(true);
				}

				/*
					if platform tab is changed, renew modifierOperatorInstance for that tab.
				*/
				if(DrawPlatformSelector(node)) {
					modifierOperatorInstance = null;
				};

				/*
					reload modifierOperator instance from saved modifierOperator data.
				*/
				if (modifierOperatorInstance == null) {
					modifierOperatorInstance = IntegratedGUIModifier.CreateModifierOperator(node.nodeId, currentEditingGroup);
				}

				/*
					Show ModifierOperator Inspector.
				*/
				if (modifierOperatorInstance != null) {
					Action onChangedAction = () => {
						IntegratedGUIModifier.SaveModifierOperatorToDisk(
							node.nodeId, currentEditingGroup, modifierOperatorInstance);

						// reflect change of data.
						AssetDatabase.Refresh();
						
						modifierOperatorInstance = null;
					};

					GUILayout.Space(10f);

					modifierOperatorInstance.DrawInspector(onChangedAction);
				}

				if (usingScriptMode) {
					EditorGUI.EndDisabledGroup();
				}
			}
		}

		/*
			・NonSerializedをセットしないと、ModifierOperators.OperatorBase型に戻ってしまう。
			・SerializeFieldにする or なにもつけないと、もれなくModifierOperators.OperatorBase型にもどる
			・Undo/Redoを行うためには、ModifierOperators.OperatorBaseを拡張した型のメンバーをUndo/Redo対象にしなければいけない
			・ModifierOperators.OperatorBase意外に晒していい型がない

			という無茶苦茶な難題があります。
			Undo/Redo時にオリジナルの型に戻ってしまう、という仕様と、追加を楽にするために型定義をModifierOperators.OperatorBase型にする、
			っていうのが相反するようです。うーんどうしよう。

			TODO:
		*/
		[NonSerialized] private ModifierOperators.OperatorBase modifierOperatorInstance;

		private void DoInspectorGroupingGUI (NodeGUI node) {
			if (node.groupingKeyword == null) return;

			EditorGUILayout.HelpBox("Grouping: Create group of assets.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			DrawPlatformSelector(node);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var newGroupingKeyword = EditorGUILayout.TextField("Grouping Keyword",node.groupingKeyword[currentEditingGroup]);
				EditorGUILayout.HelpBox(
					"Grouping Keyword requires \"*\" in itself. It assumes there is a pattern such as \"ID_0\" in incoming paths when configured as \"ID_*\" ", 
					MessageType.Info);

				IntegratedGUIGrouping.ValidateGroupingKeyword(
					newGroupingKeyword,
					() => {
//						EditorGUILayout.HelpBox("groupingKeyword is empty.", MessageType.Error);
					},
					() => {
//						EditorGUILayout.HelpBox("grouping keyword does not contain " + AssetBundleGraphSettings.KEYWORD_WILDCARD + " groupingKeyword:" + newGroupingKeyword, MessageType.Error);
					}
				);

				if (newGroupingKeyword != node.groupingKeyword[currentEditingGroup]) {
					node.BeforeSave();
					node.groupingKeyword[currentEditingGroup] = newGroupingKeyword;
					node.Save();
				}
			}
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
				
				GUILayout.Label("ここをドロップダウンにする。");
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

			//Show target configuration tab
			DrawPlatformSelector(node);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var bundleNameTemplate = EditorGUILayout.TextField("Bundle Name Template", node.bundleNameTemplate[currentEditingGroup]).ToLower();

				IntegratedGUIBundlizer.ValidateBundleNameTemplate(
					bundleNameTemplate,
					() => {
//						EditorGUILayout.HelpBox("No Bundle Name Template set.", MessageType.Error);
					}
				);

				if (bundleNameTemplate != node.bundleNameTemplate[currentEditingGroup]) {
					node.BeforeSave();
					node.bundleNameTemplate[currentEditingGroup] = bundleNameTemplate;
					node.Save();
				}

				GUILayout.Label("Variants:");
				for (int i = 0; i < node.variants.Keys.Count; ++i) {

					var inputConnectionId = node.variants.Keys[i];

					using (new GUILayout.HorizontalScope()) {
						if (GUILayout.Button("-", GUILayout.Width(30))) {
							node.BeforeSave();
							node.variants.Remove(inputConnectionId);
							node.DeleteInputPoint(inputConnectionId);
						}
						else {
							var variantName = node.variants.Values[i];

							GUIStyle s = new GUIStyle((GUIStyle)"TextFieldDropDownText");
							Action makeStyleBold = () => {
								s.fontStyle = FontStyle.Bold;
								s.fontSize = 12;
							};

							IntegratedGUIBundlizer.ValidateVariantName(variantName, node.variants.Values, 
								makeStyleBold,
								makeStyleBold,
								makeStyleBold);

							variantName = EditorGUILayout.TextField(variantName, s);

							if (variantName != node.variants.Values[i]) {
								node.BeforeSave();
								node.variants.Values[i] = variantName;
								node.RenameInputPoint(inputConnectionId, variantName);
							}
						}
					}
				}

				if (GUILayout.Button("+")) {
					node.BeforeSave();
					var newid = Guid.NewGuid().ToString();
					node.variants.Add(newid, AssetBundleGraphSettings.BUNDLIZER_VARIANTNAME_DEFAULT);
					node.AddInputPoint(newid, AssetBundleGraphSettings.BUNDLIZER_VARIANTNAME_DEFAULT);
				}

			}
		}

		private void DoInspectorBundleBuilderGUI (NodeGUI node) {
			if (node.enabledBundleOptions == null) return;

			EditorGUILayout.HelpBox("BundleBuilder: Build asset bundles with given asset bundle settings.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			DrawPlatformSelector(node);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				int bundleOptions = node.enabledBundleOptions[currentEditingGroup];

				foreach (var option in AssetBundleGraphSettings.BundleOptionSettings) {

					// contains keyword == enabled. if not, disabled.
					bool isEnabled = (bundleOptions & (int)option.option) != 0;

					var result = EditorGUILayout.ToggleLeft(option.description, isEnabled);
					if (result != isEnabled) {
						node.BeforeSave();

						bundleOptions = (result) ? ((int)option.option | bundleOptions) : (((~(int)option.option)) & bundleOptions);

						node.enabledBundleOptions[currentEditingGroup] = bundleOptions;

						/*
							Cannot use DisableWriteTypeTree and IgnoreTypeTreeChanges options together.
						*/
						if (result &&
							option.option == BuildAssetBundleOptions.DisableWriteTypeTree &&
							0 != (node.enabledBundleOptions[currentEditingGroup] & (int)BuildAssetBundleOptions.DisableWriteTypeTree))
						{
							var currentValue = node.enabledBundleOptions[currentEditingGroup];
							node.enabledBundleOptions[currentEditingGroup] = (((~(int)BuildAssetBundleOptions.DisableWriteTypeTree)) & currentValue);
						}

						if (result &&
							option.option == BuildAssetBundleOptions.IgnoreTypeTreeChanges &&
							0 != (node.enabledBundleOptions[currentEditingGroup] & (int)BuildAssetBundleOptions.IgnoreTypeTreeChanges))
						{
							var currentValue = node.enabledBundleOptions[currentEditingGroup];
							node.enabledBundleOptions[currentEditingGroup] = (((~(int)BuildAssetBundleOptions.IgnoreTypeTreeChanges)) & currentValue);
						}

						node.Save();
						return;
					}
				}
			}
		}


		private void DoInspectorExporterGUI (NodeGUI node) {
			if (node.exportTo == null) return;

			EditorGUILayout.HelpBox("Exporter: Export given files to output directory.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			DrawPlatformSelector(node);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				EditorGUILayout.LabelField("Export Path:");
				var newExportPath = EditorGUILayout.TextField(
					SystemDataUtility.GetProjectName(), 
					node.exportTo[currentEditingGroup]
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


				if (newExportPath != node.exportTo[currentEditingGroup]) {
					node.BeforeSave();
					node.exportTo[currentEditingGroup] = newExportPath;
					node.Save();
				}
			}
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

		private void ShowFilterKeyTypeMenu (string current, Action<string> ExistSelected) {
			var menu = new GenericMenu();
			
			menu.AddDisabledItem(new GUIContent(current));
			
			menu.AddSeparator(string.Empty);
			
			for (var i = 0; i < TypeUtility.KeyTypes.Count; i++) {
				var type = TypeUtility.KeyTypes[i];
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

		/*
		 *  Return true if Platform is changed
		 */ 
		private bool DrawPlatformSelector (NodeGUI node) {
			BuildTargetGroup g = currentEditingGroup;


			EditorGUI.BeginChangeCheck();
			using (new EditorGUILayout.HorizontalScope()) {
				var choosenIndex = -1;
				for (var i = 0; i < NodeGUIUtility.platformButtons.Length; i++) {
					var onOffBefore = NodeGUIUtility.platformButtons[i].targetGroup == currentEditingGroup;
					var onOffAfter = onOffBefore;

					GUIStyle toolbarbutton = new GUIStyle("toolbarbutton");

					if(NodeGUIUtility.platformButtons[i].targetGroup == BuildTargetUtility.DefaultTarget) {
						onOffAfter = GUILayout.Toggle(onOffBefore, NodeGUIUtility.platformButtons[i].ui, toolbarbutton);
					} else {
						var width = Mathf.Max(32f, toolbarbutton.CalcSize(NodeGUIUtility.platformButtons[i].ui).x);
						onOffAfter = GUILayout.Toggle(onOffBefore, NodeGUIUtility.platformButtons[i].ui, toolbarbutton, GUILayout.Width( width ));
					}

					if (onOffBefore != onOffAfter) {
						choosenIndex = i;
						break;
					}
				}

				if (EditorGUI.EndChangeCheck()) {
					g = NodeGUIUtility.platformButtons[choosenIndex].targetGroup;
				}
			}

			if (g != currentEditingGroup) {
				currentEditingGroup = g;
				GUI.FocusControl(string.Empty);
			}

			return g != currentEditingGroup;
		}

		private EditorGUI.DisabledScope DrawOverrideTargetToggle(NodeGUI node, bool status, Action<bool> onStatusChange) {

			if( currentEditingGroup == BuildTargetUtility.DefaultTarget ) {
				return new EditorGUI.DisabledScope(false);
			}

			bool newStatus = GUILayout.Toggle(status, 
				"Override for " + NodeGUIUtility.GetPlatformButtonFor(currentEditingGroup).ui.tooltip);
			
			if(newStatus != status && onStatusChange != null) {
				onStatusChange(newStatus);
			}
			return new EditorGUI.DisabledScope(!newStatus);
		}
	}
}