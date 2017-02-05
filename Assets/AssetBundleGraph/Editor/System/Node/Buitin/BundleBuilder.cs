using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	[CustomNode("Bundle Builder", 70)]
	public class BundleBuilder : INode {

		private static readonly string key = "0";

		[SerializeField] private SerializableMultiTargetInt m_enabledBundleOptions;

		public string ActiveStyle {
			get {
				return "flow node 6 on";
			}
		}

		public string InactiveStyle {
			get {
				return "flow node 6";
			}
		}

		public Model.NodeOutputSemantics NodeInputType {
			get {
				return Model.NodeOutputSemantics.AssetBundleConfigurations;
			}
		}

		public Model.NodeOutputSemantics NodeOutputType {
			get {
				return Model.NodeOutputSemantics.AssetBundles;
			}
		}

		public void Initialize(Model.NodeData data) {
			m_enabledBundleOptions = new SerializableMultiTargetInt();

			data.AddInputPoint(Model.Settings.DEFAULT_INPUTPOINT_LABEL);
			data.AddOutputPoint(Model.Settings.DEFAULT_OUTPUTPOINT_LABEL);
		}

		public INode Clone() {
			var newNode = new BundleBuilder();
			newNode.m_enabledBundleOptions = new SerializableMultiTargetInt(m_enabledBundleOptions);

			return newNode;
		}

		public bool IsEqual(INode node) {
			BundleBuilder rhs = node as BundleBuilder;
			return rhs != null && 
				m_enabledBundleOptions == rhs.m_enabledBundleOptions;
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

		public void OnInspectorGUI (NodeGUI node, NodeGUIEditor editor) {
			if (m_enabledBundleOptions == null) {
				return;
			}

			EditorGUILayout.HelpBox("BundleBuilder: Build asset bundles with given asset bundle settings.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			editor.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = editor.DrawOverrideTargetToggle(node, m_enabledBundleOptions.ContainsValueOf(editor.CurrentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Bundle Options", node, true)){
						if(enabled) {
							m_enabledBundleOptions[editor.CurrentEditingGroup] = m_enabledBundleOptions.DefaultValue;
						}  else {
							m_enabledBundleOptions.Remove(editor.CurrentEditingGroup);
						}
					}
				} );

				using (disabledScope) {
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

		public void OnNodeGUI(NodeGUI node) {
		}

		public void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			// BundleBuilder do nothing without incoming connections
			if(incoming == null) {
				return;
			}

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
			var manifestName = BuildTargetUtility.TargetToAssetBundlePlatformName(target);
			bundleNames.Add( manifestName );
			bundleVariants[manifestName] = new List<string>() {""};

			if(connectionsToOutput != null && Output != null) {
				UnityEngine.Assertions.Assert.IsTrue(connectionsToOutput.Any());

				var outputDict = new Dictionary<string, List<AssetReference>>();
				outputDict[key] = new List<AssetReference>();
				var bundleOutputDir = FileUtility.EnsureAssetBundleCacheDirExists(target, node);

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

			var bundleOutputDir = FileUtility.EnsureAssetBundleCacheDirExists(target, node);

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
					++bbIndex;
				}
			}

			if(progressFunc != null) progressFunc(node, "Building Asset Bundles...", 0.7f);

			AssetBundleManifest m = BuildPipeline.BuildAssetBundles(bundleOutputDir, bundleBuild, (BuildAssetBundleOptions)m_enabledBundleOptions[target], target);

			var output = new Dictionary<string, List<AssetReference>>();
			output[key] = new List<AssetReference>();

			var generatedFiles = FileUtility.GetAllFilePathsInFolder(bundleOutputDir);
			// add manifest file
			bundleVariants.Add( BuildTargetUtility.TargetToAssetBundlePlatformName(target).ToLower(), new List<string> { null } );
			foreach (var path in generatedFiles) {
				var fileName = path.Substring(bundleOutputDir.Length+1);
				if( IsFileIntendedItem(fileName, bundleVariants) ) {
					output[key].Add( AssetReferenceDatabase.GetAssetBundleReference(path) );
				}
			}

			if(Output != null) {
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();
				Output(dst, output);
			}

			AssetBundleBuildReport.AddBuildReport(new AssetBundleBuildReport(node, m, bundleBuild, output[key], aggregatedGroups, bundleVariants));
		}

		// Check if given file is generated Item
		private bool IsFileIntendedItem(string filename, Dictionary<string, List<string>> bundleVariants) {
			filename = filename.ToLower();

			int lastDotManifestIndex = filename.LastIndexOf(".manifest");
			filename = (lastDotManifestIndex > 0)? filename.Substring(0, lastDotManifestIndex) : filename;

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