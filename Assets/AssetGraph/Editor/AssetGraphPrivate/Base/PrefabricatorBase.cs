using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AssetGraph {
	public class PrefabricatorBase : INodeBase {
		public void Setup (string nodeId, string labelToNext, List<AssetData> inputSources, Action<string, string, List<AssetData>> Output) {
			Output(nodeId, labelToNext, inputSources);
		}

		public void Run (string nodeId, string labelToNext, List<AssetData> inputSources, Action<string, string, List<AssetData>> Output) {
			var assets = new List<Asset>();
			foreach (var assetData in inputSources) {
				var assetName = assetData.fileNameAndExtension;
				var assetType = assetData.assetType;
				var assetPath = assetData.importedPath;
				var assetId = assetData.assetId;
				assets.Add(new Asset(assetName, assetType, assetPath, assetId));
			}

			var recommendedPrefabOutputDir = Path.Combine(AssetGraphSettings.PREFABRICATOR_TEMP_PLACE, nodeId);
			FileController.RemakeDirectory(recommendedPrefabOutputDir);

			/*
				files under "Assets/" before prefabricate.
			*/
			var localFilePathsBeforePrefabricate = FileController.FilePathsInFolderWithoutMeta("Assets");
			
			/*
				execute inheritee's input method.
			*/
			In(assets, recommendedPrefabOutputDir);

			AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);

			/*
				collect outputs.
			*/
			var outputSources = new List<AssetData>();

			foreach (var assetData in inputSources) {
				var inheritedAssetData = AssetData.AssetDataByImporter(
					assetData.traceId,
					assetData.absoluteSourcePath,
					assetData.sourceBasePath,
					assetData.fileNameAndExtension,
					assetData.pathUnderSourceBase,
					assetData.importedPath,
					assetData.assetId,
					assetData.assetType
				);
				outputSources.Add(inheritedAssetData);
			}

			AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
			AssetDatabase.SaveAssets();

			/*
				files under "Assets/" after prefabricated.
			*/
			var localFilePathsAfterPrefabricate = FileController.FilePathsInFolderWithoutMeta("Assets");
			
			/*
				check if new Assets are generated, trace it.
			*/
			var assetPathsWhichAreNotTraced = localFilePathsAfterPrefabricate.Except(localFilePathsBeforePrefabricate);
			foreach (var newAssetPath in assetPathsWhichAreNotTraced) {
				var newAssetData = AssetData.AssetDataGeneratedByImporterOrPrefabricatorOrBundlizer(
					newAssetPath,
					AssetDatabase.AssetPathToGUID(newAssetPath),
					AssetGraphInternalFunctions.GetAssetType(newAssetPath)
				);
				outputSources.Add(newAssetData);
			}

			Output(nodeId, labelToNext, outputSources);
		}

		public virtual void In (List<Asset> source, string recommendedPrefabOutputDir) {
			Debug.LogError("should implement \"public override void In (List<Asset> source, string recommendedPrefabOutputDir)\" in class:" + this);
		}
	}
}