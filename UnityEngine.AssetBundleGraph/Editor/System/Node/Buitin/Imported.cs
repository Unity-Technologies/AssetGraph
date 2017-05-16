using UnityEngine;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	[CustomNode("Load Assets/Last Imported Items", 19)]
	public class Imported : Node {

		private List<string> m_lastImportedAssetPaths;

		public override string ActiveStyle {
			get {
				return "node 0 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 0";
			}
		}

		public override string Category {
			get {
				return "Loader";
			}
		}
			
		public override Model.NodeOutputSemantics NodeInputType {
			get {
				return Model.NodeOutputSemantics.None;
			}
		}

		public override void Initialize(Model.NodeData data) {
			data.AddDefaultOutputPoint();
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new Imported();

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
			if (m_lastImportedAssetPaths == null) {
				m_lastImportedAssetPaths = new List<string> ();
			}
		
            var imported = importedAssets.Where (path => !TypeUtility.IsGraphToolSystemAsset (path));
            var moved = movedAssets.Where (path => !TypeUtility.IsGraphToolSystemAsset (path));

			if (imported.Any () || moved.Any ()) {
				m_lastImportedAssetPaths.Clear ();
				m_lastImportedAssetPaths.AddRange (imported);
				m_lastImportedAssetPaths.AddRange (moved);
			}

//			var assetsFolderPath = Application.dataPath + Model.Settings.UNITY_FOLDER_SEPARATOR;
//
//			foreach (var path in importedAssets) {
//				if (path.StartsWith (assetsFolderPath)) {
//					m_lastImportedAssetPaths.Add( path.Replace (assetsFolderPath, Model.Settings.ASSETS_PATH) );
//				}
//			}
//
//			foreach (var path in movedAssets) {
//				if (path.StartsWith (assetsFolderPath)) {
//					m_lastImportedAssetPaths.Add( path.Replace (assetsFolderPath, Model.Settings.ASSETS_PATH) );
//				}
//			}

			return true;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			EditorGUILayout.HelpBox("Last Imported Items: Load assets just imported.", MessageType.Info);
			editor.UpdateNodeName(node);

		}


		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			if (m_lastImportedAssetPaths != null) {
				m_lastImportedAssetPaths.RemoveAll (path => !File.Exists (path));
			}

			Load(target, node, connectionsToOutput, Output);
		}
		
		void Load (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			if(connectionsToOutput == null || Output == null) {
				return;
			}
			var outputSource = new List<AssetReference>();

			if (m_lastImportedAssetPaths != null) {
				foreach (var path in m_lastImportedAssetPaths) {
                    if (TypeUtility.IsGraphToolSystemAsset (path)) {
                        continue;
                    }

					var r = AssetReferenceDatabase.GetReference(path);

					if(!TypeUtility.IsLoadingAsset(r)) {
						continue;
					}

					if(r != null) {
						outputSource.Add(r);
					}
				}
			}


			var output = new Dictionary<string, List<AssetReference>> {
				{"0", outputSource}
			};

			var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();
			Output(dst, output);
		}
	}
}