using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	
	/**
		ImportSetting is the class for apply specific setting to already imported files.
	*/
	[CustomNode("Import Setting", 30)]
	public class ImportSetting : Node {

		public override string ActiveStyle {
			get {
				return "flow node 2 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "flow node 2";
			}
		}

		public override void Initialize(Model.NodeData data) {
			data.AddInputPoint(Model.Settings.DEFAULT_INPUTPOINT_LABEL);
			data.AddOutputPoint(Model.Settings.DEFAULT_OUTPUTPOINT_LABEL);
		}

		public override Node Clone() {
			var newNode = new ImportSetting();

			return newNode;
		}

		public override bool IsEqual(Node node) {
			ImportSetting rhs = node as ImportSetting;
			return rhs != null;
		}

		public override string Serialize() {
			return JsonUtility.ToJson(this);
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
			var samplingDirectoryPath = FileUtility.PathCombine(Model.Settings.IMPORTER_SETTINGS_PLACE, nodeData.Id);

			foreach(var imported in importedAssets) {
				if(imported.StartsWith(samplingDirectoryPath)) {
					return true;
				}
			}

			return false;
		}

		public override void OnInspectorGUI(NodeGUI node, NodeGUIEditor editor, Action onValueChanged) {

			EditorGUILayout.HelpBox("ImportSetting: Force apply import settings to given assets.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

			/*
				importer node has no platform key. 
				platform key is contained by Unity's importer inspector itself.
			*/
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				Type incomingType = TypeUtility.FindFirstIncomingAssetType(node.Data.InputPoints[0]);
				ImportSetting.ConfigStatus status = 
					ImportSetting.GetConfigStatus(node.Data);

				if(incomingType == null) {
					// try to retrieve incoming type from configuration
					if(status == ImportSetting.ConfigStatus.GoodSampleFound) {
						incomingType = ImportSetting.GetReferenceAssetImporter(node.Data).GetType();
					} else {
						EditorGUILayout.HelpBox("ImportSetting needs a single type of incoming assets.", MessageType.Info);
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
					if (GUILayout.Button("Configure Import Setting")) {
						Selection.activeObject = ImportSetting.GetReferenceAssetImporter(node.Data);
					}
					if (GUILayout.Button("Reset Import Setting")) {
						ImportSetting.ResetConfig(node.Data);
					}
					break;
				case ImportSetting.ConfigStatus.TooManySamplesFound:
					if (GUILayout.Button("Reset Import Setting")) {
						ImportSetting.ResetConfig(node.Data);
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

			Action<ConfigStatus> errorInConfig = (ConfigStatus _) => {

				var firstAsset = TypeUtility.GetFirstIncomingAsset(incoming);

				if(firstAsset != null) {
					// give a try first in sampling file
					SaveSampleFile(node, firstAsset);

					ValidateInputSetting(node, target, incoming, multipleAssetTypeFound, unsupportedType, incomingTypeMismatch, (ConfigStatus eType) => {
						if(eType == ConfigStatus.NoSampleFound) {
							throw new NodeException(node.Name + " :ImportSetting has no sampling file. Please configure it from Inspector.", node.Id);
						}
						if(eType == ConfigStatus.TooManySamplesFound) {
							throw new NodeException(node.Name + " :ImportSetting has too many sampling file. Please fix it from Inspector.", node.Id);
						}
					});
				}
			};

			ValidateInputSetting(node, target, incoming, multipleAssetTypeFound, unsupportedType, incomingTypeMismatch, errorInConfig);

			if(incoming != null){
				ApplyImportSetting(node, incoming);
			}

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
		
		private void SaveSampleFile(Model.NodeData node, AssetReference asset) {
			var samplingDirectoryPath = FileUtility.PathCombine(Model.Settings.IMPORTER_SETTINGS_PLACE, node.Id);
			if (!Directory.Exists(samplingDirectoryPath)) {
				Directory.CreateDirectory(samplingDirectoryPath);
			}

			var configFilePath = FileUtility.GetImportSettingTemplateFilePath(asset);
			UnityEngine.Assertions.Assert.IsNotNull(configFilePath);
			var targetFilePath = FileUtility.PathCombine(samplingDirectoryPath, Path.GetFileName(configFilePath));

			FileUtility.CopyFile(configFilePath, targetFilePath);

			AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
		}

		public static ConfigStatus GetConfigStatus(Model.NodeData node) {
			var sampleFileDir = FileUtility.PathCombine(Model.Settings.IMPORTER_SETTINGS_PLACE, node.Id);

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

		public static void ResetConfig(Model.NodeData node) {
			var sampleFileDir = FileUtility.PathCombine(Model.Settings.IMPORTER_SETTINGS_PLACE, node.Id);
			FileUtility.RemakeDirectory(sampleFileDir);
		}

		public static AssetImporter GetReferenceAssetImporter(Model.NodeData node) {
			var sampleFileDir = FileUtility.PathCombine(Model.Settings.IMPORTER_SETTINGS_PLACE, node.Id);

			UnityEngine.Assertions.Assert.IsTrue(Directory.Exists(sampleFileDir));

			var sampleFiles = FileUtility.GetFilePathsInFolder(sampleFileDir)
				.Where(path => !path.EndsWith(Model.Settings.UNITY_METAFILE_EXTENSION))
				.ToList();

			UnityEngine.Assertions.Assert.IsTrue(sampleFiles.Count == 1);

			return AssetImporter.GetAtPath(sampleFiles[0]);	
		}

		private void ApplyImportSetting(Model.NodeData node, IEnumerable<PerformGraph.AssetGroups> incoming) {

			var referenceImporter = GetReferenceAssetImporter(node);	
			var configurator = new ImportSettingsConfigurator(referenceImporter);

			foreach(var ag in incoming) {
				foreach(var assets in ag.assetGroups.Values) {
					foreach(var asset in assets) {
						var importer = AssetImporter.GetAtPath(asset.importFrom);
						if(!configurator.IsEqual(importer)) {
							configurator.OverwriteImportSettings(importer);
							importer.SaveAndReimport();
							asset.TouchImportAsset();
						}
					}
				}
			}


		}

		public enum ConfigStatus {
			NoSampleFound,
			TooManySamplesFound,
			GoodSampleFound
		}

		public static void ValidateInputSetting (
			Model.NodeData node,
			BuildTarget target,
			IEnumerable<PerformGraph.AssetGroups> incoming,
			Action<Type, Type, AssetReference> multipleAssetTypeFound,
			Action<Type> unsupportedType,
			Action<Type, Type> incomingTypeMismatch,
			Action<ConfigStatus> errorInConfig
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
					if(expectedType == typeof(UnityEditor.TextureImporter) 	||
						expectedType == typeof(UnityEditor.ModelImporter) 	||
						expectedType == typeof(UnityEditor.AudioImporter) 
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
					Type targetType = GetReferenceAssetImporter(node).GetType();
					if( targetType != expectedType ) {
						incomingTypeMismatch(targetType, expectedType);
					}
				}
			}
		}
	}
}
