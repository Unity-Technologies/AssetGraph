using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AssetBundleGraph {
	
	/**
		IntegratedGUIImportSetting is the class for apply specific setting to already imported files.
	*/
	public class IntegratedGUIImportSetting : INodeOperation {
		
		public void Setup (BuildTarget target, 
			NodeData node, 
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUIImportSetting.Setup");
			var incomingAssets = inputGroupAssets.SelectMany(v => v.Value).ToList();

			Action<Type, Type, AssetReference> multipleAssetTypeFound = (Type expectedType, Type foundType, AssetReference foundAsset) => {
				throw new NodeException(string.Format("{3} :ImportSetting expect {0}, but different type of incoming asset is found({1} {2})", 
					expectedType.FullName, foundType.FullName, foundAsset.fileNameAndExtension, node.Name), node.Id);
			};

			Action<Type> unsupportedType = (Type unsupported) => {
				throw new NodeException(string.Format("{0} :Incoming asset type is not supported by ImportSetting (Incoming type:{1}). Perhaps you want to use Modifier instead?",
					node.Name, (unsupported != null)?unsupported.FullName:"null"), node.Id);
			};

			Action<Type, Type> incomingTypeMismatch = (Type expected, Type incoming) => {
				throw new NodeException(string.Format("{0} :Incoming asset type is does not match with this ImportSetting (Expected type:{1}, Incoming type:{2}).",
					node.Name, (expected != null)?expected.FullName:"null", (incoming != null)?incoming.FullName:"null"), node.Id);
			};

			Action<ConfigStatus> errorInConfig = (ConfigStatus _) => {
				// give a try first in sampling file
				if(incomingAssets.Any()) {
					SaveSampleFile(node, incomingAssets[0]);

					ValidateInputSetting(node, target, incomingAssets, multipleAssetTypeFound, unsupportedType, incomingTypeMismatch, (ConfigStatus eType) => {
						if(eType == ConfigStatus.NoSampleFound) {
							throw new NodeException(node.Name + " :ImportSetting has no sampling file. Please configure it from Inspector.", node.Id);
						}
						if(eType == ConfigStatus.TooManySamplesFound) {
							throw new NodeException(node.Name + " :ImportSetting has too many sampling file. Please fix it from Inspector.", node.Id);
						}
					});
				}
			};

			ValidateInputSetting(node, target, incomingAssets, multipleAssetTypeFound, unsupportedType, incomingTypeMismatch, errorInConfig);

			// ImportSettings does not add, filter or change structure of group, so just pass given group of assets
			Output(inputGroupAssets);
			Profiler.EndSample();
		}
		
		public void Run (BuildTarget target, 
			NodeData node, 
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUIImportSetting.Run");
			var incomingAssets = inputGroupAssets.SelectMany(v => v.Value).ToList();

			ApplyImportSetting(node, incomingAssets);

			Output(inputGroupAssets);
			Profiler.EndSample();
		}

		private void SaveSampleFile(NodeData node, AssetReference asset) {
			var samplingDirectoryPath = FileUtility.PathCombine(AssetBundleGraphSettings.IMPORTER_SETTINGS_PLACE, node.Id);
			if (!Directory.Exists(samplingDirectoryPath)) {
				Directory.CreateDirectory(samplingDirectoryPath);
			}

			var configFilePath = FileUtility.GetImportSettingTemplateFilePath(asset);
			UnityEngine.Assertions.Assert.IsNotNull(configFilePath);
			var targetFilePath = FileUtility.PathCombine(samplingDirectoryPath, Path.GetFileName(configFilePath));

			FileUtility.CopyFile(configFilePath, targetFilePath);

			AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
		}

		public static ConfigStatus GetConfigStatus(NodeData node) {
			var sampleFileDir = FileUtility.PathCombine(AssetBundleGraphSettings.IMPORTER_SETTINGS_PLACE, node.Id);

			if(!Directory.Exists(sampleFileDir)) {
				return ConfigStatus.NoSampleFound;
			}

			var sampleFiles = FileUtility.GetFilePathsInFolder(sampleFileDir)
				.Where(path => !path.EndsWith(AssetBundleGraphSettings.UNITY_METAFILE_EXTENSION))
				.ToList();

			if(sampleFiles.Count == 0) {
				return ConfigStatus.NoSampleFound;
			}
			if(sampleFiles.Count == 1) {
				return ConfigStatus.GoodSampleFound;
			}

			return ConfigStatus.TooManySamplesFound;
		}

		public static void ResetConfig(NodeData node) {
			var sampleFileDir = FileUtility.PathCombine(AssetBundleGraphSettings.IMPORTER_SETTINGS_PLACE, node.Id);
			FileUtility.RemakeDirectory(sampleFileDir);
		}

		public static AssetImporter GetReferenceAssetImporter(NodeData node) {
			var sampleFileDir = FileUtility.PathCombine(AssetBundleGraphSettings.IMPORTER_SETTINGS_PLACE, node.Id);

			UnityEngine.Assertions.Assert.IsTrue(Directory.Exists(sampleFileDir));

			var sampleFiles = FileUtility.GetFilePathsInFolder(sampleFileDir)
				.Where(path => !path.EndsWith(AssetBundleGraphSettings.UNITY_METAFILE_EXTENSION))
				.ToList();

			UnityEngine.Assertions.Assert.IsTrue(sampleFiles.Count == 1);

			return AssetImporter.GetAtPath(sampleFiles[0]);	
		}

		private void ApplyImportSetting(NodeData node, List<AssetReference> assets) {

			if(!assets.Any()) {
				return;
			}

			var referenceImporter = GetReferenceAssetImporter(node);	
			var configurator = new ImportSettingsConfigurator(referenceImporter);

			foreach(var asset in assets) {
				var importer = AssetImporter.GetAtPath(asset.importFrom);
				if(!configurator.IsEqual(importer)) {
					configurator.OverwriteImportSettings(importer);
				}
			}
		}

		public enum ConfigStatus {
			NoSampleFound,
			TooManySamplesFound,
			GoodSampleFound
		}

		public static void ValidateInputSetting (
			NodeData node,
			BuildTarget target,
			List<AssetReference> incomingAssets,
			Action<Type, Type, AssetReference> multipleAssetTypeFound,
			Action<Type> unsupportedType,
			Action<Type, Type> incomingTypeMismatch,
			Action<ConfigStatus> errorInConfig
		) {
			Type expectedType = TypeUtility.FindFirstIncomingAssetType(incomingAssets);
			if(multipleAssetTypeFound != null) {
				if(expectedType != null) {
					foreach(var a  in incomingAssets) {
						Type assetType = a.filterType;
						if(assetType != expectedType) {
							multipleAssetTypeFound(expectedType, assetType, a);
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
				if(incomingAssets.Any() && status == ConfigStatus.GoodSampleFound) {
					Type targetType = GetReferenceAssetImporter(node).GetType();
					if( targetType != expectedType ) {
						incomingTypeMismatch(targetType, expectedType);
					}
				}
			}
		}
	}
}
