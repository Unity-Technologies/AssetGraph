using UnityEditor;
using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	[CustomNode("Bundle Configurator", 60)]
	public class BundleConfigurator : INode {

		[Serializable]
		public class Variant {
			[SerializeField] private string m_name;
			[SerializeField] private string m_pointId;

			public Variant(string name, Model.ConnectionPointData point) {
				m_name = name;
				m_pointId = point.Id;
			}
			public Variant(Variant v) {
				m_name = v.m_name;
				m_pointId = v.m_pointId;
			}

			public string Name {
				get {
					return m_name;
				}
				set {
					m_name = value;
				}
			}
			public string ConnectionPointId {
				get {
					return m_pointId; 
				}
			}
		}

		[SerializeField] private SerializableMultiTargetString m_bundleNameTemplate;
		[SerializeField] private List<Variant> m_variants;
		[SerializeField] private bool m_useGroupAsVariants;

		public string ActiveStyle {
			get {
				return "flow node 5 on";
			}
		}

		public string InactiveStyle {
			get {
				return "flow node 5";
			}
		}

		public Model.NodeOutputSemantics NodeInputType {
			get {
				return Model.NodeOutputSemantics.Assets;
			}
		}

		public Model.NodeOutputSemantics NodeOutputType {
			get {
				return Model.NodeOutputSemantics.AssetBundleConfigurations;
			}
		}

		public void Initialize(Model.NodeData data) {
			m_bundleNameTemplate = new SerializableMultiTargetString(Model.Settings.BUNDLECONFIG_BUNDLENAME_TEMPLATE_DEFAULT);
			m_useGroupAsVariants = false;
			m_variants = new List<Variant>();

			data.AddInputPoint(Model.Settings.DEFAULT_INPUTPOINT_LABEL);
			data.AddOutputPoint(Model.Settings.DEFAULT_OUTPUTPOINT_LABEL);
		}

		public INode Clone() {
			var newNode = new BundleConfigurator();
			newNode.m_bundleNameTemplate = new SerializableMultiTargetString(m_bundleNameTemplate);
			newNode.m_variants = new List<Variant>(m_variants.Count);
			m_variants.ForEach(v => newNode.m_variants.Add(new Variant(v)));
			newNode.m_useGroupAsVariants = m_useGroupAsVariants;

			//				foreach(var v in m_variants) {
			//					if(null == rhs.m_variants.Find(x => x.Name == v.Name && x.ConnectionPointId == v.ConnectionPointId)) {
			//						LogUtility.Logger.LogFormat(LogType.Log, "{0} and {1} was different: {2}", Name, rhs.Name, "Variants not found");
			//						return false;
			//					}
			//				}
			return newNode;
		}

		public bool IsEqual(INode node) {
			BundleConfigurator rhs = node as BundleConfigurator;
			return rhs != null && 
				m_bundleNameTemplate == rhs.m_bundleNameTemplate &&
				m_useGroupAsVariants == rhs.m_useGroupAsVariants &&
				m_variants.SequenceEqual(rhs.m_variants);
		}

		public string Serialize() {
			return JsonUtility.ToJson(this);
		}

		public bool IsValidInputConnectionPoint(Model.ConnectionPointData point) {
			if(!m_useGroupAsVariants) {
				if(m_variants.Count > 0 && m_variants.Find(v => v.ConnectionPointId == point.Id) == null) 
				{
					return false;
				}
			}
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

		private void AddVariant(Model.NodeData n, string name) {
			var p = n.AddInputPoint(name);
			var newEntry = new Variant(name, p);
			m_variants.Add(newEntry);
			UpdateVariant(n, newEntry);
		}

		private void RemoveVariant(Model.NodeData n, Variant v) {
			m_variants.Remove(v);
			n.InputPoints.Remove(GetConnectionPoint(n, v));
		}

		private Model.ConnectionPointData GetConnectionPoint(Model.NodeData n, Variant v) {
			Model.ConnectionPointData p = n.InputPoints.Find(point => point.Id == v.ConnectionPointId);
			UnityEngine.Assertions.Assert.IsNotNull(p);
			return p;
		}

		private void UpdateVariant(Model.NodeData n,Variant variant) {

			Model.ConnectionPointData p = n.InputPoints.Find(v => v.Id == variant.ConnectionPointId);
			UnityEngine.Assertions.Assert.IsNotNull(p);

			p.Label = variant.Name;
		}



		public void OnNodeGUI(NodeGUI node) {
		}
			
		public void OnInspectorGUI (NodeGUI node, NodeGUIEditor editor) {
			if (m_bundleNameTemplate == null) return;

			EditorGUILayout.HelpBox("BundleConfigurator: Create asset bundle settings with given group of assets.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {

				var newUseGroupAsVariantValue = GUILayout.Toggle(m_useGroupAsVariants, "Use input group as variants");
				if(newUseGroupAsVariantValue != m_useGroupAsVariants) {
					using(new RecordUndoScope("Change Bundle Config", node, true)){
						m_useGroupAsVariants = newUseGroupAsVariantValue;

						List<Variant> rv = new List<Variant>(m_variants);
						foreach(var v in rv) {
							NodeGUIUtility.NodeEventHandler(
								new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED, node, Vector2.zero, GetConnectionPoint(node.Data, v)));
							RemoveVariant(node.Data, v);
						}
					}
				}

				using (new EditorGUI.DisabledScope(newUseGroupAsVariantValue)) {
					GUILayout.Label("Variants:");
					var variantNames = m_variants.Select(v => v.Name).ToList();
					Variant removing = null;
					foreach (var v in m_variants) {
						using (new GUILayout.HorizontalScope()) {
							if (GUILayout.Button("-", GUILayout.Width(30))) {
								removing = v;
							}
							else {
								GUIStyle s = new GUIStyle((GUIStyle)"TextFieldDropDownText");
								Action makeStyleBold = () => {
									s.fontStyle = FontStyle.Bold;
									s.fontSize = 12;
								};

								ValidateVariantName(v.Name, variantNames, 
									makeStyleBold,
									makeStyleBold,
									makeStyleBold);

								var variantName = EditorGUILayout.TextField(v.Name, s);

								if (variantName != v.Name) {
									using(new RecordUndoScope("Change Variant Name", node, true)){
										v.Name = variantName;
										UpdateVariant(node.Data, v);
									}
								}
							}
						}
					}
					if (GUILayout.Button("+")) {
						using(new RecordUndoScope("Add Variant", node, true)){
							if(m_variants.Count == 0) {
								NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_DELETE_ALL_CONNECTIONS_TO_POINT, node, Vector2.zero, node.Data.InputPoints[0]));
							}
							AddVariant(node.Data, Model.Settings.BUNDLECONFIG_VARIANTNAME_DEFAULT);
						}
					}
					if(removing != null) {
						using(new RecordUndoScope("Remove Variant", node, true)){
							// event must raise to remove connection associated with point
							NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED, node, Vector2.zero, GetConnectionPoint(node.Data, removing)));
							RemoveVariant(node.Data, removing);
						}
					}
				}
			}

			//Show target configuration tab
			editor.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = editor.DrawOverrideTargetToggle(node, m_bundleNameTemplate.ContainsValueOf(editor.CurrentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Bundle Name Template Setting", node, true)){
						if(enabled) {
							m_bundleNameTemplate[editor.CurrentEditingGroup] = m_bundleNameTemplate.DefaultValue;
						} else {
							m_bundleNameTemplate.Remove(editor.CurrentEditingGroup);
						}
					}
				});

				using (disabledScope) {
					var bundleNameTemplate = EditorGUILayout.TextField("Bundle Name Template", m_bundleNameTemplate[editor.CurrentEditingGroup]).ToLower();

					if (bundleNameTemplate != m_bundleNameTemplate[editor.CurrentEditingGroup]) {
						using(new RecordUndoScope("Change Bundle Name Template", node, true)){
							m_bundleNameTemplate[editor.CurrentEditingGroup] = bundleNameTemplate;
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
			int groupCount = 0;

			if(incoming != null) {
				var groupNames = new List<string>();
				foreach(var ag in incoming) {
					foreach (var groupKey in ag.assetGroups.Keys) {
						if(!groupNames.Contains(groupKey)) {
							groupNames.Add(groupKey);
						}
					}
				}
				groupCount = groupNames.Count;
			}

			ValidateBundleNameTemplate(
				m_bundleNameTemplate[target],
				m_useGroupAsVariants,
				groupCount,
				() => {
					throw new NodeException(node.Name + ":Bundle Name Template is empty.", node.Id);
				},
				() => {
					throw new NodeException(node.Name + ":Bundle Name Template can not contain '" + Model.Settings.KEYWORD_WILDCARD.ToString() 
						+ "' when group name is used for variants.", node.Id);
				},
				() => {
					throw new NodeException(node.Name + ":Bundle Name Template must contain '" + Model.Settings.KEYWORD_WILDCARD.ToString() 
						+ "' when group name is not used for variants and expecting multiple incoming groups.", node.Id);
				}
			);

			var variantNames = m_variants.Select(v=>v.Name).ToList();
			foreach(var variant in m_variants) {
				ValidateVariantName(variant.Name, variantNames, 
					() => {
						throw new NodeException(node.Name + ":Variant name is empty.", node.Id);
					},
					() => {
						throw new NodeException(node.Name + ":Variant name cannot contain whitespace \"" + variant.Name + "\".", node.Id);
					},
					() => {
						throw new NodeException(node.Name + ":Variant name already exists \"" + variant.Name + "\".", node.Id);
					});
			}


			if(incoming != null) {
				/**
				 * Check if incoming asset has valid import path
				 */
				var invalids = new List<AssetReference>();
				foreach(var ag in incoming) {
					foreach (var groupKey in ag.assetGroups.Keys) {
						ag.assetGroups[groupKey].ForEach( a => { if (string.IsNullOrEmpty(a.importFrom)) invalids.Add(a); } );
					}
				}
				if (invalids.Any()) {
					throw new NodeException(node.Name + 
						": Invalid files are found. Following files need to be imported to put into asset bundle: " + 
						string.Join(", ", invalids.Select(a =>a.absolutePath).ToArray()), node.Id );
				}
			}

			Dictionary<string, List<AssetReference>> output = null;
			if(Output != null) {
				output = new Dictionary<string, List<AssetReference>>();
			}

			if(incoming != null) {
				foreach(var ag in incoming) {
					string variantName = null;
					if(!m_useGroupAsVariants) {
						var currentVariant = m_variants.Find( v => v.ConnectionPointId == ag.connection.ToNodeConnectionPointId );
						variantName = (currentVariant == null) ? null : currentVariant.Name;
					}

					// set configured assets in bundle name
					foreach (var groupKey in ag.assetGroups.Keys) {
						if(m_useGroupAsVariants) {
							variantName = groupKey;
						}
						var bundleName = GetBundleName(target, node, groupKey);
						var assets = ag.assetGroups[groupKey];
						ConfigureAssetBundleSettings(variantName, assets);
						if(output != null) {
							if(!output.ContainsKey(bundleName)) {
								output[bundleName] = new List<AssetReference>();
							} 
							output[bundleName].AddRange(assets);
						}
					}
				}
			}

			if(Output != null) {
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();
				Output(dst, output);
			}
		}
		
		public void Build (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output,
			Action<Model.NodeData, string, float> progressFunc) 
		{
			Dictionary<string, List<AssetReference>> output = null;
			if(Output != null) {
				output = new Dictionary<string, List<AssetReference>>();
			}

			if(incoming != null) {
				foreach(var ag in incoming) {
					string variantName = null;
					if(!m_useGroupAsVariants) {
						var currentVariant = m_variants.Find( v => v.ConnectionPointId == ag.connection.ToNodeConnectionPointId );
						variantName = (currentVariant == null) ? null : currentVariant.Name;
					}

					// set configured assets in bundle name
					foreach (var groupKey in ag.assetGroups.Keys) {
						if(m_useGroupAsVariants) {
							variantName = groupKey;
						}
						var bundleName = GetBundleName(target, node, groupKey);

						if(progressFunc != null) progressFunc(node, string.Format("Configuring {0}", bundleName), 0.5f);

						var assets = ag.assetGroups[groupKey];
						ConfigureAssetBundleSettings(variantName, assets);
						if(output != null) {
							if(!output.ContainsKey(bundleName)) {
								output[bundleName] = new List<AssetReference>();
							} 
							output[bundleName].AddRange(assets);
						}
					}
				}
			}

			if(Output != null) {
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();
				Output(dst, output);
			}
		}

		public void ConfigureAssetBundleSettings (string variantName, List<AssetReference> assets) {		

			foreach(var a in assets) {
				a.variantName = (string.IsNullOrEmpty(variantName))? null : variantName.ToLower();;
			}
		}

		public static void ValidateBundleNameTemplate (string bundleNameTemplate, bool useGroupAsVariants, int groupCount,
			Action NullOrEmpty, 
			Action InvalidBundleNameTemplateForVariants, 
			Action InvalidBundleNameTemplateForNotVariants
		) {
			if (string.IsNullOrEmpty(bundleNameTemplate)){
				NullOrEmpty();
			}
			if(useGroupAsVariants && bundleNameTemplate.IndexOf(Model.Settings.KEYWORD_WILDCARD) >= 0) {
				InvalidBundleNameTemplateForVariants();
			}
			if(!useGroupAsVariants && bundleNameTemplate.IndexOf(Model.Settings.KEYWORD_WILDCARD) < 0 &&
				groupCount > 1) {
				InvalidBundleNameTemplateForNotVariants();
			}
		}

		public static void ValidateVariantName (string variantName, List<string> names, Action NullOrEmpty, Action ContainsSpace, Action NameAlreadyExists) {
			if (string.IsNullOrEmpty(variantName)) {
				NullOrEmpty();
			}
			if(Regex.IsMatch(variantName, "\\s")) {
				ContainsSpace();
			}
			var overlappings = names.GroupBy(x => x)
				.Where(group => 1 < group.Count())
				.Select(group => group.Key)
				.ToList();

			if (overlappings.Any()) {
				NameAlreadyExists();
			}
		}

		public string GetBundleName(BuildTarget target, Model.NodeData node, string groupKey) {
			var bundleName = m_bundleNameTemplate[target];

			if(m_useGroupAsVariants) {
				return bundleName;
			} else {
				return bundleName.Replace(Model.Settings.KEYWORD_WILDCARD.ToString(), groupKey);
			}
		}
	}
}