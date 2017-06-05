using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	[CustomNode("Build/Build Asset Bundles", 90)]
	public class BundleBuilder : Node, Model.NodeDataImporter {

		struct AssetImporterSetting {
			private AssetImporter importer;
			private string assetBundleName;
			private string assetBundleVariant;

			public AssetImporterSetting(AssetImporter imp) {
				importer = imp;
				assetBundleName = importer.assetBundleName;
				assetBundleVariant = importer.assetBundleVariant;
			}

			public void WriteBack() {
				importer.SetAssetBundleNameAndVariant (assetBundleName, assetBundleVariant);
				importer.SaveAndReimport ();
			}
		}

        public enum OutputOption : int {
            BuildInCacheDirectory,
            ErrorIfNoOutputDirectoryFound,
            AutomaticallyCreateIfNoOutputDirectoryFound,
            DeleteAndRecreateOutputDirectory
        }

		private static readonly string key = "0";

        [SerializeField] private SerializableMultiTargetInt m_enabledBundleOptions;
        [SerializeField] private SerializableMultiTargetString m_outputDir;
        [SerializeField] private SerializableMultiTargetInt m_outputOption;
		[SerializeField] private bool m_overwriteImporterSetting;

		public override string ActiveStyle {
			get {
				return "node 5 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 5";
			}
		}

		public override string Category {
			get {
				return "Build";
			}
		}

		public override Model.NodeOutputSemantics NodeInputType {
			get {
				return Model.NodeOutputSemantics.AssetBundleConfigurations;
			}
		}

		public override Model.NodeOutputSemantics NodeOutputType {
			get {
				return Model.NodeOutputSemantics.AssetBundles;
			}
		}

		public override void Initialize(Model.NodeData data) {
            m_enabledBundleOptions = new SerializableMultiTargetInt();
            m_outputDir = new SerializableMultiTargetString();
            m_outputOption = new SerializableMultiTargetInt((int)OutputOption.BuildInCacheDirectory);

			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public void Import(V1.NodeData v1, Model.NodeData v2) {
			m_enabledBundleOptions = new SerializableMultiTargetInt(v1.BundleBuilderBundleOptions);
            m_outputDir = new SerializableMultiTargetString();
            m_outputOption = new SerializableMultiTargetInt((int)OutputOption.BuildInCacheDirectory);
		}
			
		public override Node Clone(Model.NodeData newData) {
			var newNode = new BundleBuilder();
			newNode.m_enabledBundleOptions = new SerializableMultiTargetInt(m_enabledBundleOptions);
            newNode.m_outputDir = new SerializableMultiTargetString(m_outputDir);
            newNode.m_outputOption = new SerializableMultiTargetInt(m_outputOption);

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();

			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			if (m_enabledBundleOptions == null) {
				return;
			}

			EditorGUILayout.HelpBox("Build Asset Bundles: Build asset bundles with given asset bundle settings.", MessageType.Info);
			editor.UpdateNodeName(node);

			bool newOverwrite = EditorGUILayout.ToggleLeft ("Keep AssetImporter settings for variants", m_overwriteImporterSetting);
			if (newOverwrite != m_overwriteImporterSetting) {
				using(new RecordUndoScope("Remove Target Bundle Options", node, true)){
					m_overwriteImporterSetting = newOverwrite;
					onValueChanged();
				}
			}

			GUILayout.Space(10f);

			//Show target configuration tab
			editor.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = editor.DrawOverrideTargetToggle(node, m_enabledBundleOptions.ContainsValueOf(editor.CurrentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Bundle Options", node, true)){
						if(enabled) {
                            m_enabledBundleOptions[editor.CurrentEditingGroup] = m_enabledBundleOptions.DefaultValue;
                            m_outputDir[editor.CurrentEditingGroup] = m_outputDir.DefaultValue;
                            m_outputOption[editor.CurrentEditingGroup] = m_outputOption.DefaultValue;
						}  else {
                            m_enabledBundleOptions.Remove(editor.CurrentEditingGroup);
                            m_outputDir.Remove(editor.CurrentEditingGroup);
                            m_outputOption.Remove(editor.CurrentEditingGroup);
						}
						onValueChanged();
					}
				} );

				using (disabledScope) {
                    OutputOption opt = (OutputOption)m_outputOption[editor.CurrentEditingGroup];
                    var newOption = (OutputOption)EditorGUILayout.EnumPopup("Output Option", opt);
                    if(newOption != opt) {
                        using(new RecordUndoScope("Change Output Option", node, true)){
                            m_outputOption[editor.CurrentEditingGroup] = (int)newOption;
                            onValueChanged();
                        }
                    }

                    using (new EditorGUI.DisabledScope (opt == OutputOption.BuildInCacheDirectory)) {
                        var newDirPath = editor.DrawFolderSelector ("Output Directory", "Select Output Folder", 
                            m_outputDir[editor.CurrentEditingGroup],
                            Application.dataPath + "/../",
                            (string folderSelected) => {
                                var projectPath = Directory.GetParent(Application.dataPath).ToString();

                                if(projectPath == folderSelected) {
                                    folderSelected = string.Empty;
                                } else {
                                    var index = folderSelected.IndexOf(projectPath);
                                    if(index >= 0 ) {
                                        folderSelected = folderSelected.Substring(projectPath.Length + index);
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

                        if (opt == OutputOption.ErrorIfNoOutputDirectoryFound && 
                            !string.IsNullOrEmpty(m_outputDir [editor.CurrentEditingGroup]) &&
                            !Directory.Exists (m_outputDir [editor.CurrentEditingGroup])) 
                        {
                            using (new EditorGUILayout.HorizontalScope()) {
                                EditorGUILayout.LabelField(m_outputDir[editor.CurrentEditingGroup] + " does not exist.");
                                if(GUILayout.Button("Create directory")) {
                                    Directory.CreateDirectory(m_outputDir[editor.CurrentEditingGroup]);
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

                        var outputDir = PrepareOutputDirectory (BuildTargetUtility.GroupToTarget(editor.CurrentEditingGroup), node.Data, false, false);

                        using (new EditorGUI.DisabledScope (!Directory.Exists (outputDir))) 
                        {
                            using (new EditorGUILayout.HorizontalScope ()) {
                                GUILayout.FlexibleSpace ();
                                #if UNITY_EDITOR_OSX
                                string buttonName = "Reveal in Finder";
                                #else
                                string buttonName = "Show in Explorer";
                                #endif
                                if (GUILayout.Button (buttonName)) {
                                    EditorUtility.RevealInFinder (outputDir);
                                }
                            }
                        }
                    }

					int bundleOptions = m_enabledBundleOptions[editor.CurrentEditingGroup];

					bool isDisableWriteTypeTreeEnabled  = 0 < (bundleOptions & (int)BuildAssetBundleOptions.DisableWriteTypeTree);
					bool isIgnoreTypeTreeChangesEnabled = 0 < (bundleOptions & (int)BuildAssetBundleOptions.IgnoreTypeTreeChanges);

					// buildOptions are validated during loading. Two flags should not be true at the same time.
					UnityEngine.Assertions.Assert.IsFalse(isDisableWriteTypeTreeEnabled && isIgnoreTypeTreeChangesEnabled);

					bool isSomethingDisabled = isDisableWriteTypeTreeEnabled || isIgnoreTypeTreeChangesEnabled;

					foreach (var option in Model.Settings.BundleOptionSettings) {

						// contains keyword == enabled. if not, disabled.
						bool isEnabled = (bundleOptions & (int)option.option) != 0;

						bool isToggleDisabled = 
							(option.option == BuildAssetBundleOptions.DisableWriteTypeTree  && isIgnoreTypeTreeChangesEnabled) ||
							(option.option == BuildAssetBundleOptions.IgnoreTypeTreeChanges && isDisableWriteTypeTreeEnabled);

						using(new EditorGUI.DisabledScope(isToggleDisabled)) {
							var result = EditorGUILayout.ToggleLeft(option.description, isEnabled);
							if (result != isEnabled) {
								using(new RecordUndoScope("Change Bundle Options", node, true)){
									bundleOptions = (result) ? 
										((int)option.option | bundleOptions) : 
										(((~(int)option.option)) & bundleOptions);
									m_enabledBundleOptions[editor.CurrentEditingGroup] = bundleOptions;
									onValueChanged();
								}
							}
						}
					}
					if(isSomethingDisabled) {
						EditorGUILayout.HelpBox("'Disable Write Type Tree' and 'Ignore Type Tree Changes' can not be used together.", MessageType.Info);
					}
				}
			}
		}

		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			// BundleBuilder do nothing without incoming connections
			if(incoming == null) {
				return;
			}

            var bundleOutputDir = PrepareOutputDirectory (target, node, false, true);

			var bundleNames = incoming.SelectMany(v => v.assetGroups.Keys).Distinct().ToList();
			var bundleVariants = new Dictionary<string, List<string>>();

			// get all variant name for bundles
			foreach(var ag in incoming) {
				foreach(var name in ag.assetGroups.Keys) {
					if(!bundleVariants.ContainsKey(name)) {
						bundleVariants[name] = new List<string>();
					}
					var assets = ag.assetGroups[name];
					foreach(var a in assets) {
						var variantName = a.variantName;
						if(!bundleVariants[name].Contains(variantName)) {
							bundleVariants[name].Add(variantName);
						}
					}
				}
			}

			// add manifest file
            var manifestName = GetManifestName(target);
			bundleNames.Add( manifestName );
			bundleVariants[manifestName] = new List<string>() {""};

			if(connectionsToOutput != null && Output != null) {
				UnityEngine.Assertions.Assert.IsTrue(connectionsToOutput.Any());

				var outputDict = new Dictionary<string, List<AssetReference>>();
				outputDict[key] = new List<AssetReference>();

				foreach (var name in bundleNames) {
					foreach(var v in bundleVariants[name]) {
						string bundleName = (string.IsNullOrEmpty(v))? name : name + "." + v;
						AssetReference bundle = AssetReferenceDatabase.GetAssetBundleReference( FileUtility.PathCombine(bundleOutputDir, bundleName) );
						AssetReference manifest = AssetReferenceDatabase.GetAssetBundleReference( FileUtility.PathCombine(bundleOutputDir, bundleName + Model.Settings.MANIFEST_FOOTER) );
						outputDict[key].Add(bundle);
						outputDict[key].Add(manifest);
					}
				}

				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();
				Output(dst, outputDict);
			}
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

			var aggregatedGroups = new Dictionary<string, List<AssetReference>>();
			aggregatedGroups[key] = new List<AssetReference>();

			if(progressFunc != null) progressFunc(node, "Collecting all inputs...", 0f);

			foreach(var ag in incoming) {
				foreach(var name in ag.assetGroups.Keys) {
					if(!aggregatedGroups.ContainsKey(name)) {
						aggregatedGroups[name] = new List<AssetReference>();
					}
					aggregatedGroups[name].AddRange(ag.assetGroups[name].AsEnumerable());
				}
			}

            var bundleOutputDir = PrepareOutputDirectory (target, node, true, true);
			var bundleNames = aggregatedGroups.Keys.ToList();
			var bundleVariants = new Dictionary<string, List<string>>();

			if(progressFunc != null) progressFunc(node, "Building bundle variants map...", 0.2f);

			// get all variant name for bundles
			foreach(var name in aggregatedGroups.Keys) {
				if(!bundleVariants.ContainsKey(name)) {
					bundleVariants[name] = new List<string>();
				}
				var assets = aggregatedGroups[name];
				foreach(var a in assets) {
					var variantName = a.variantName;
					if(!bundleVariants[name].Contains(variantName)) {
						bundleVariants[name].Add(variantName);
					}
				}
			}

			int validNames = 0;
			foreach (var name in bundleNames) {
				var assets = aggregatedGroups[name];
				// we do not build bundle without any asset
				if( assets.Count > 0 ) {
					validNames += bundleVariants[name].Count;
				}
			}

			AssetBundleBuild[] bundleBuild = new AssetBundleBuild[validNames];
			List<AssetImporterSetting> importerSetting = null;

			if (!m_overwriteImporterSetting) {
				importerSetting = new List<AssetImporterSetting> ();
			}

			int bbIndex = 0;
			foreach(var name in bundleNames) {
				foreach(var v in bundleVariants[name]) {
					var assets = aggregatedGroups[name];

					if(assets.Count <= 0) {
						continue;
					}

					bundleBuild[bbIndex].assetBundleName = name;
					bundleBuild[bbIndex].assetBundleVariant = v;
					bundleBuild[bbIndex].assetNames = assets.Where(x => x.variantName == v).Select(x => x.importFrom).ToArray();

					/**
					 * WORKAROND: This will be unnecessary in future version
					 * Unity currently have issue in configuring variant assets using AssetBundleBuild[] that
					 * internal identifier does not match properly unless you configure value in AssetImporter.
					 */
					if (!string.IsNullOrEmpty (v)) {
						foreach (var path in bundleBuild[bbIndex].assetNames) {
							AssetImporter importer = AssetImporter.GetAtPath (path);

							if (importer.assetBundleName != name || importer.assetBundleVariant != v) {
								if (!m_overwriteImporterSetting) {
									importerSetting.Add (new AssetImporterSetting(importer));
								}
								importer.SetAssetBundleNameAndVariant (name, v);
								importer.SaveAndReimport ();
							}
						}
					}

					++bbIndex;
				}
			}

			if(progressFunc != null) progressFunc(node, "Building Asset Bundles...", 0.7f);

			AssetBundleManifest m = BuildPipeline.BuildAssetBundles(bundleOutputDir, bundleBuild, (BuildAssetBundleOptions)m_enabledBundleOptions[target], target);

			var output = new Dictionary<string, List<AssetReference>>();
			output[key] = new List<AssetReference>();

			var generatedFiles = FileUtility.GetAllFilePathsInFolder(bundleOutputDir);
            var manifestName = GetManifestName (target);
			// add manifest file
            bundleVariants.Add( manifestName.ToLower(), new List<string> { null } );
			foreach (var path in generatedFiles) {
				var fileName = path.Substring(bundleOutputDir.Length+1);
				if( IsFileIntendedItem(fileName, bundleVariants) ) {
                    if (fileName == manifestName) {
                        output[key].Add( AssetReferenceDatabase.GetAssetBundleManifestReference(path) );
                    } else {
                        output[key].Add( AssetReferenceDatabase.GetAssetBundleReference(path) );
                    }
				}
			}

			if(Output != null) {
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();
				Output(dst, output);
			}

			if (importerSetting != null) {
				importerSetting.ForEach (i => i.WriteBack ());
			}

            AssetBundleBuildReport.AddBuildReport(new AssetBundleBuildReport(node, m, manifestName, bundleBuild, output[key], aggregatedGroups, bundleVariants));
		}

        private string GetManifestName(BuildTarget target) {
            if (string.IsNullOrEmpty (m_outputDir [target])) {
                return BuildTargetUtility.TargetToAssetBundlePlatformName (target);
            } else {
                return Path.GetFileName (m_outputDir [target]);
            }
        }

        private string PrepareOutputDirectory(BuildTarget target, Model.NodeData node, bool autoCreate, bool throwException) {

            var outputOption = (OutputOption)m_outputOption [target];
            var outputDir = m_outputDir [target];

            if(outputOption == OutputOption.BuildInCacheDirectory) {
                return FileUtility.EnsureAssetBundleCacheDirExists (target, node);
            }

            if (throwException) {
                if(string.IsNullOrEmpty(outputDir)) {
                    throw new NodeException (node.Name + ":Output directory is empty.", node.Id);
                }

                if(outputOption == OutputOption.ErrorIfNoOutputDirectoryFound) {
                    if (!Directory.Exists (outputDir)) {
                        throw new NodeException (node.Name + ":Output directory not found.", node.Id);
                    }
                }
            }

            if (autoCreate) {
                if(outputOption == OutputOption.DeleteAndRecreateOutputDirectory) {
                    if (Directory.Exists(outputDir)) {
                        Directory.Delete(outputDir, true);
                    }
                }

                if (!Directory.Exists(outputDir)) {
                    Directory.CreateDirectory(outputDir);
                }
            }

            return outputDir;
        }

		// Check if given file is generated Item
		private bool IsFileIntendedItem(string filename, Dictionary<string, List<string>> bundleVariants) {
			filename = filename.ToLower();

			int lastDotManifestIndex = filename.LastIndexOf(".manifest");
			filename = (lastDotManifestIndex > 0)? filename.Substring(0, lastDotManifestIndex) : filename;

			// test if given file is not configured as variant
			if(bundleVariants.ContainsKey(filename)) {
				var v = bundleVariants[filename];
				if(v.Contains(null)) {
					return true;
				}
			}

			int lastDotIndex = filename.LastIndexOf('.');
			var bundleNameFromFile  = (lastDotIndex > 0) ? filename.Substring(0, lastDotIndex): filename;
			var variantNameFromFile = (lastDotIndex > 0) ? filename.Substring(lastDotIndex+1): null;

			if(!bundleVariants.ContainsKey(bundleNameFromFile)) {
				return false;
			}

			var variants = bundleVariants[bundleNameFromFile];
			return variants.Contains(variantNameFromFile);
		}
	}
}