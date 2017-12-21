using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	
	[CustomNode("Modify Assets/Overwrite Import Settings", 60)]
	public class ImportSetting : Node, Model.NodeDataImporter {

		[SerializeField] private SerializableMultiTargetString m_spritePackingTagNameTemplate;          // legacy
        [SerializeField] private bool m_overwritePackingTag;                                            // legacy
        [SerializeField] private bool m_useCustomSettingAsset;                                          // legacy
        [SerializeField] private bool m_overwriteSpriteSheet;                                           // legacy
        [SerializeField] private string m_customSettingAssetGuid;
        [SerializeField] private string m_referenceAssetGuid;
        [SerializeField] public SerializableMultiTargetInstance m_configuratorInstance;

        private Object m_customSettingAssetObject;
        private Editor m_importerEditor;
        private Type m_configureImporterFor;

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
                if (m_importerEditor != null) {
                    UnityEngine.Object.DestroyImmediate (m_importerEditor);
                    m_importerEditor = null;
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
                if (m_importerEditor != null) {
                    UnityEngine.Object.DestroyImmediate (m_importerEditor);
                    m_importerEditor = null;
                }
            }
        }

		public override void Initialize(Model.NodeData data) {
			m_spritePackingTagNameTemplate = new SerializableMultiTargetString("*");
			m_overwritePackingTag = false;
            m_useCustomSettingAsset = false;
            m_customSettingAssetGuid = string.Empty;
            m_overwriteSpriteSheet = false;
            m_referenceAssetGuid = string.Empty;
            m_configureImporterFor = null;

            m_configuratorInstance = new SerializableMultiTargetInstance ();

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
            newNode.m_configuratorInstance = new SerializableMultiTargetInstance (m_configuratorInstance);
            newNode.m_referenceAssetGuid = string.Empty;
            newNode.m_configureImporterFor = null;

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();
			return newNode;
		}

		public override bool OnAssetsReimported(
			Model.NodeData nodeData,
			AssetReferenceStreamManager streamManager,
			BuildTarget target, 
            AssetPostprocessorContext ctx,
            bool isBuilding)
		{
            var samplingDirectoryPath = FileUtility.PathCombine(Model.Settings.Path.SavedSettingsPath, "ImportSettings", nodeData.Id);

            foreach(var importedAsset in ctx.ImportedAssets) {
                if(importedAsset.importFrom.StartsWith(samplingDirectoryPath)) {
					return true;
				}
                // Test this later
//                if (m_customSettingAssetGuid != null) {
//                    if(imported.StartsWith(AssetDatabase.GUIDToAssetPath(m_customSettingAssetGuid))) {
//                        return true;
//                    }
//                }
			}

			return false;
		}

        public override void OnNodeDelete(Model.NodeData nodeData) {
            var savedSettingDir = FileUtility.PathCombine(Model.Settings.Path.SavedSettingsPath, "ImportSettings", nodeData.Id);
            if (AssetDatabase.IsValidFolder (savedSettingDir)) {
                FileUtility.DeleteDirectory (savedSettingDir, true);
            }
        }

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			EditorGUILayout.HelpBox("Overwrite Import Setting: Overwrite import settings of incoming assets.", MessageType.Info);
			editor.UpdateNodeName(node);

            // prevent inspector flicking by new Editor changing active selction
            node.SetActive (true);

			using (new EditorGUILayout.VerticalScope()) {

                Type importerType = null;
                Type assetType = null;

                var referenceImporter = GetReferenceAssetImporter (node.Data, false);
                if (referenceImporter != null) {
                    importerType = referenceImporter.GetType ();
                    assetType = TypeUtility.GetMainAssetTypeAtPath (AssetDatabase.GUIDToAssetPath (m_referenceAssetGuid));
                } else {
                    GUILayout.Space(10f);
                    using (new EditorGUILayout.VerticalScope (GUI.skin.box)) {
                        EditorGUILayout.HelpBox ("Import setting type can be set by incoming asset, or you can specify by selecting.", MessageType.Info);
                        using (new EditorGUILayout.HorizontalScope ()) {
                            EditorGUILayout.LabelField ("Importer Type");
                            if (GUILayout.Button ("", "Popup", GUILayout.MinWidth (150f))) {

                                var menu = new GenericMenu ();

                                var guiMap = ImporterConfiguratorUtility.GetImporterConfiguratorGuiNameTypeMap ();
                                var guiNames = guiMap.Keys.ToArray ();

                                for (var i = 0; i < guiNames.Length; i++) {
                                    var index = i;
                                    menu.AddItem (
                                        new GUIContent (guiNames [i]),
                                        false,
                                        () => {
                                            ResetConfig (node.Data);
                                            m_configureImporterFor = guiMap [guiNames [index]];
                                            // call Validate
                                            NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_NODE_UPDATED, node));
                                        }
                                    );
                                }
                                menu.ShowAsContext ();
                            }
                        }
                    }
                    return;
                }

                if (importerType != null && assetType != null) {
                    GUILayout.Space (10f);
                    DoCustomAssetGUI (assetType, importerType, node, editor, onValueChanged);
                }

                // get reference importer again (enabling custom asset this time)
                referenceImporter = GetReferenceAssetImporter (node.Data, true);

                if (referenceImporter != null) {
                    var configurator = m_configuratorInstance.Get<IAssetImporterConfigurator> (editor.CurrentEditingGroup);
                    if (configurator != null) {
                        GUILayout.Space (10f);

                        Action onChangedAction = () => {
                            using(new RecordUndoScope(string.Format("Change {0} Setting", node.Name), node)) {
                                m_configuratorInstance.Set(editor.CurrentEditingGroup, configurator);
                                onValueChanged();
                            }
                        };

                        configurator.OnInspectorGUI (referenceImporter, editor.CurrentEditingGroup, onChangedAction);
                    }

                    if (m_importerEditor == null) {
                        m_importerEditor = Editor.CreateEditor (referenceImporter);
                    }
                }

                if (m_importerEditor != null) {
                    GUILayout.Space (10f);
                    GUILayout.Label (string.Format("Import Setting ({0})", importerType.Name));
                    m_importerEditor.OnInspectorGUI ();
                }

                GUILayout.Space (40f);
                using (new EditorGUILayout.HorizontalScope (GUI.skin.box)) {
                    GUILayout.Space (4f);
                    EditorGUILayout.LabelField ("Clear Saved Import Setting");

                    if (GUILayout.Button ("Clear")) {
                        if (EditorUtility.DisplayDialog ("Clear Saved Import Setting", 
                            string.Format ("Do you want to reset saved import setting for \"{0}\"? This operation is not undoable.", node.Name), "OK", "Cancel")) {
                            ResetConfig (node.Data);
                        }
                    }
                }
			}
		}

        private void DoCustomAssetGUI(Type assetType, Type importerType, NodeGUI node, NodeGUIEditor editor, Action onValueChanged) {
            // Custom Settings Asset
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
                    var newObject = EditorGUILayout.ObjectField ("Asset", CustomSettingAsset, assetType, false);
                    if (importerType == typeof(ModelImporter) && newObject != null) {
                        // disallow selecting non-model prefab
                        if (PrefabUtility.GetPrefabType (newObject) != PrefabType.ModelPrefab) {
                            newObject = CustomSettingAsset;
                        }
                    }
                    if (importerType == typeof(TrueTypeFontImporter) && newObject != null) {
                        var selectedAssetPath = AssetDatabase.GetAssetPath (newObject);
                        var importer = AssetImporter.GetAtPath (selectedAssetPath);
                        // disallow selecting Custom Font
                        if (importer != null && importer.GetType() != typeof(TrueTypeFontImporter)) {
                            newObject = CustomSettingAsset;
                        }
                    }


                    if (newObject != CustomSettingAsset) {
                        using (new RecordUndoScope ("Change Custom Setting Asset", node, true)) {
                            CustomSettingAsset = newObject;
                            onValueChanged ();
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
                }
                EditorGUILayout.HelpBox (
                    "Custom setting asset is useful when you need specific needs for setting asset; i.e. when configuring with multiple sprite mode.", 
                    MessageType.Info);
            }
        }


		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
            if (m_configureImporterFor != null) {
                CreateConfigurator (node, m_configureImporterFor);
            }

            if (string.IsNullOrEmpty (m_referenceAssetGuid)) {
                // give a try first in sampling file
                var a = AssetReferenceUtility.FindFirstIncomingAssetReference(incoming);
                if (a != null) {
                    CreateConfigurator(node, a.importerType);
                }
            }

            ValidateInputSetting(node, target, incoming);

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

        private void CreateConfigurator(Model.NodeData node, Type importerType) {

            var configFileGUID = ImporterConfiguratorUtility.FindSettingTemplateFileGUID (importerType);
            var configFilePath = AssetDatabase.GUIDToAssetPath (configFileGUID);

            if (string.IsNullOrEmpty (configFilePath)) {
                throw new NodeException("Setting template file not found. Incoming file type must be properly configured with CustomImporterConfigurator.", 
                    "Place setting file template for this file type in SettingTemplate folder in your project.", node);
            }

            var samplingDirectoryPath = FileUtility.PathCombine(Model.Settings.Path.SavedSettingsPath, "ImportSettings", node.Id);
			if (!Directory.Exists(samplingDirectoryPath)) {
				Directory.CreateDirectory(samplingDirectoryPath);
			}

            IAssetImporterConfigurator configurator = ImporterConfiguratorUtility.CreateConfigurator (importerType);
            if (configurator == null) {
                throw new NodeException("Failed to create importer configurator for " + importerType.FullName,
                    "Make sure CustomAssetImporterConfigurator for this type is added in your project.", node);
            }
            m_configuratorInstance.SetDefaultValue (configurator);

            var targetFilePath = FileUtility.PathCombine(samplingDirectoryPath, Path.GetFileName(configFilePath));

            FileUtility.CopyFile(configFilePath, targetFilePath);

			AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);

            m_referenceAssetGuid = AssetDatabase.AssetPathToGUID (targetFilePath);
            m_configureImporterFor = null;
		}

		public void ResetConfig(Model.NodeData node) {
            m_useCustomSettingAsset = false;
            CustomSettingAssetGuid = string.Empty;
            m_referenceAssetGuid = null;
            m_configuratorInstance = new SerializableMultiTargetInstance ();
            var sampleFileDir = FileUtility.PathCombine(Model.Settings.Path.SavedSettingsPath, "ImportSettings", node.Id);
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

            if (!string.IsNullOrEmpty(m_referenceAssetGuid)) {
                var path = AssetDatabase.GUIDToAssetPath (m_referenceAssetGuid);
                return AssetImporter.GetAtPath (path);
            }

            return null;
		}

		private void ApplyImportSetting(BuildTarget target, Model.NodeData node, IEnumerable<PerformGraph.AssetGroups> incoming) {

			var referenceImporter = GetReferenceAssetImporter(node, true);

            var configurator = m_configuratorInstance.Get<IAssetImporterConfigurator> (target);
            Assertions.Assert.IsNotNull (configurator);

			foreach(var ag in incoming) {
				foreach(var groupKey in ag.assetGroups.Keys) {
					var assets = ag.assetGroups[groupKey];
					foreach(var asset in assets) {

                        // skip if incoming asset is this custom setting asset
                        if (m_useCustomSettingAsset) {
                            var assetGuid = AssetDatabase.AssetPathToGUID (asset.importFrom);
                            if (assetGuid == m_customSettingAssetGuid) {
                                continue;
                            }
                        }

						var importer = AssetImporter.GetAtPath(asset.importFrom);
						bool importerModified = false;

                        if(configurator.IsModified(referenceImporter, importer, target, groupKey)) {
                            configurator.Configure(referenceImporter, importer, target, groupKey);
                            AssetProcessEventRecord.GetRecord ().LogModify (asset);
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

        private void ValidateInputSetting (
			Model.NodeData node,
			BuildTarget target,
			IEnumerable<PerformGraph.AssetGroups> incoming) 
        {
            var firstAsset = AssetReferenceUtility.FindFirstIncomingAssetReference (incoming);
            Type firstAssetImporterType = null;

            // check if first Asset has importer
            if (firstAsset != null) {
                firstAssetImporterType = firstAsset.importerType;
                if (firstAssetImporterType == null) {
                    throw new NodeException(string.Format("Incoming asset '{0}' does not have importer. (type={1}) Perhaps you want to use Modifier instead?", 
                        firstAsset.fileNameAndExtension, firstAsset.assetType.FullName), 
                        "Add ScriptedImporter for this type of asset or replace this node to use Modifier.",
                        node);
                }
            }

            // check if all incoming assets are the same asset types
			if(firstAssetImporterType != null) {
                foreach(var ag in incoming) {
                    foreach(var assets in ag.assetGroups.Values) {
                        foreach(var a in assets) {
                            if(a.importerType != firstAssetImporterType) {
                                throw new NodeException(
                                    string.Format("ImportSetting expect {0}, but different type of incoming asset is found({1}, {2})", firstAssetImporterType.FullName, a.fileNameAndExtension, a.importerType), 
                                    string.Format("Remove {0} from node input, or change this importer type.", a.fileName),
                                    node);
                            }
                        }
                    }
                }
			}

            // check if there is a valid reference asset
            if(!string.IsNullOrEmpty(m_referenceAssetGuid)) {
                var referenceImporter = GetReferenceAssetImporter (node, false);

                if (referenceImporter == null) {
                    throw new NodeException("Reference importer not found.",
                        "Configure reference importer from inspector", node);
                }
            }

            // check if there is a valid custom setting asset
            if (m_useCustomSettingAsset && CustomSettingAsset == null) {
                throw new NodeException("You must select custom setting asset.",
                    "Select custom setting asset.", node);
            }

            // check if reference asset type matches with incoming asset types
            if(firstAssetImporterType != null) {
                Type targetType = GetReferenceAssetImporter(node, false).GetType();
                if( targetType != firstAssetImporterType ) {
                    throw new NodeException(
                        string.Format("Incoming asset type is does not match with this ImportSetting (Expected type:{0}, Incoming type:{1}).", targetType.FullName, firstAssetImporterType.FullName), 
                        string.Format("Remove {0} from incoming assets.", firstAsset.fileName), node);
                }
            }

            // check if there is valid configurator for this asset importer
            var importer = GetReferenceAssetImporter (node, true);
            if (importer != null) {
                var configuratorType = ImporterConfiguratorUtility.GetConfiguratorTypeFor (importer.GetType());
                if (configuratorType == null) {
                    throw new NodeException (
                        string.Format ("Configurator for {0} not found.", importer.GetType().FullName), 
                        string.Format ("Add CustomAssetImporterConfigurator."),
                        node);
                }

                var c = m_configuratorInstance.Get<IAssetImporterConfigurator> (target);
                if (c == null) {
                    throw new NodeException("Failed to get configurator for " + importer.GetType().FullName,
                        "You may need to reset this node.", node);
                }
            }
		}
	}
}
