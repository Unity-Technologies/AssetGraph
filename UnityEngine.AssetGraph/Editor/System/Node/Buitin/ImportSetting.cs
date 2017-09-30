using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	
	[CustomNode("Modify Assets/Overwrite Import Setting", 60)]
	public class ImportSetting : Node, Model.NodeDataImporter {

		public enum ConfigStatus {
			NoSampleFound,
			TooManySamplesFound,
			GoodSampleFound
		}

		private static readonly string[] s_importerTypeList = new string[] {
			Model.Settings.GUI_TEXT_SETTINGTEMPLATE_MODEL,
			Model.Settings.GUI_TEXT_SETTINGTEMPLATE_TEXTURE,
			Model.Settings.GUI_TEXT_SETTINGTEMPLATE_AUDIO,
            #if UNITY_5_6_OR_NEWER
			Model.Settings.GUI_TEXT_SETTINGTEMPLATE_VIDEO,
            #endif
		};

		[SerializeField] private SerializableMultiTargetString m_spritePackingTagNameTemplate;
        [SerializeField] private bool m_overwritePackingTag;
        [SerializeField] private bool m_useCustomSettingAsset;
        [SerializeField] private bool m_overwriteSpriteSheet;
        [SerializeField] private string m_customSettingAssetGuid;

        private Object m_customSettingAssetObject;

		private Editor m_importerEditor;

		public override string ActiveStyle {
			get {
				return "node 8 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 8";
			}
		}

		public override string Category {
			get {
				return "Modify";
			}
		}

        private Object CustomSettingAsset {
            get {
                if (m_customSettingAssetObject == null) {
                    if (!string.IsNullOrEmpty (m_customSettingAssetGuid)) {
                        var path = AssetDatabase.GUIDToAssetPath (m_customSettingAssetGuid);
                        if (!string.IsNullOrEmpty (path)) {
                            m_customSettingAssetObject = AssetDatabase.LoadMainAssetAtPath (path);
                        }
                    }
                }
                return m_customSettingAssetObject;
            }
            set {
                m_customSettingAssetObject = value;
                if (value != null) {
                    var path = AssetDatabase.GetAssetPath (value);
                    m_customSettingAssetGuid = AssetDatabase.AssetPathToGUID (path);
                } else {
                    m_customSettingAssetGuid = string.Empty;
                }
            }
        }

        private string CustomSettingAssetGuid {
            get {
                return m_customSettingAssetGuid;
            }
            set {
                m_customSettingAssetGuid = value;
                m_customSettingAssetObject = null;
            }
        }

		public override void Initialize(Model.NodeData data) {
			m_spritePackingTagNameTemplate = new SerializableMultiTargetString("*");
			m_overwritePackingTag = false;
            m_useCustomSettingAsset = false;
            m_customSettingAssetGuid = string.Empty;
            m_overwriteSpriteSheet = false;

			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public void Import(V1.NodeData v1, Model.NodeData v2) {
			// do nothing
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new ImportSetting();

            newNode.m_overwritePackingTag = m_overwritePackingTag;
            newNode.m_spritePackingTagNameTemplate = new SerializableMultiTargetString(m_spritePackingTagNameTemplate);
            newNode.m_useCustomSettingAsset = m_useCustomSettingAsset;
            newNode.m_customSettingAssetGuid = m_customSettingAssetGuid;
            newNode.m_overwriteSpriteSheet = m_overwriteSpriteSheet;

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();
			return newNode;
		}

		public override bool OnAssetsReimported(
			Model.NodeData nodeData,
			AssetReferenceStreamManager streamManager,
			BuildTarget target, 
			string[] importedAssets, 
			string[] deletedAssets, 
			string[] movedAssets, 
			string[] movedFromAssetPaths)
		{
            var samplingDirectoryPath = FileUtility.PathCombine(Model.Settings.Path.ImporterSettingsPath, nodeData.Id);

			foreach(var imported in importedAssets) {
				if(imported.StartsWith(samplingDirectoryPath)) {
					return true;
				}
			}

			return false;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			EditorGUILayout.HelpBox("Overwrite Import Setting: Overwrite import settings of incoming assets.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

            // prevent inspector flicking by new Editor changing active selction
            node.SetActive (true);

			/*
				importer node has no platform key. 
				platform key is contained by Unity's importer inspector itself.
			*/
			using (new EditorGUILayout.VerticalScope()) {
				Type incomingType = TypeUtility.FindFirstIncomingAssetType(streamManager, node.Data.InputPoints[0]);
				ImportSetting.ConfigStatus status = 
					ImportSetting.GetConfigStatus(node.Data);

				if(incomingType == null) {
					// try to retrieve incoming type from configuration
					if(status == ImportSetting.ConfigStatus.GoodSampleFound) {
						incomingType = GetReferenceAssetImporter(node.Data, false).GetType();
					} else {
						using (new EditorGUILayout.VerticalScope (GUI.skin.box)) {
							EditorGUILayout.HelpBox ("Import setting type can be set by incoming asset, or you can specify by selecting.", MessageType.Info);
							using (new EditorGUILayout.HorizontalScope ()) {
								EditorGUILayout.LabelField ("Importer Type");
								if (GUILayout.Button ("", "Popup", GUILayout.MinWidth (150f))) {

									var menu = new GenericMenu ();

									for (var i = 0; i < s_importerTypeList.Length; i++) {
										var index = i;
										menu.AddItem (
											new GUIContent (s_importerTypeList [i]),
											false,
											() => {
												ResetConfig (node.Data);
												var configFilePath = FileUtility.GetImportSettingTemplateFilePath (s_importerTypeList [index]);
												SaveSampleFile (node.Data, configFilePath);
											}
										);
									}
									menu.ShowAsContext ();
								}
							}
						}
						return;
					}
				}

				switch(status) {
				case ImportSetting.ConfigStatus.NoSampleFound:
					// ImportSetting.Setup() must run to grab another sample to configure.
					EditorGUILayout.HelpBox("Press Refresh to configure.", MessageType.Info);
					node.Data.NeedsRevisit = true;
					break;
                case ImportSetting.ConfigStatus.GoodSampleFound:
                    var importer = GetReferenceAssetImporter (node.Data, true);

                    if (m_importerEditor == null) {
                        if (importer != null) {
                            m_importerEditor = Editor.CreateEditor (importer);
                        }
                    }

                    // Custom Sprite Packing Tag
                    if (incomingType == typeof(UnityEditor.TextureImporter)) {
                        var textureImporter = importer as TextureImporter;
                        if (textureImporter != null) {
                            if (textureImporter.textureType == TextureImporterType.Sprite) {
                                using (new EditorGUILayout.VerticalScope (GUI.skin.box)) {
                                    GUILayout.Label ("Sprite Settings");
                                    GUILayout.Space (4f);
                                    m_overwriteSpriteSheet = EditorGUILayout.ToggleLeft ("Configure Sprite Mode", m_overwriteSpriteSheet);
                                    m_overwritePackingTag = EditorGUILayout.ToggleLeft ("Configure Sprite Packing Tag", m_overwritePackingTag);

                                    if (m_overwritePackingTag) {
                                        var val = m_spritePackingTagNameTemplate [editor.CurrentEditingGroup];

                                        var newValue = EditorGUILayout.TextField ("Packing Tag", val);
                                        if (newValue != val) {
                                            using (new RecordUndoScope ("Change Packing Tag", node, true)) {
                                                m_spritePackingTagNameTemplate [editor.CurrentEditingGroup] = newValue;
                                                onValueChanged ();
                                            }
                                        }
                                    }
                                    EditorGUILayout.HelpBox (
                                        "You can configure packing tag name with \"*\" to include group name in your sprite tag.", 
                                        MessageType.Info);
                                }
                                GUILayout.Space (10);
                            }
                        }
                    }

                    // Custom Sample Asset
                    using (new EditorGUILayout.VerticalScope (GUI.skin.box)) {

                        var newUseCustomAsset = EditorGUILayout.ToggleLeft ("Use Custom Setting Asset", m_useCustomSettingAsset);
                        if (newUseCustomAsset != m_useCustomSettingAsset) {
                            using (new RecordUndoScope ("Change Custom Setting Asset", node, true)) {
                                m_useCustomSettingAsset = newUseCustomAsset;
                                onValueChanged ();

                                if (m_importerEditor != null) {
                                    UnityEngine.Object.DestroyImmediate (m_importerEditor);
                                    m_importerEditor = null;
                                }
                            }
                        }

                        if (m_useCustomSettingAsset) {
                            var assetType = GetAssetTypeFromImporterType (incomingType);
                            if (assetType != null) {
                                var newObject = EditorGUILayout.ObjectField ("Asset", CustomSettingAsset, assetType, false);
                                if (incomingType == typeof(ModelImporter)) {
                                    // disallow selecting non-model prefab
                                    if (PrefabUtility.GetPrefabType (newObject) != PrefabType.ModelPrefab) {
                                        newObject = CustomSettingAsset;
                                    }
                                }

                                if (newObject != CustomSettingAsset) {
                                    using (new RecordUndoScope ("Change Custom Setting Asset", node, true)) {
                                        CustomSettingAsset = newObject;
                                        onValueChanged ();

                                        if (m_importerEditor != null) {
                                            UnityEngine.Object.DestroyImmediate (m_importerEditor);
                                            m_importerEditor = null;
                                        }
                                    }
                                }
                                if (CustomSettingAsset != null) {
                                    using (new EditorGUILayout.HorizontalScope ()) {
                                        GUILayout.FlexibleSpace ();
                                        if (GUILayout.Button ("Highlight in Project Window", GUILayout.Width (180f))) {
                                            EditorGUIUtility.PingObject (CustomSettingAsset);
                                        }
                                    }
                                }
                            } else {
                                EditorGUILayout.HelpBox (
                                    "Incoming asset type is not supported. Please fix issue first or clear the saved import setting.", 
                                    MessageType.Error);
                                if (m_importerEditor != null) {
                                    UnityEngine.Object.DestroyImmediate (m_importerEditor);
                                    m_importerEditor = null;
                                }
                            }
                        }
                        EditorGUILayout.HelpBox (
                            "Custom setting asset is useful when you need specific needs for setting asset; i.e. when configuring with multiple sprite mode.", 
                            MessageType.Info);
                    }
                    GUILayout.Space (10);

                    if (m_importerEditor != null) {
                        GUILayout.Label (string.Format("Import Setting ({0})", incomingType.Name));
                        m_importerEditor.OnInspectorGUI ();
                        GUILayout.Space (40);
                    }

					using (new EditorGUILayout.HorizontalScope (GUI.skin.box)) {
						GUILayout.Space (4);
						EditorGUILayout.LabelField ("Clear Saved Import Setting");

						if (GUILayout.Button ("Clear")) {
							if (EditorUtility.DisplayDialog ("Clear Saved Import Setting", 
								    string.Format ("Do you want to reset saved import setting for \"{0}\"? This operation is not undoable.", node.Name), "OK", "Cancel")) {
								ResetConfig (node.Data);
							}
						}
					}
					break;
				case ImportSetting.ConfigStatus.TooManySamplesFound:
					if (GUILayout.Button("Reset Import Setting")) {
						ResetConfig(node.Data);
					}
					break;
				}
			}
			return;
		}

		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			Action<Type, Type, AssetReference> multipleAssetTypeFound = (Type expectedType, Type foundType, AssetReference foundAsset) => {
				throw new NodeException(string.Format("{3} :ImportSetting expect {0}, but different type of incoming asset is found({1} {2})", 
					expectedType.FullName, foundType.FullName, foundAsset.fileNameAndExtension, node.Name), node.Id);
			};

			Action<Type> unsupportedType = (Type unsupported) => {
				throw new NodeException(string.Format("{0} :Incoming asset type is not supported by ImportSetting (Incoming type:{1}). Perhaps you want to use Modifier instead?",
					node.Name, (unsupported != null)?unsupported.FullName:"null"), node.Id);
			};

			Action<Type, Type> incomingTypeMismatch = (Type expectedType, Type incomingType) => {
				throw new NodeException(string.Format("{0} :Incoming asset type is does not match with this ImportSetting (Expected type:{1}, Incoming type:{2}).",
					node.Name, (expectedType != null)?expectedType.FullName:"null", (incomingType != null)?incomingType.FullName:"null"), node.Id);
			};
            Action customConfigIsNull = () => {
                throw new NodeException(string.Format("{0} :You must select custom setting asset.", node.Name), node.Id);
            };

			Action<ConfigStatus> errorInConfig = (ConfigStatus _) => {

				var firstAsset = TypeUtility.GetFirstIncomingAsset(incoming);

				if(firstAsset != null) {
					// give a try first in sampling file
					var configFilePath = FileUtility.GetImportSettingTemplateFilePath(firstAsset);
					SaveSampleFile(node, configFilePath);

					ValidateInputSetting(node, target, incoming, multipleAssetTypeFound, unsupportedType, incomingTypeMismatch, (ConfigStatus eType) => {
						if(eType == ConfigStatus.NoSampleFound) {
							throw new NodeException(node.Name + " :ImportSetting has no sampling file. Please configure it from Inspector.", node.Id);
						}
						if(eType == ConfigStatus.TooManySamplesFound) {
							throw new NodeException(node.Name + " :ImportSetting has too many sampling file. Please fix it from Inspector.", node.Id);
                        }
                    }, customConfigIsNull);
				}
			};

            ValidateInputSetting(node, target, incoming, multipleAssetTypeFound, unsupportedType, incomingTypeMismatch, errorInConfig, customConfigIsNull);

			// ImportSettings does not add, filter or change structure of group, so just pass given group of assets
			if(Output != null) {
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();

				if(incoming != null) {
					foreach(var ag in incoming) {
						Output(dst, ag.assetGroups);
					}
				} else {
					Output(dst, new Dictionary<string, List<AssetReference>>());
				}
			}
		}

		public override void Build (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output,
			Action<Model.NodeData, string, float> progressFunc) 
		{
			if(incoming != null){
				ApplyImportSetting(target, node, incoming);
			}
		}

		private void SaveSampleFile(Model.NodeData node, string configFilePath) {
            var samplingDirectoryPath = FileUtility.PathCombine(Model.Settings.Path.ImporterSettingsPath, node.Id);
			if (!Directory.Exists(samplingDirectoryPath)) {
				Directory.CreateDirectory(samplingDirectoryPath);
			}

			UnityEngine.Assertions.Assert.IsNotNull(configFilePath);
			var targetFilePath = FileUtility.PathCombine(samplingDirectoryPath, Path.GetFileName(configFilePath));

			FileUtility.CopyFile(configFilePath, targetFilePath);

			AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
		}

		public static ConfigStatus GetConfigStatus(Model.NodeData node) {
            var sampleFileDir = FileUtility.PathCombine(Model.Settings.Path.ImporterSettingsPath, node.Id);

			if(!Directory.Exists(sampleFileDir)) {
				return ConfigStatus.NoSampleFound;
			}

			var sampleFiles = FileUtility.GetFilePathsInFolder(sampleFileDir)
				.Where(path => !path.EndsWith(Model.Settings.UNITY_METAFILE_EXTENSION))
				.ToList();

			if(sampleFiles.Count == 0) {
				return ConfigStatus.NoSampleFound;
			}
			if(sampleFiles.Count == 1) {
				return ConfigStatus.GoodSampleFound;
			}

			return ConfigStatus.TooManySamplesFound;
		}

		public void ResetConfig(Model.NodeData node) {
			if (m_importerEditor != null) {
				UnityEngine.Object.DestroyImmediate (m_importerEditor);
				m_importerEditor = null;
			}
            m_useCustomSettingAsset = false;
            CustomSettingAssetGuid = string.Empty;
            var sampleFileDir = FileUtility.PathCombine(Model.Settings.Path.ImporterSettingsPath, node.Id);
			FileUtility.RemakeDirectory(sampleFileDir);
		}

        private AssetImporter GetReferenceAssetImporter(Model.NodeData node, bool allowCustom) {

            if (allowCustom) {
                if (m_useCustomSettingAsset) {
                    if (CustomSettingAsset != null) {
                        var path = AssetDatabase.GUIDToAssetPath (CustomSettingAssetGuid);
                        return AssetImporter.GetAtPath (path);
                    } 
                    return null;
                }
            }

            var sampleFileDir = FileUtility.PathCombine(Model.Settings.Path.ImporterSettingsPath, node.Id);

			UnityEngine.Assertions.Assert.IsTrue(Directory.Exists(sampleFileDir));

			var sampleFiles = FileUtility.GetFilePathsInFolder(sampleFileDir)
				.Where(path => !path.EndsWith(Model.Settings.UNITY_METAFILE_EXTENSION))
				.ToList();

			UnityEngine.Assertions.Assert.IsTrue(sampleFiles.Count == 1);

			return AssetImporter.GetAtPath(sampleFiles[0]);	
		}

		private void ApplyImportSetting(BuildTarget target, Model.NodeData node, IEnumerable<PerformGraph.AssetGroups> incoming) {

			var referenceImporter = GetReferenceAssetImporter(node, true);
			var configurator = new ImportSettingsConfigurator(referenceImporter);

            ConfigurationOption opt;
            opt.keepPackingTag  = !m_overwritePackingTag;
            opt.keepSpriteSheet = !m_overwriteSpriteSheet;

			foreach(var ag in incoming) {
				foreach(var groupKey in ag.assetGroups.Keys) {
					var assets = ag.assetGroups[groupKey];
					foreach(var asset in assets) {
						var importer = AssetImporter.GetAtPath(asset.importFrom);
						bool importerModified = false;
                        opt.customPackingTag = GetTagName(target, groupKey);

                        if(!configurator.IsEqual(importer, opt)) {
                            configurator.OverwriteImportSettings(importer, opt);
							importerModified = true;
						}

						if(importerModified) {
							importer.SaveAndReimport();
							asset.TouchImportAsset();
						}
					}
				}
			}
		}

		private string GetTagName(BuildTarget target, string groupName) {
			return m_spritePackingTagNameTemplate[target].Replace("*", groupName);
		}

		private void ApplySpriteTag(BuildTarget target, IEnumerable<PerformGraph.AssetGroups> incoming) {

			foreach(var ag in incoming) {
				foreach(var groupKey in ag.assetGroups.Keys) {
					var assets = ag.assetGroups[groupKey];
					foreach(var asset in assets) {

						if(asset.filterType == typeof(UnityEditor.TextureImporter) ) {
							var importer = AssetImporter.GetAtPath(asset.importFrom) as TextureImporter;

							importer.spritePackingTag = GetTagName(target, groupKey);
							importer.SaveAndReimport();
							asset.TouchImportAsset();
						}
					}
				}
			}
		}

        private Type GetAssetTypeFromImporterType(Type importerType) {

            if (importerType == typeof(TextureImporter)) {
                return typeof(Texture);
            } else if (importerType == typeof(ModelImporter)) {
                return typeof(GameObject);
            } else if (importerType == typeof(AudioImporter)) {
                return typeof(AudioClip);
            }
            #if UNITY_5_6_OR_NEWER
            else if (importerType == typeof(VideoClipImporter)) {
                return typeof(UnityEngine.Video.VideoClip);
            }
            #endif
            return null;
        }

		private void ValidateInputSetting (
			Model.NodeData node,
			BuildTarget target,
			IEnumerable<PerformGraph.AssetGroups> incoming,
			Action<Type, Type, AssetReference> multipleAssetTypeFound,
			Action<Type> unsupportedType,
			Action<Type, Type> incomingTypeMismatch,
			Action<ConfigStatus> errorInConfig,
            Action customAssetIsNull
		) {
			Type expectedType = TypeUtility.FindFirstIncomingAssetType(incoming);
			if(multipleAssetTypeFound != null) {
				if(expectedType != null && incoming != null) {
					foreach(var ag in incoming) {
						foreach(var assets in ag.assetGroups.Values) {
							foreach(var a in assets) {
								Type assetType = a.filterType;
								if(assetType != expectedType) {
									multipleAssetTypeFound(expectedType, assetType, a);
								}
							}
						}
					}
				}
			}

			if(unsupportedType != null) {
				if(expectedType != null) {
					if(expectedType == typeof(UnityEditor.TextureImporter) 	
						|| expectedType == typeof(UnityEditor.ModelImporter) 	
						|| expectedType == typeof(UnityEditor.AudioImporter) 
						#if UNITY_5_6 || UNITY_5_6_OR_NEWER
						|| expectedType == typeof(UnityEditor.VideoClipImporter) 	
						#endif
					) {
						// good. do nothing
					} else {
						unsupportedType(expectedType);
					}
				}
			}

			var status = GetConfigStatus(node);

			if(errorInConfig != null) {
				if(status != ConfigStatus.GoodSampleFound) {
					errorInConfig(status);
				}
			}

			if(incomingTypeMismatch != null) {
				// if there is no incoming assets, there is no way to check if 
				// right type of asset is coming in - so we'll just skip the test
				if(incoming != null && expectedType != null && status == ConfigStatus.GoodSampleFound) {
                    Type targetType = GetReferenceAssetImporter(node, false).GetType();
					if( targetType != expectedType ) {
						incomingTypeMismatch(targetType, expectedType);
					}
				}
			}

            if (m_useCustomSettingAsset && CustomSettingAsset == null) {
                customAssetIsNull ();
            }
		}
	}
}
