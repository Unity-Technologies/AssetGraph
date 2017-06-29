using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	[CustomNode("Create Assets/Generate Asset", 51)]
	public class AssetGenerator : Node {

        [System.Serializable]
        public class GeneratorEntry
        {
            public string m_name;
            public string m_id;
            public SerializableMultiTargetInstance m_instance;

            public GeneratorEntry(string name, Model.ConnectionPointData point) {
                m_name = name;
                m_id = point.Id;
                m_instance = new SerializableMultiTargetInstance();
            }

            public GeneratorEntry(string name, SerializableMultiTargetInstance i, Model.ConnectionPointData point) {
                m_name = name;
                m_id = point.Id;
                m_instance = new SerializableMultiTargetInstance(i);
            }
        }

        [SerializeField] private List<GeneratorEntry> m_entries;
        [SerializeField] private string m_defaultOutputPointId;

        private GeneratorEntry m_removingEntry;

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
            m_entries = new List<GeneratorEntry>();

            data.AddDefaultInputPoint();
            var point = data.AddDefaultOutputPoint();
            m_defaultOutputPointId = point.Id;
		}

		public override Node Clone(Model.NodeData newData) {
            var newNode = new AssetGenerator();
            newData.AddDefaultInputPoint();
            newData.AddDefaultOutputPoint();
            var point = newData.AddDefaultOutputPoint();
            newNode.m_defaultOutputPointId = point.Id;

            newNode.m_entries = new List<GeneratorEntry>();
            foreach(var s in m_entries) {
                newNode.AddEntry (newData, s);
            }

			return newNode;
		}

        private void DrawGeneratorSetting(
            GeneratorEntry entry, 
            NodeGUI node, 
            AssetReferenceStreamManager streamManager, 
            NodeGUIEditor editor, 
            Action onValueChanged) 
        {
            var generator = entry.m_instance.Get<IAssetGenerator>(editor.CurrentEditingGroup);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                var newName = EditorGUILayout.TextField ("Name", entry.m_name);
                if (newName != entry.m_name) {
                    using(new RecordUndoScope("Change Name", node, true)) {
                        entry.m_name = newName;
                        UpdateGeneratorEntry (node.Data, entry);
                        // event must raise to propagate change to connection associated with point
                        NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_LABELCHANGED, node, Vector2.zero, GetConnectionPoint(node.Data, entry)));
                        onValueChanged();
                    }
                }

                var map = AssetGeneratorUtility.GetAttributeAssemblyQualifiedNameMap();
                if(map.Count > 0) {
                    using(new GUILayout.HorizontalScope()) {
                        GUILayout.Label("AssetGenerator");
                        var guiName = AssetGeneratorUtility.GetGUIName(entry.m_instance.ClassName);

                        if (GUILayout.Button(guiName, "Popup", GUILayout.MinWidth(150f))) {
                            var builders = map.Keys.ToList();

                            if(builders.Count > 0) {
                                NodeGUI.ShowTypeNamesMenu(guiName, builders, (string selectedGUIName) => 
                                    {
                                        using(new RecordUndoScope("Change AssetGenerator class", node, true)) {
                                            generator = AssetGeneratorUtility.CreateGenerator(selectedGUIName);
                                            entry.m_instance.Set(editor.CurrentEditingGroup, generator);
                                            onValueChanged();
                                        }
                                    } 
                                );
                            }
                        }

                        MonoScript s = TypeUtility.LoadMonoScript(entry.m_instance.ClassName);

                        using(new EditorGUI.DisabledScope(s == null)) {
                            if(GUILayout.Button("Edit", GUILayout.Width(50))) {
                                AssetDatabase.OpenAsset(s, 0);
                            }
                        }
                    }
                } else {
                    if(!string.IsNullOrEmpty(entry.m_instance.ClassName)) {
                        EditorGUILayout.HelpBox(
                            string.Format(
                                "Your AssetGenerator script {0} is missing from assembly. Did you delete script?", entry.m_instance.ClassName), MessageType.Info);
                    } else {
                        string[] menuNames = Model.Settings.GUI_TEXT_MENU_GENERATE_ASSETGENERATOR.Split('/');
                        EditorGUILayout.HelpBox(
                            string.Format(
                                "You need to create at least one AssetGenerator script to use this node. To start, select {0}>{1}>{2} menu and create new script from template.",
                                menuNames[1],menuNames[2], menuNames[3]
                            ), MessageType.Info);
                    }
                }

                GUILayout.Space(10f);

                editor.DrawPlatformSelector(node);
                using (new EditorGUILayout.VerticalScope()) {
                    var disabledScope = editor.DrawOverrideTargetToggle(node, entry.m_instance.ContainsValueOf(editor.CurrentEditingGroup), (bool enabled) => {
                        if(enabled) {
                            entry.m_instance.CopyDefaultValueTo(editor.CurrentEditingGroup);
                        } else {
                            entry.m_instance.Remove(editor.CurrentEditingGroup);
                        }
                        onValueChanged();
                    });

                    using (disabledScope) {
                        if (generator != null) {
                            Action onChangedAction = () => {
                                using(new RecordUndoScope("Change AssetGenerator Setting", node)) {
                                    entry.m_instance.Set(editor.CurrentEditingGroup, generator);
                                    onValueChanged();
                                }
                            };

                            generator.OnInspectorGUI(onChangedAction);
                        }
                    }
                }

                GUILayout.Space (4);

                if (GUILayout.Button ("Remove Generator")) {
                    m_removingEntry = entry;
                }
            }
        }

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			EditorGUILayout.HelpBox("Generate Asset: Generate new asset from incoming asset.", MessageType.Info);
			editor.UpdateNodeName(node);

            foreach (var s in m_entries) {
                DrawGeneratorSetting (s, node, streamManager, editor, onValueChanged);
            }

            if (m_removingEntry != null) {
                using (new RecordUndoScope ("Remove Generator", node)) {
                    RemoveGeneratorEntry (node.Data, m_removingEntry);
                    m_removingEntry = null;
                    onValueChanged ();
                }
            }

            GUILayout.Space (8);

            if (GUILayout.Button ("Add Generator")) {
                using (new RecordUndoScope ("Add Generator", node)) {
                    AddEntry (node.Data);
                    onValueChanged ();
                }
            }
		}

		public override void OnContextMenuGUI(GenericMenu menu) {
            foreach (var s in m_entries) {
                MonoScript script = TypeUtility.LoadMonoScript(s.m_instance.ClassName);
                if(script != null) {
                    menu.AddItem(
                        new GUIContent(string.Format("Edit Script({0})", script.name)),
                        false, 
                        () => {
                            AssetDatabase.OpenAsset(script, 0);
                        }
                    );
                }
            }
		}

		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{

            ValidateAssetGenerator(node, target, incoming,
                () => {
                    throw new NodeException(node.Name + " :AssetGenerator is not specified. Please select generator from Inspector.", node.Id);
                },
                () => {
                    throw new NodeException(node.Name + " :Failed to create AssetGenerator from settings. Please fix settings from Inspector.", node.Id);
                },
                (AssetReference badAsset, string msg) => {
                    throw new NodeException(string.Format("{0} :{1} : Source: {2}", node.Name, msg, badAsset.importFrom), node.Id);
                },
                (AssetReference badAsset) => {
                    throw new NodeException(string.Format("{0} :Can not import incoming asset {1}.", node.Name, badAsset.fileNameAndExtension), node.Id);
                }
            );

			if(incoming == null) {
				return;
			}

            if(connectionsToOutput == null || Output == null) {
                return;
            }

            var allOutput = new Dictionary<string, Dictionary<string, List<AssetReference>>>();

            foreach(var outPoints in node.OutputPoints) {
                allOutput[outPoints.Id] = new Dictionary<string, List<AssetReference>>();
            }

            var defaultOutputCond = connectionsToOutput.Where (c => c.FromNodeConnectionPointId == m_defaultOutputPointId);
            Model.ConnectionData defaultOutput = null;
            if (defaultOutputCond.Any ()) {
                defaultOutput = defaultOutputCond.First ();
            }

			foreach(var ag in incoming) {
                if (defaultOutput != null) {
                    Output(defaultOutput, ag.assetGroups);
                }
                foreach(var groupKey in ag.assetGroups.Keys) {
                    foreach(var a in ag.assetGroups [groupKey]) {
                        foreach (var entry in m_entries) {
                            var assetOutputDir = FileUtility.EnsureAssetGeneratorCacheDirExists(target, node);
                            var generator = entry.m_instance.Get<IAssetGenerator>(target);
                            UnityEngine.Assertions.Assert.IsNotNull(generator);

                            var newItem = FileUtility.PathCombine (assetOutputDir, entry.m_id, a.fileName + generator.Extension);
                            var output = allOutput[entry.m_id];
                            if(!output.ContainsKey(groupKey)) {
                                output[groupKey] = new List<AssetReference>();
                            }
                            output[groupKey].Add(AssetReferenceDatabase.GetReferenceWithType (newItem, generator.AssetType));
                        }
                    }
				}
			}

            foreach(var dst in connectionsToOutput) {
                if(allOutput.ContainsKey(dst.FromNodeConnectionPointId)) {
                    Output(dst, allOutput[dst.FromNodeConnectionPointId]);
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
			if(incoming == null) {
				return;
			}

            bool isAnyAssetGenerated = false;

            foreach (var entry in m_entries) {
                var generator = entry.m_instance.Get<IAssetGenerator>(target);
                UnityEngine.Assertions.Assert.IsNotNull(generator);

                var assetOutputDir = FileUtility.EnsureAssetGeneratorCacheDirExists(target, node);
                var assetSaveDir  = FileUtility.PathCombine (assetOutputDir, entry.m_id);

                if (!Directory.Exists (assetSaveDir)) {
                    Directory.CreateDirectory (assetSaveDir);
                }

                foreach(var ag in incoming) {
                    foreach(var assets in ag.assetGroups.Values) {
                        foreach (var a in assets) {
                            if(AssetGenerateInfo.DoesAssetNeedRegenerate(entry, node, target, a)) {

                                var assetSavePath = FileUtility.PathCombine (assetSaveDir, a.fileName + generator.Extension);

                                if (!generator.GenerateAsset (a, assetSavePath)) {
                                    throw new AssetBundleGraphException(string.Format("{0} :Failed to generate asset for {1}", 
                                        node.Name, entry.m_name));
                                }
                                if (!File.Exists (assetSavePath)) {
                                    throw new AssetBundleGraphException(string.Format("{0} :{1} returned success, but generated asset not found.", 
                                        node.Name, entry.m_name));
                                }

                                isAnyAssetGenerated = true;

                                LogUtility.Logger.LogFormat(LogType.Log, "{0} is (re)generating Asset:{1} with {2}({3})", node.Name, assetSavePath,
                                    AssetGeneratorUtility.GetGUIName(entry.m_instance.ClassName),
                                    AssetGeneratorUtility.GetVersion(entry.m_instance.ClassName));

                                if(progressFunc != null) progressFunc(node, string.Format("Creating {0}", assetSavePath), 0.5f);

                                AssetGenerateInfo.SaveAssetGenerateInfo(entry, node, target, a);
                            }
                        }
                    }
                }
            }

            if (isAnyAssetGenerated) {
                AssetDatabase.Refresh ();
            }
		}

        public void AddEntry(Model.NodeData n) {
            var point = n.AddOutputPoint("");
            var newEntry = new GeneratorEntry("", point);
            m_entries.Add(newEntry);
            UpdateGeneratorEntry(n, newEntry);
        }

        public void AddEntry(Model.NodeData n, GeneratorEntry src) {
            var point = n.AddOutputPoint(src.m_name);
            var newEntry = new GeneratorEntry(src.m_name, src.m_instance, point);
            m_entries.Add(newEntry);
            UpdateGeneratorEntry(n, newEntry);
        }

        public void RemoveGeneratorEntry(Model.NodeData n, GeneratorEntry e) {
            m_entries.Remove(e);
            n.OutputPoints.Remove(GetConnectionPoint(n, e));
        }

        public Model.ConnectionPointData GetConnectionPoint(Model.NodeData n, GeneratorEntry e) {
            Model.ConnectionPointData p = n.OutputPoints.Find(v => v.Id == e.m_id);
            UnityEngine.Assertions.Assert.IsNotNull(p);
            return p;
        }

        public void UpdateGeneratorEntry(Model.NodeData n, GeneratorEntry e) {

            Model.ConnectionPointData p = n.OutputPoints.Find(v => v.Id == e.m_id);
            UnityEngine.Assertions.Assert.IsNotNull(p);

            p.Label = e.m_name;
        }

		public void ValidateAssetGenerator (
			Model.NodeData node,
			BuildTarget target,
			IEnumerable<PerformGraph.AssetGroups> incoming, 
            Action noGeneratorData,
			Action failedToCreateGenerator,
            Action<AssetReference, string> canNotGenerateAsset,
			Action<AssetReference> canNotImportAsset
		) {
            foreach (var entry in m_entries) {

                var generator = entry.m_instance.Get<IAssetGenerator>(target);

                if(null == generator ) {
                    failedToCreateGenerator();
                }

                if(null != generator && null != incoming) {
                    foreach(var ag in incoming) {
                        foreach(var assets in ag.assetGroups.Values) {
                            foreach (var a in assets) {
                                if(string.IsNullOrEmpty(a.importFrom)) {
                                    canNotImportAsset(a);
                                    continue;
                                }
                                string msg = null;
                                if(!generator.CanGenerateAsset(a, out msg)) {
                                    canNotGenerateAsset(a, msg);
                                }
                            }
                        }
                    }
                }
            }
		}			
	}
}