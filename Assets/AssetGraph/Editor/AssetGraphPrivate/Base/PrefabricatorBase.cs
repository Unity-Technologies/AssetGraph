using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AssetGraph {
	public class PrefabricatorBase : INodeBase {
		public void Setup (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>> Output) {
			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				outputDict[groupKey] = inputSources;
			};

			Output(nodeId, labelToNext, outputDict);
		}

		public void Run (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>> Output) {
			var recommendedPrefabOutputDir = Path.Combine(AssetGraphSettings.PREFABRICATOR_TEMP_PLACE, nodeId);
			FileController.RemakeDirectory(recommendedPrefabOutputDir);

			var outputDict = new Dictionary<string, List<InternalAssetData>>();
			
			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				var assets = new List<AssetInfo>();
				foreach (var assetData in inputSources) {
					var assetName = assetData.fileNameAndExtension;
					var assetType = assetData.assetType;
					var assetPath = assetData.importedPath;
					var assetId = assetData.assetId;
					assets.Add(new AssetInfo(assetName, assetType, assetPath, assetId));
				}

				/*
					files under "Assets/" before prefabricate.
				*/
				var localFilePathsBeforePrefabricate = FileController.FilePathsInFolderWithoutMeta("Assets");
				
				/*
					execute inheritee's input method.
				*/
				try {
					In(groupKey, assets, recommendedPrefabOutputDir);
				} catch (Exception e) {
					Debug.LogError("Prefabricator:" + this + " error:" + e);
				}

				AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);

				/*
					collect outputs.
				*/
				var outputSources = new List<InternalAssetData>();

				foreach (var assetData in inputSources) {
					var inheritedInternalAssetData = InternalAssetData.InternalAssetDataByImporter(
						assetData.traceId,
						assetData.absoluteSourcePath,
						assetData.sourceBasePath,
						assetData.fileNameAndExtension,
						assetData.pathUnderSourceBase,
						assetData.importedPath,
						assetData.assetId,
						assetData.assetType
					);
					outputSources.Add(inheritedInternalAssetData);
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
					var newInternalAssetData = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricatorOrBundlizer(
						newAssetPath,
						AssetDatabase.AssetPathToGUID(newAssetPath),
						AssetGraphInternalFunctions.GetAssetType(newAssetPath)
					);
					outputSources.Add(newInternalAssetData);
				}

				outputDict[groupKey] = outputSources;
			}

			Output(nodeId, labelToNext, outputDict);
		}

		public virtual void In (string groupKey, List<AssetInfo> source, string recommendedPrefabOutputDir) {
			Debug.LogError("should implement \"public override void In (List<AssetGraph.AssetInfo> source, string recommendedPrefabOutputDir)\" in class:" + this);
		}
	}
}