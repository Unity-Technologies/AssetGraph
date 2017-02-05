using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	[CustomNode("Prefab Builder", 70)]
	public class PrefabBuilder : INode {

		[SerializeField] private MultiTargetSerializedInstance<IPrefabBuilder> m_instance;
		[SerializeField] private UnityEditor.ReplacePrefabOptions m_replacePrefabOptions = UnityEditor.ReplacePrefabOptions.Default;

		public UnityEditor.ReplacePrefabOptions Options {
			get {
				return m_replacePrefabOptions;
			}
		}

		public MultiTargetSerializedInstance<IPrefabBuilder> Builder {
			get {
				return m_instance;
			}
		}

		public string ActiveStyle {
			get {
				return "flow node 4 on";
			}
		}

		public string InactiveStyle {
			get {
				return "flow node 4";
			}
		}

		public Model.NodeOutputSemantics NodeInputType {
			get {
				return Model.NodeOutputSemantics.Assets;
			}
		}

		public Model.NodeOutputSemantics NodeOutputType {
			get {
				return Model.NodeOutputSemantics.Assets;
			}
		}

		public void Initialize(Model.NodeData data) {
			m_instance = new MultiTargetSerializedInstance<IPrefabBuilder>();

			data.AddInputPoint(Model.Settings.DEFAULT_INPUTPOINT_LABEL);
			data.AddOutputPoint(Model.Settings.DEFAULT_OUTPUTPOINT_LABEL);
		}

		public INode Clone() {
			var newNode = new PrefabBuilder();
			newNode.m_instance = new MultiTargetSerializedInstance<IPrefabBuilder>(m_instance);
			newNode.m_replacePrefabOptions = m_replacePrefabOptions;

			return newNode;
		}

		public bool IsEqual(INode node) {
			PrefabBuilder rhs = node as PrefabBuilder;
			return rhs != null && 
				m_instance == rhs.m_instance &&
				m_replacePrefabOptions == rhs.m_replacePrefabOptions;
		}

		public string Serialize() {
			return JsonUtility.ToJson(this);
		}

		public bool IsValidInputConnectionPoint(Model.ConnectionPointData point) {
			return true;
		}

		public bool OnAssetsReimported(BuildTarget target, 
			string[] importedAssets, 
			string[] deletedAssets, 
			string[] movedAssets, 
			string[] movedFromAssetPaths)
		{
			return false;
		}

		public void OnNodeGUI(NodeGUI node) {
		}

		public void OnInspectorGUI (NodeGUI node, NodeGUIEditor editor) {
			EditorGUILayout.HelpBox("PrefabBuilder: Create prefab with given assets and script.", MessageType.Info);
			editor.UpdateNodeName(node);

			var builder = m_instance[editor.CurrentEditingGroup];

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {

				var map = PrefabBuilderUtility.GetAttributeClassNameMap();
				if(map.Count > 0) {
					using(new GUILayout.HorizontalScope()) {
						GUILayout.Label("PrefabBuilder");
						var guiName = PrefabBuilderUtility.GetPrefabBuilderGUIName(builder.ClassName);

						if (GUILayout.Button(guiName, "Popup", GUILayout.MinWidth(150f))) {
							var builders = map.Keys.ToList();

							if(builders.Count > 0) {
								NodeGUI.ShowTypeNamesMenu(guiName, builders, (string selectedGUIName) => 
									{
										using(new RecordUndoScope("Change PrefabBuilder class", node, true)) {
											var prefabBuilder = PrefabBuilderUtility.CreatePrefabBuilder(selectedGUIName);
											if(prefabBuilder != null) {
												builder = new SerializedInstance<IPrefabBuilder>(prefabBuilder);
												m_instance[editor.CurrentEditingGroup] = builder;
											}
										}
									} 
								);
							}
						}

						MonoScript s = TypeUtility.LoadMonoScript(builder.ClassName);

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
						}
					}
				} else {
					if(!string.IsNullOrEmpty(builder.ClassName)) {
						EditorGUILayout.HelpBox(
							string.Format(
								"Your PrefabBuilder script {0} is missing from assembly. Did you delete script?", builder.ClassName), MessageType.Info);
					} else {
						string[] menuNames = Model.Settings.GUI_TEXT_MENU_GENERATE_PREFABBUILDER.Split('/');
						EditorGUILayout.HelpBox(
							string.Format(
								"You need to create at least one PrefabBuilder script to use PrefabBuilder node. To start, select {0}>{1}>{2} menu and create new script from template.",
								menuNames[1],menuNames[2], menuNames[3]
							), MessageType.Info);
					}
				}

				GUILayout.Space(10f);

				if(editor.DrawPlatformSelector(node)) {
					// if platform tab is changed, renew prefabBuilder for that tab.
					//m_prefabBuilder = null;
				}
				using (new EditorGUILayout.VerticalScope()) {
					var disabledScope = editor.DrawOverrideTargetToggle(node, m_instance.ContainsValueOf(editor.CurrentEditingGroup), (bool enabled) => {
						if(enabled) {
							m_instance[editor.CurrentEditingGroup] = m_instance.DefaultValue;
						} else {
							m_instance.Remove(editor.CurrentEditingGroup);
						}
						//m_prefabBuilder = null;
					});

					using (disabledScope) {
						//reload prefabBuilder instance from saved instance data.
//						if (m_prefabBuilder == null) {
//							m_prefabBuilder = PrefabBuilderUtility.CreatePrefabBuilder(node.Data, editor.CurrentEditingGroup);
//							if(m_prefabBuilder != null) {
//								m_className = m_prefabBuilder.GetType().FullName;
//								if(m_instanceData.ContainsValueOf(editor.CurrentEditingGroup)) {
//									m_instanceData[editor.CurrentEditingGroup] = m_prefabBuilder.Serialize();
//								}
//							}
//						}

						if (builder.Object != null) {
							Action onChangedAction = () => {
								using(new RecordUndoScope("Change PrefabBuilder Setting", node)) {
									builder.Save();
//									m_className = m_prefabBuilder.GetType().FullName;
//									if(m_instanceData.ContainsValueOf(editor.CurrentEditingGroup)) {
//										m_instanceData[editor.CurrentEditingGroup] = m_prefabBuilder.Serialize();
//									}
								}
							};

							builder.Object.OnInspectorGUI(onChangedAction);
						}
					}
				}
			}
		}

		public void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			ValidatePrefabBuilder(node, target, incoming,
				() => {
					throw new NodeException(node.Name + " :PrefabBuilder is not configured. Please configure from Inspector.", node.Id);
				},
				() => {
					throw new NodeException(node.Name + " :Failed to create PrefabBuilder from settings. Please fix settings from Inspector.", node.Id);
				},
				(string groupKey) => {
					throw new NodeException(string.Format("{0} :Can not create prefab with incoming assets for group {1}.", node.Name, groupKey), node.Id);
				},
				(AssetReference badAsset) => {
					throw new NodeException(string.Format("{0} :Can not import incoming asset {1}.", node.Name, badAsset.fileNameAndExtension), node.Id);
				}
			);

			if(incoming == null) {
				return;
			}

			var builder = m_instance[target];
			UnityEngine.Assertions.Assert.IsNotNull(builder.Object);


			var prefabOutputDir = FileUtility.EnsurePrefabBuilderCacheDirExists(target, node);
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
				var thresold = PrefabBuilderUtility.GetPrefabBuilderAssetThreshold(builder.ClassName);
				if( thresold < assets.Count ) {
					var guiName = PrefabBuilderUtility.GetPrefabBuilderGUIName(builder.ClassName);
					throw new NodeException(string.Format("{0} :Too many assets passed to {1} for group:{2}. {3}'s threshold is set to {4}", 
						node.Name, guiName, key, guiName,thresold), node.Id);
				}

				List<UnityEngine.Object> allAssets = LoadAllAssets(assets);
				var prefabFileName = builder.Object.CanCreatePrefab(key, allAssets);
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

		public void Build (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output,
			Action<Model.NodeData, string, float> progressFunc) 
		{
			if(incoming == null) {
				return;
			}

			var builder = m_instance[target];
			UnityEngine.Assertions.Assert.IsNotNull(builder.Object);

			var prefabOutputDir = FileUtility.EnsurePrefabBuilderCacheDirExists(target, node);
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

				var allAssets = LoadAllAssets(assets);

				var prefabFileName = builder.Object.CanCreatePrefab(key, allAssets);
				var prefabSavePath = FileUtility.PathCombine(prefabOutputDir, prefabFileName + ".prefab");

				if (!Directory.Exists(Path.GetDirectoryName(prefabSavePath))) {
					Directory.CreateDirectory(Path.GetDirectoryName(prefabSavePath));
				}

				if(PrefabBuildInfo.DoesPrefabNeedRebuilding(this, node, target, key, assets)) {
					UnityEngine.GameObject obj = builder.Object.CreatePrefab(key, allAssets);
					if(obj == null) {
						throw new AssetBundleGraphException(string.Format("{0} :PrefabBuilder {1} returned null in CreatePrefab() [groupKey:{2}]", 
							node.Name, builder.GetType().FullName, key));
					}

					LogUtility.Logger.LogFormat(LogType.Log, "{0} is (re)creating Prefab:{1} with {2}({3})", node.Name, prefabFileName,
						PrefabBuilderUtility.GetPrefabBuilderGUIName(builder.ClassName),
						PrefabBuilderUtility.GetPrefabBuilderVersion(builder.ClassName));

					if(progressFunc != null) progressFunc(node, string.Format("Creating {0}", prefabFileName), 0.5f);

					PrefabUtility.CreatePrefab(prefabSavePath, obj, m_replacePrefabOptions);
					PrefabBuildInfo.SavePrefabBuildInfo(this, node, target, key, assets);
					GameObject.DestroyImmediate(obj);
				}
				UnloadAllAssets(assets);

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

		public void ValidatePrefabBuilder (
			Model.NodeData node,
			BuildTarget target,
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			Action noBuilderData,
			Action failedToCreateBuilder,
			Action<string> canNotCreatePrefab,
			Action<AssetReference> canNotImportAsset
		) {
			if(!m_instance.ContainsValueOf(BuildTargetUtility.TargetToGroup(target))) {
				noBuilderData();
			}

			var builder = m_instance[target].Object;

			if(null == builder ) {
				failedToCreateBuilder();
			}

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
								// do not call LoadAllAssets() unless all assets have importFrom
								var al = ag.assetGroups[key];
								List<UnityEngine.Object> allAssets = LoadAllAssets(al);
								if(string.IsNullOrEmpty(builder.CanCreatePrefab(key, allAssets))) {
									canNotCreatePrefab(key);
								}
								UnloadAllAssets(al);
							}
						}
					}
				}
			}
		}			
	}
}