using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomNode("Create Assets/Create Prefab From Group", 50)]
	public class PrefabBuilder : Node, Model.NodeDataImporter {

        public enum OutputOption : int {
            CreateInCacheDirectory,
            CreateInSelectedDirectory
        }

		[SerializeField] private SerializableMultiTargetInstance m_instance;
		[SerializeField] private UnityEditor.ReplacePrefabOptions m_replacePrefabOptions = UnityEditor.ReplacePrefabOptions.Default;
        [SerializeField] private SerializableMultiTargetString m_outputDir;
        [SerializeField] private SerializableMultiTargetInt m_outputOption;

        public static readonly string kCacheDirName = "Prefabs";

		public UnityEditor.ReplacePrefabOptions Options {
			get {
				return m_replacePrefabOptions;
			}
		}

		public SerializableMultiTargetInstance Builder {
			get {
				return m_instance;
			}
		}

		public override string ActiveStyle {
			get {
				return "node 4 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 4";
			}
		}

		public override string Category {
			get {
				return "Create";
			}
		}

		public override void Initialize(Model.NodeData data) {
			m_instance = new SerializableMultiTargetInstance();
            m_outputDir = new SerializableMultiTargetString();
            m_outputOption = new SerializableMultiTargetInt((int)OutputOption.CreateInCacheDirectory);

			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public void Import(V1.NodeData v1, Model.NodeData v2) {
			m_instance = new SerializableMultiTargetInstance(v1.ScriptClassName, v1.InstanceData);
			m_replacePrefabOptions = v1.ReplacePrefabOptions;
            m_outputDir = new SerializableMultiTargetString();
            m_outputOption = new SerializableMultiTargetInt((int)OutputOption.CreateInCacheDirectory);
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new PrefabBuilder();
			newNode.m_instance = new SerializableMultiTargetInstance(m_instance);
			newNode.m_replacePrefabOptions = m_replacePrefabOptions;
            newNode.m_outputDir = new SerializableMultiTargetString(m_outputDir);
            newNode.m_outputOption = new SerializableMultiTargetInt(m_outputOption);

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();
			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			EditorGUILayout.HelpBox("Create Prefab From Group: Create prefab from incoming group of assets, using assigned script.", MessageType.Info);
			editor.UpdateNodeName(node);

			var builder = m_instance.Get<IPrefabBuilder>(editor.CurrentEditingGroup);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {

				var map = PrefabBuilderUtility.GetAttributeAssemblyQualifiedNameMap();
				if(map.Count > 0) {
					using(new GUILayout.HorizontalScope()) {
						GUILayout.Label("PrefabBuilder");
						var guiName = PrefabBuilderUtility.GetPrefabBuilderGUIName(m_instance.ClassName);

						if (GUILayout.Button(guiName, "Popup", GUILayout.MinWidth(150f))) {
							var builders = map.Keys.ToList();

							if(builders.Count > 0) {
								NodeGUI.ShowTypeNamesMenu(guiName, builders, (string selectedGUIName) => 
									{
										using(new RecordUndoScope("Change PrefabBuilder class", node, true)) {
											builder = PrefabBuilderUtility.CreatePrefabBuilder(selectedGUIName);
											m_instance.Set(editor.CurrentEditingGroup, builder);
											onValueChanged();
										}
									} 
								);
							}
						}

                        MonoScript s = TypeUtility.LoadMonoScript(m_instance.ClassName);

						using(new EditorGUI.DisabledScope(s == null)) {
							if(GUILayout.Button("Edit", GUILayout.Width(50))) {
								AssetDatabase.OpenAsset(s, 0);
							}
						}
					}
					ReplacePrefabOptions opt = (ReplacePrefabOptions)EditorGUILayout.EnumPopup("Prefab Replace Option", m_replacePrefabOptions, GUILayout.MinWidth(150f));
					if(m_replacePrefabOptions != opt) {
						using(new RecordUndoScope("Change Prefab Replace Option", node, true)) {
							m_replacePrefabOptions = opt;
							onValueChanged();
						}
                        opt = m_replacePrefabOptions;
					}
				} else {
					if(!string.IsNullOrEmpty(m_instance.ClassName)) {
						EditorGUILayout.HelpBox(
							string.Format(
								"Your PrefabBuilder script {0} is missing from assembly. Did you delete script?", m_instance.ClassName), MessageType.Info);
					} else {
						string[] menuNames = Model.Settings.GUI_TEXT_MENU_GENERATE_PREFABBUILDER.Split('/');
						EditorGUILayout.HelpBox(
							string.Format(
								"You need to create at least one PrefabBuilder script to use this node. To start, select {0}>{1}>{2} menu and create new script from template.",
								menuNames[1],menuNames[2], menuNames[3]
							), MessageType.Info);
					}
				}

				GUILayout.Space(10f);

				editor.DrawPlatformSelector(node);
				using (new EditorGUILayout.VerticalScope()) {
					var disabledScope = editor.DrawOverrideTargetToggle(node, m_instance.ContainsValueOf(editor.CurrentEditingGroup), (bool enabled) => {
						if(enabled) {
							m_instance.CopyDefaultValueTo(editor.CurrentEditingGroup);
                            m_outputDir[editor.CurrentEditingGroup] = m_outputDir.DefaultValue;
                            m_outputOption[editor.CurrentEditingGroup] = m_outputOption.DefaultValue;
						} else {
							m_instance.Remove(editor.CurrentEditingGroup);
                            m_outputDir.Remove(editor.CurrentEditingGroup);
                            m_outputOption.Remove(editor.CurrentEditingGroup);
						}
						onValueChanged();
					});

					using (disabledScope) {
                        OutputOption opt = (OutputOption)m_outputOption[editor.CurrentEditingGroup];
                        var newOption = (OutputOption)EditorGUILayout.EnumPopup("Output Option", opt);
                        if(newOption != opt) {
                            using(new RecordUndoScope("Change Output Option", node, true)){
                                m_outputOption[editor.CurrentEditingGroup] = (int)newOption;
                                onValueChanged();
                            }
                            opt = newOption;
                        }

                        using (new EditorGUI.DisabledScope (opt == OutputOption.CreateInCacheDirectory)) {
                            var newDirPath = editor.DrawFolderSelector ("Output Directory", "Select Output Folder", 
                                m_outputDir[editor.CurrentEditingGroup],
                                Application.dataPath,
                                (string folderSelected) => {
                                    string basePath = Application.dataPath;

                                    if(basePath == folderSelected) {
                                        folderSelected = string.Empty;
                                    } else {
                                        var index = folderSelected.IndexOf(basePath);
                                        if(index >= 0 ) {
                                            folderSelected = folderSelected.Substring(basePath.Length + index);
                                            if(folderSelected.IndexOf('/') == 0) {
                                                folderSelected = folderSelected.Substring(1);
                                            }
                                        }
                                    }
                                    return folderSelected;
                                }
                            );
                            if (newDirPath != m_outputDir[editor.CurrentEditingGroup]) {
                                using(new RecordUndoScope("Change Output Directory", node, true)){
                                    m_outputDir[editor.CurrentEditingGroup] = newDirPath;
                                    onValueChanged();
                                }
                            }

                            var dirPath = Path.Combine (Application.dataPath, m_outputDir [editor.CurrentEditingGroup]);

                            if (opt == OutputOption.CreateInSelectedDirectory && 
                                !string.IsNullOrEmpty(m_outputDir [editor.CurrentEditingGroup]) &&
                                !Directory.Exists (dirPath)) 
                            {
                                using (new EditorGUILayout.HorizontalScope()) {
                                    EditorGUILayout.LabelField(m_outputDir[editor.CurrentEditingGroup] + " does not exist.");
                                    if(GUILayout.Button("Create directory")) {
                                        Directory.CreateDirectory(dirPath);
                                        AssetDatabase.Refresh ();
                                    }
                                }
                                EditorGUILayout.Space();

                                string parentDir = Path.GetDirectoryName(m_outputDir[editor.CurrentEditingGroup]);
                                if(Directory.Exists(parentDir)) {
                                    EditorGUILayout.LabelField("Available Directories:");
                                    string[] dirs = Directory.GetDirectories(parentDir);
                                    foreach(string s in dirs) {
                                        EditorGUILayout.LabelField(s);
                                    }
                                }
                                EditorGUILayout.Space();
                            }

                            var outputDir = PrepareOutputDirectory (BuildTargetUtility.GroupToTarget(editor.CurrentEditingGroup), node.Data);

                            using (new EditorGUI.DisabledScope (!Directory.Exists (outputDir))) 
                            {
                                using (new EditorGUILayout.HorizontalScope ()) {
                                    GUILayout.FlexibleSpace ();
                                    if (GUILayout.Button ("Highlight in Project Window", GUILayout.Width (180f))) {
                                        var folder = AssetDatabase.LoadMainAssetAtPath (outputDir);
                                        EditorGUIUtility.PingObject (folder);
                                    }
                                }
                            }
                        }

                        GUILayout.Space (8f);

						if (builder != null) {
							Action onChangedAction = () => {
								using(new RecordUndoScope("Change PrefabBuilder Setting", node)) {
									m_instance.Set(editor.CurrentEditingGroup, builder);
									onValueChanged();
								}
							};

							builder.OnInspectorGUI(onChangedAction);
						}
					}
				}
			}
		}

		public override void OnContextMenuGUI(GenericMenu menu) {
			MonoScript s = TypeUtility.LoadMonoScript(m_instance.ClassName);
			if(s != null) {
				menu.AddItem(
					new GUIContent("Edit Script"),
					false, 
					() => {
						AssetDatabase.OpenAsset(s, 0);
					}
				);
			}
		}

		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			ValidatePrefabBuilder(node, target, incoming,
                () => {
                    throw new NodeException ("Output directory not found.", "Create output directory or set a valid directory path.", node);
                },
				() => {
                    throw new NodeException("PrefabBuilder is not configured.", "Configure PrefabBuilder from inspector.", node);
				},
				() => {
                    throw new NodeException("Failed to create PrefabBuilder from settings.", "Fix settings from inspector.", node);
				},
				(string groupKey) => {
					throw new NodeException(string.Format("Can not create prefab with incoming assets for group {0}.", groupKey), "Fix group input assets for selected PrefabBuilder.",node);
				},
				(AssetReference badAsset) => {
					throw new NodeException(string.Format("Can not import incoming asset {0}.", badAsset.fileNameAndExtension), "", node);
				}
			);

			if(incoming == null) {
				return;
			}

            var prefabOutputDir = PrepareOutputDirectory (target, node);

			var builder = m_instance.Get<IPrefabBuilder>(target);
			UnityEngine.Assertions.Assert.IsNotNull(builder);

			Dictionary<string, List<AssetReference>> output = null;
			if(Output != null) {
				output = new Dictionary<string, List<AssetReference>>();
			}

			var aggregatedGroups = new Dictionary<string, List<AssetReference>>();
			foreach(var ag in incoming) {
				foreach(var key in ag.assetGroups.Keys) {
					if(!aggregatedGroups.ContainsKey(key)){
						aggregatedGroups[key] = new List<AssetReference>();
					}
					aggregatedGroups[key].AddRange(ag.assetGroups[key].AsEnumerable());
				}
			}

			foreach(var key in aggregatedGroups.Keys) {

				var assets = aggregatedGroups[key];
                var threshold = PrefabBuilderUtility.GetPrefabBuilderAssetThreshold(m_instance.ClassName);
				if( threshold < assets.Count ) {
					var guiName = PrefabBuilderUtility.GetPrefabBuilderGUIName(m_instance.ClassName);
					throw new NodeException(
                        string.Format("Too many assets passed to {0} for group:{1}. {2}'s threshold is set to {4}", guiName, key, guiName,threshold),
                        string.Format("Limit number of assets in a group to {4}", threshold), node);
				}

                GameObject previousPrefab = null; //TODO

				List<UnityEngine.Object> allAssets = LoadAllAssets(assets);
                var prefabFileName = builder.CanCreatePrefab(key, allAssets, previousPrefab);
				if(output != null && prefabFileName != null) {
					output[key] = new List<AssetReference> () {
						AssetReferenceDatabase.GetPrefabReference(FileUtility.PathCombine(prefabOutputDir, prefabFileName + ".prefab"))
					};
				}
				UnloadAllAssets(assets);
			}

			if(Output != null) {
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();
				Output(dst, output);
			}
		}

		private static List<UnityEngine.Object> LoadAllAssets(List<AssetReference> assets) {
			List<UnityEngine.Object> objects = new List<UnityEngine.Object>();

			foreach(var a in assets) {
				objects.AddRange(a.allData.AsEnumerable());
			}
			return objects;
		}

		private static void UnloadAllAssets(List<AssetReference> assets) {
			assets.ForEach(a => a.ReleaseData());
		}

		public override void Build (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output,
			Action<Model.NodeData, string, float> progressFunc) 
		{
			if(incoming == null) {
				return;
			}

			var builder = m_instance.Get<IPrefabBuilder>(target);
			UnityEngine.Assertions.Assert.IsNotNull(builder);

            var prefabOutputDir = PrepareOutputDirectory(target, node);
			Dictionary<string, List<AssetReference>> output = null;
			if(Output != null) {
				output = new Dictionary<string, List<AssetReference>>();
			}

			var aggregatedGroups = new Dictionary<string, List<AssetReference>>();
			foreach(var ag in incoming) {
				foreach(var key in ag.assetGroups.Keys) {
					if(!aggregatedGroups.ContainsKey(key)){
						aggregatedGroups[key] = new List<AssetReference>();
					}
					aggregatedGroups[key].AddRange(ag.assetGroups[key].AsEnumerable());
				}
			}

            var anyPrefabCreated = false;

			foreach(var key in aggregatedGroups.Keys) {

				var assets = aggregatedGroups[key];

				var allAssets = LoadAllAssets(assets);
                GameObject previousPrefab = null; //TODO

                var prefabFileName = builder.CanCreatePrefab(key, allAssets, previousPrefab);
				var prefabSavePath = FileUtility.PathCombine(prefabOutputDir, prefabFileName + ".prefab");

				if (!Directory.Exists(Path.GetDirectoryName(prefabSavePath))) {
					Directory.CreateDirectory(Path.GetDirectoryName(prefabSavePath));
				}

                if(!File.Exists(prefabSavePath) || PrefabBuildInfo.DoesPrefabNeedRebuilding(prefabOutputDir, this, node, target, key, assets)) {
                    UnityEngine.GameObject obj = builder.CreatePrefab(key, allAssets, previousPrefab);
					if(obj == null) {
						throw new AssetGraphException(string.Format("{0} :PrefabBuilder {1} returned null in CreatePrefab() [groupKey:{2}]", 
							node.Name, builder.GetType().FullName, key));
					}

					LogUtility.Logger.LogFormat(LogType.Log, "{0} is (re)creating Prefab:{1} with {2}({3})", node.Name, prefabFileName,
						PrefabBuilderUtility.GetPrefabBuilderGUIName(m_instance.ClassName),
						PrefabBuilderUtility.GetPrefabBuilderVersion(m_instance.ClassName));

					if(progressFunc != null) progressFunc(node, string.Format("Creating {0}", prefabFileName), 0.5f);

                    PrefabUtility.CreatePrefab(prefabSavePath, obj, m_replacePrefabOptions);
                    PrefabBuildInfo.SavePrefabBuildInfo(prefabOutputDir, this, node, target, key, assets);
					GameObject.DestroyImmediate(obj);
                    anyPrefabCreated = true;
                    AssetProcessEventRecord.GetRecord ().LogModify (AssetDatabase.AssetPathToGUID(prefabSavePath));
				}
				UnloadAllAssets(assets);

                if (anyPrefabCreated) {
                    AssetDatabase.SaveAssets ();
                }

				if(output != null) {
					output[key] = new List<AssetReference> () {
						AssetReferenceDatabase.GetPrefabReference(prefabSavePath)
					};
				}
			}

			if(Output != null) {
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();
				Output(dst, output);
			}
		}

		private void ValidatePrefabBuilder (
			Model.NodeData node,
			BuildTarget target,
			IEnumerable<PerformGraph.AssetGroups> incoming, 
            Action folderDoesntExist,
			Action noBuilderData,
			Action failedToCreateBuilder,
			Action<string> canNotCreatePrefab,
			Action<AssetReference> canNotImportAsset
		) {
            var outputDir = PrepareOutputDirectory (target, node);

            if (!Directory.Exists (outputDir)) {
                folderDoesntExist ();
            }

			var builder = m_instance.Get<IPrefabBuilder>(target);

			if(null == builder ) {
				failedToCreateBuilder();
			}

            builder.OnValidate ();

			if(null != builder && null != incoming) {
				foreach(var ag in incoming) {
					foreach(var key in ag.assetGroups.Keys) {
						var assets = ag.assetGroups[key];
						if(assets.Any()) {
							bool isAllGoodAssets = true;
							foreach(var a in assets) {
								if(string.IsNullOrEmpty(a.importFrom)) {
									canNotImportAsset(a);
									isAllGoodAssets = false;
								}
							}
							if(isAllGoodAssets) {
                                GameObject previousPrefab = null; //TODO

								// do not call LoadAllAssets() unless all assets have importFrom
								var al = ag.assetGroups[key];
								List<UnityEngine.Object> allAssets = LoadAllAssets(al);
                                if(string.IsNullOrEmpty(builder.CanCreatePrefab(key, allAssets, previousPrefab))) {
									canNotCreatePrefab(key);
								}
								UnloadAllAssets(al);
							}
						}
					}
				}
			}
		}	

        private string PrepareOutputDirectory(BuildTarget target, Model.NodeData node) {

            var outputOption = (OutputOption)m_outputOption [target];

            if(outputOption == OutputOption.CreateInCacheDirectory) {
                return FileUtility.EnsureCacheDirExists (target, node, kCacheDirName);
            }

            return Path.Combine("Assets", m_outputDir [target]);
        }
	}
}