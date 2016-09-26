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
			if (node.Data.LoaderLoadPath == null) {
				return;
			}

			EditorGUILayout.HelpBox("Loader: Load assets in given directory path.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = DrawOverrideTargetToggle(node, node.Data.LoaderLoadPath.ContainsValueOf(currentEditingGroup), (bool b) => {
					using(new RecordUndoScope("Remove Target Load Path Settings", node, true)) {
						if(b) {
							node.Data.LoaderLoadPath[currentEditingGroup] = node.Data.LoaderLoadPath.DefaultValue;
						} else {
							node.Data.LoaderLoadPath.Remove(currentEditingGroup);
						}
					}
				});

				using (disabledScope) {
					EditorGUILayout.LabelField("Load Path:");
					var newLoadPath = EditorGUILayout.TextField(
						SystemDataUtility.GetProjectName() + AssetBundleGraphSettings.ASSETS_PATH,
						node.Data.LoaderLoadPath[currentEditingGroup]
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

					if (newLoadPath !=	node.Data.LoaderLoadPath[currentEditingGroup]) {
						using(new RecordUndoScope("Load Path Changed", node, true)){
							node.Data.LoaderLoadPath[currentEditingGroup] = newLoadPath;
						}
					}
				}
			}
		}

		private void DoInspectorFilterScriptGUI (NodeGUI node) {
			EditorGUILayout.HelpBox("Filter(Script): Filter given assets by script.", MessageType.Info);
			UpdateNodeName(node);

			EditorGUILayout.LabelField("Script:", node.Data.ScriptClassName);

			var outputs = node.Data.OutputPoints;
			EditorGUILayout.LabelField("ConnectionPoints Count", outputs.Count.ToString());

			foreach (var point in outputs) {
				EditorGUILayout.LabelField("label", point.Label);
			}
		}

		private void DoInspectorFilterGUI (NodeGUI node) {
			EditorGUILayout.HelpBox("Filter: Filter incoming assets by keywords and types. You can use regular expressions for keyword field.", MessageType.Info);
			UpdateNodeName(node);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				GUILayout.Label("Filter Settings:");
				for (int i= 0; i < node.Data.FilterConditions.Count; ++i) {
					var cond = node.Data.FilterConditions[i];

					Action messageAction = null;

					using (new GUILayout.HorizontalScope()) {
						if (GUILayout.Button("-", GUILayout.Width(30))) {
							using(new RecordUndoScope("Remove Filter Condition", node)){
								node.Data.RemoveFilterCondition(cond);
							}
						}
						else {
							var newContainsKeyword = cond.FilterKeyword;

							GUIStyle s = new GUIStyle((GUIStyle)"TextFieldDropDownText");

							using (new EditorGUILayout.HorizontalScope()) {
								newContainsKeyword = EditorGUILayout.TextField(cond.FilterKeyword, s, GUILayout.Width(120));
								if (GUILayout.Button(cond.FilterKeytype , "Popup")) {
									var ind = i;// need this because of closure locality bug in unity C#
									NodeGUI.ShowFilterKeyTypeMenu(
										cond.FilterKeytype,
										(string selectedTypeStr) => {
											using(new RecordUndoScope("Modify Filter Type", node, true)){
												node.Data.FilterConditions[ind].FilterKeytype = selectedTypeStr;
											}
										} 
									);
								}
							}

							if (newContainsKeyword != cond.FilterKeyword) {
								using(new RecordUndoScope("Modify Filter Keyword", node, true)){
									cond.FilterKeyword = newContainsKeyword;
									node.UpdateNodeRect();
								}
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
					using(new RecordUndoScope("Add Filter Condition", node)){
						node.Data.AddFilterCondition(
							AssetBundleGraphSettings.DEFAULT_FILTER_KEYWORD, 
							AssetBundleGraphSettings.DEFAULT_FILTER_KEYTYPE);
						node.UpdateNodeRect();
					}
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
				var nodeId = node.Id;

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
							using(new SaveScope(node)){
								FileUtility.RemakeDirectory(samplingPath);
							}
						}
					},
					(string tooManyFilesFoundMessage) => {
						if (GUILayout.Button("Reset Import Setting")) {
							// delete all import setting files.
							using(new SaveScope(node)){
								FileUtility.RemakeDirectory(samplingPath);
							}
						}
					}
				);
			}
		}

		private void DoInspectorModifierGUI (NodeGUI node) {
			EditorGUILayout.HelpBox("Modifier: Force apply asset settings to given assets.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

//			var currentModifierTargetType = IntegratedGUIModifier.ModifierOperationTargetTypeName(node.Id);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				// show incoming type of Assets and reset interface.
				IntegratedGUIModifier.ValidateModifiyOperationData(
					node.Id,
					BuildTargetUtility.GroupToTarget(currentEditingGroup),
					() => {
						GUILayout.Label("No modifier data found, please Reload first.");
					},
					() => {
					}
				);
				
				if (!IntegratedGUIModifier.HasModifierDataFor(node.Id, currentEditingGroup, true)) {
					return;
				}

				/*
					reset whole platform's data for this modifier.
				*/
				if (GUILayout.Button("Reset Modifier")) {
					using(new RecordUndoScope("Reset Modifier", node, true)){
						var modifierFolderPath = FileUtility.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, node.Id);
						FileUtility.RemakeDirectory(modifierFolderPath);
						modifierOperatorInstance = null;
					}
					return;
				}

				GUILayout.Space(10f);

				var usingScriptMode = !string.IsNullOrEmpty(node.Data.ScriptClassName);

				// use modifier script manually.
				{
					GUIStyle s = new GUIStyle("TextFieldDropDownText");
					/*
						check prefabricator script-type string.
					*/
					if (string.IsNullOrEmpty(node.Data.ScriptClassName)) {
						s.fontStyle = FontStyle.Bold;
						s.fontSize  = 12;
					} else {
						var loadedType = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(node.Data.ScriptClassName);

						if (loadedType == null) {
							s.fontStyle = FontStyle.Bold;
							s.fontSize  = 12;
						}
					}
					
					var before = !string.IsNullOrEmpty(node.Data.ScriptClassName);
					usingScriptMode = EditorGUILayout.ToggleLeft("Use ModifierOperator Script", !string.IsNullOrEmpty(node.Data.ScriptClassName));
					
					// detect mode changed.
					if (before != usingScriptMode) {
						// checked. initialize value of scriptClassName.
						if (usingScriptMode) {
							using(new RecordUndoScope("Change Modifier", node, true)){
								node.Data.ScriptClassName = "MyModifier";
							}
						}

						// unchecked.
						if (!usingScriptMode) {
							using(new RecordUndoScope("Change Modifier", node, true)){
								node.Data.ScriptClassName = string.Empty;
							}
						}
					}
					
					if (!usingScriptMode) {
						EditorGUI.BeginDisabledGroup(true);	
					}
					GUILayout.Label("ここをドロップダウンにする。2");
					var newScriptClass = EditorGUILayout.TextField("Classname", node.Data.ScriptClassName, s);
					if (newScriptClass != node.Data.ScriptClassName) {
						using(new RecordUndoScope("Change Script Class Name", node, true)){
							node.Data.ScriptClassName = newScriptClass;
						}
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
				using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
					var disabledScope = DrawOverrideTargetToggle(node, IntegratedGUIModifier.HasModifierDataFor(node.Id, currentEditingGroup), (bool enabled) => {
						if(enabled) {
							// do nothing
						} else {
							IntegratedGUIModifier.DeletePlatformData(node.Id, currentEditingGroup);
						}
						// reset modifier operator when state change
						modifierOperatorInstance = null;						
					});

					using (disabledScope) {
						/*
						reload modifierOperator instance from saved modifierOperator data.
						*/
						if (modifierOperatorInstance == null) {
							// CreateModifierOperator will create default modifier operator if no target specific settings are present
							modifierOperatorInstance = IntegratedGUIModifier.CreateModifierOperator(node.Id, currentEditingGroup);
						}

						/*
						Show ModifierOperator Inspector.
						*/
						if (modifierOperatorInstance != null) {
							Action onChangedAction = () => {
								IntegratedGUIModifier.SaveModifierOperatorToDisk(
									node.Id, currentEditingGroup, modifierOperatorInstance);

								// reflect change of data.
								AssetDatabase.Refresh();

								modifierOperatorInstance = null;
							};

							GUILayout.Space(10f);

							modifierOperatorInstance.DrawInspector(onChangedAction);
						}
					}
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
			if (node.Data.GroupingKeywords == null) {
				return;
			}

			EditorGUILayout.HelpBox("Grouping: Create group of assets.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = DrawOverrideTargetToggle(node, node.Data.GroupingKeywords.ContainsValueOf(currentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Grouping Keyword Settings", node, true)){
						if(enabled) {
							node.Data.GroupingKeywords[currentEditingGroup] = node.Data.GroupingKeywords.DefaultValue;
						} else {
							node.Data.GroupingKeywords.Remove(currentEditingGroup);
						}
					}
				});

				using (disabledScope) {
					var newGroupingKeyword = EditorGUILayout.TextField("Grouping Keyword",node.Data.GroupingKeywords[currentEditingGroup]);
					EditorGUILayout.HelpBox(
						"Grouping Keyword requires \"*\" in itself. It assumes there is a pattern such as \"ID_0\" in incoming paths when configured as \"ID_*\" ", 
						MessageType.Info);

					if (newGroupingKeyword != node.Data.GroupingKeywords[currentEditingGroup]) {
						using(new RecordUndoScope("Change Grouping Keywords", node, true)){
							node.Data.GroupingKeywords[currentEditingGroup] = newGroupingKeyword;
						}
					}
				}
			}
		}

		private void DoInspectorPrefabricatorScriptGUI (NodeGUI node) {
			EditorGUILayout.HelpBox("Prefabricator: Create prefab with given assets and script.", MessageType.Info);
			UpdateNodeName(node);

			EditorGUILayout.LabelField("Script:", node.Data.ScriptClassName);
		}

		private void DoInspectorPrefabricatorGUI (NodeGUI node) {
			EditorGUILayout.HelpBox("Prefabricator: Create prefab with given assets and script.", MessageType.Info);
			UpdateNodeName(node);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {

				GUIStyle s = new GUIStyle("TextFieldDropDownText");

				/*
					check prefabricator script-type string.
				*/
				if (string.IsNullOrEmpty(node.Data.ScriptClassName)) {
					s.fontStyle = FontStyle.Bold;
					s.fontSize  = 12;
				} else {
					var loadedType = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(node.Data.ScriptClassName);

					if (loadedType == null) {
						s.fontStyle = FontStyle.Bold;
						s.fontSize  = 12;
					}
				}
				
				GUILayout.Label("ここをドロップダウンにする。");
				var newScriptClass = EditorGUILayout.TextField("Classname", node.Data.ScriptClassName, s);

				if (newScriptClass != node.Data.ScriptClassName) {
					using(new RecordUndoScope("Change Script Classname", node, true)){
						node.Data.ScriptClassName = newScriptClass;
					}
				}
			}
		}
		
		private void DoInspectorBundlizerGUI (NodeGUI node) {
			if (node.Data.BundleNameTemplate == null) return;

			EditorGUILayout.HelpBox("Bundlizer: Create asset bundle settings with given group of assets.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = DrawOverrideTargetToggle(node, node.Data.BundleNameTemplate.ContainsValueOf(currentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Bundle Name Template Setting", node, true)){
						if(enabled) {
							node.Data.BundleNameTemplate[currentEditingGroup] = node.Data.BundleNameTemplate.DefaultValue;
						} else {
							node.Data.BundleNameTemplate.Remove(currentEditingGroup);
						}
					}
				});

				using (disabledScope) {
					var bundleNameTemplate = EditorGUILayout.TextField("Bundle Name Template", node.Data.BundleNameTemplate[currentEditingGroup]).ToLower();

					if (bundleNameTemplate != node.Data.BundleNameTemplate[currentEditingGroup]) {
						using(new RecordUndoScope("Change Bundle Name Template", node, true)){
							node.Data.BundleNameTemplate[currentEditingGroup] = bundleNameTemplate;
						}
					}
				}
			}

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				GUILayout.Label("Variants:");
				var variantNames = node.Data.Variants.Select(v => v.Name).ToList();
				foreach (var v in node.Data.Variants) {

					using (new GUILayout.HorizontalScope()) {
						if (GUILayout.Button("-", GUILayout.Width(30))) {
							using(new RecordUndoScope("Remove Variant")){
								node.Data.RemoveVariant(v);
								node.UpdateNodeRect();
							}
						}
						else {
							GUIStyle s = new GUIStyle((GUIStyle)"TextFieldDropDownText");
							Action makeStyleBold = () => {
								s.fontStyle = FontStyle.Bold;
								s.fontSize = 12;
							};

							IntegratedGUIBundlizer.ValidateVariantName(v.Name, variantNames, 
								makeStyleBold,
								makeStyleBold,
								makeStyleBold);

							var variantName = EditorGUILayout.TextField(v.Name, s);

							if (variantName != v.Name) {
								using(new RecordUndoScope("Change Variant Name")){
									v.Name = variantName;
									node.UpdateNodeRect();
								}
							}
						}
					}

					if (GUILayout.Button("+")) {
						using(new RecordUndoScope("Add Variant", node, true)){
							node.Data.AddVariant(AssetBundleGraphSettings.BUNDLIZER_VARIANTNAME_DEFAULT);
							node.UpdateNodeRect();
						}
					}
				}
			}
		}

		private void DoInspectorBundleBuilderGUI (NodeGUI node) {
			if (node.Data.BundleBuilderBundleOptions == null) {
				return;
			}

			EditorGUILayout.HelpBox("BundleBuilder: Build asset bundles with given asset bundle settings.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = DrawOverrideTargetToggle(node, node.Data.BundleBuilderBundleOptions.ContainsValueOf(currentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Bundle Options", node, true)){
						if(enabled) {
							node.Data.BundleBuilderBundleOptions[currentEditingGroup] = node.Data.BundleBuilderBundleOptions.DefaultValue;
						}  else {
							node.Data.BundleBuilderBundleOptions.Remove(currentEditingGroup);
						}
					}
				} );

				using (disabledScope) {
					int bundleOptions = node.Data.BundleBuilderBundleOptions[currentEditingGroup];

					foreach (var option in AssetBundleGraphSettings.BundleOptionSettings) {

						// contains keyword == enabled. if not, disabled.
						bool isEnabled = (bundleOptions & (int)option.option) != 0;

						var result = EditorGUILayout.ToggleLeft(option.description, isEnabled);
						if (result != isEnabled) {
							using(new RecordUndoScope("Change Bundle Options", node, true)){
								bundleOptions = (result) ? 
									((int)option.option | bundleOptions) : 
									(((~(int)option.option)) & bundleOptions);
								node.Data.BundleBuilderBundleOptions[currentEditingGroup] = bundleOptions;
								/*
								 * Cannot use DisableWriteTypeTree and IgnoreTypeTreeChanges options together.
								 */
								if (result &&
									option.option == BuildAssetBundleOptions.DisableWriteTypeTree &&
									0 != (node.Data.BundleBuilderBundleOptions[currentEditingGroup] & (int)BuildAssetBundleOptions.DisableWriteTypeTree))
								{
									var currentValue = node.Data.BundleBuilderBundleOptions[currentEditingGroup];
									node.Data.BundleBuilderBundleOptions[currentEditingGroup] = (((~(int)BuildAssetBundleOptions.DisableWriteTypeTree)) & currentValue);
								}

								if (result &&
									option.option == BuildAssetBundleOptions.IgnoreTypeTreeChanges &&
									0 != (node.Data.BundleBuilderBundleOptions[currentEditingGroup] & (int)BuildAssetBundleOptions.IgnoreTypeTreeChanges))
								{
									var currentValue = node.Data.BundleBuilderBundleOptions[currentEditingGroup];
									node.Data.BundleBuilderBundleOptions[currentEditingGroup] = (((~(int)BuildAssetBundleOptions.IgnoreTypeTreeChanges)) & currentValue);
								}
							}
							return;
						}
					}
				}
			}
		}


		private void DoInspectorExporterGUI (NodeGUI node) {
			if (node.Data.ExporterExportPath == null) {
				return;
			}

			EditorGUILayout.HelpBox("Exporter: Export given files to output directory.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = DrawOverrideTargetToggle(node, node.Data.ExporterExportPath.ContainsValueOf(currentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Export Settings", node, true)){
						if(enabled) {
							node.Data.ExporterExportPath[currentEditingGroup] = node.Data.ExporterExportPath.DefaultValue;
						}  else {
							node.Data.ExporterExportPath.Remove(currentEditingGroup);
						}
					}
				} );

				using (disabledScope) {
					EditorGUILayout.LabelField("Export Path:");
					var newExportPath = EditorGUILayout.TextField(
						SystemDataUtility.GetProjectName(), 
						node.Data.ExporterExportPath[currentEditingGroup]
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
									using(new SaveScope(node)) {
										Directory.CreateDirectory(exporterrNodePath);
									}
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
						
					if (newExportPath != node.Data.ExporterExportPath[currentEditingGroup]) {
						using(new RecordUndoScope("Change Export Path", node, true)){
							node.Data.ExporterExportPath[currentEditingGroup] = newExportPath;
						}
					}
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

			switch (node.Kind) {
			case NodeKind.LOADER_GUI:
				DoInspectorLoaderGUI(node);
				break;
			case NodeKind.FILTER_SCRIPT:
				DoInspectorFilterScriptGUI(node);
				break;
			case NodeKind.FILTER_GUI:
				DoInspectorFilterGUI(node);
				break;
			case NodeKind.IMPORTSETTING_GUI :
				DoInspectorImportSettingGUI(node);
				break;
			case NodeKind.MODIFIER_GUI :
				DoInspectorModifierGUI(node);
				break;
			case NodeKind.GROUPING_GUI:
				DoInspectorGroupingGUI(node);
				break;
			case NodeKind.PREFABRICATOR_SCRIPT:
				DoInspectorPrefabricatorScriptGUI(node);
				break;
			case NodeKind.PREFABRICATOR_GUI:
				DoInspectorPrefabricatorGUI(node);
				break;
			case NodeKind.BUNDLIZER_GUI:
				DoInspectorBundlizerGUI(node);
				break;
			case NodeKind.BUNDLEBUILDER_GUI:
				DoInspectorBundleBuilderGUI(node);
				break;
			case NodeKind.EXPORTER_GUI: 
				DoInspectorExporterGUI(node);
				break;
			default: 
				Debug.LogError(node.Name + " is defined as unknown kind of node. value:" + node.Kind);
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
			var newName = EditorGUILayout.TextField("Node Name", node.Name);

			if( NodeGUIUtility.allNodeNames != null ) {
				var overlapping = NodeGUIUtility.allNodeNames.GroupBy(x => x)
					.Where(group => group.Count() > 1)
					.Select(group => group.Key);
				if (overlapping.Any() && overlapping.Contains(newName)) {
					EditorGUILayout.HelpBox("This node name already exist. Please put other name:" + newName, MessageType.Error);
					AssetBundleGraphEditorWindow.AddNodeException(new NodeException("Node name " + newName + " already exist.", node.Id ));
				}
			}

			if (newName != node.Name) {
				using(new RecordUndoScope("Change Node Name", node, true)){
					node.Name = newName;
					node.UpdateNodeRect();
				}
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