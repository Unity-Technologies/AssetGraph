using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedGUIImporter : INodeBase {
		public void Setup (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			var samplingDirectoryPath = FileController.PathCombine(AssetGraphSettings.IMPORTER_SAMPLING_PLACE, nodeId);
			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				
				var assumedImportedAssetDatas = new List<InternalAssetData>();
				
				var first = true;

				// caution if file is exists already.
				if (Directory.Exists(samplingDirectoryPath)) {
					var filesInSampling = FileController.FilePathsInFolder(samplingDirectoryPath);
					switch (filesInSampling.Count) {
						case 0: {
							Debug.LogWarning("sampling start. 仮のimportが走るんで、なにかするならここ。");
							break;
						}
						case 1: {
							first = false;
							break;
						}
						default: {
							first = false;
							break;
						}
					}
				}

				foreach (var inputSource in inputSources) {
					var assumedImportedBasePath = inputSource.absoluteSourcePath.Replace(inputSource.sourceBasePath, AssetGraphSettings.IMPORTER_TEMP_PLACE);
					var assumedImportedPath = FileController.PathCombine(assumedImportedBasePath, nodeId);

					var assumedType = AssumeTypeFromExtension();

					var newData = InternalAssetData.InternalAssetDataByImporter(
						inputSource.traceId,
						inputSource.absoluteSourcePath,
						inputSource.sourceBasePath,
						inputSource.fileNameAndExtension,
						inputSource.pathUnderSourceBase,
						assumedImportedPath,
						null,
						assumedType
					);
					assumedImportedAssetDatas.Add(newData);

					if (first) {
						if (!Directory.Exists(samplingDirectoryPath)) Directory.CreateDirectory(samplingDirectoryPath);

						var absoluteFilePath = inputSource.absoluteSourcePath;
						var targetFilePath = FileController.PathCombine(samplingDirectoryPath, inputSource.fileNameAndExtension);

						FileController.CopyFileFromGlobalToLocal(absoluteFilePath, targetFilePath);
						first = false;
						Debug.Log("succeeded to sampling:" + targetFilePath);
						AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
					}
				}

				outputDict[groupKey] = assumedImportedAssetDatas;
			}

			Output(nodeId, labelToNext, outputDict, alreadyCached);
		}
		
		public void Run (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			var samplingDirectoryPath = FileController.PathCombine(AssetGraphSettings.IMPORTER_SAMPLING_PLACE, nodeId);
			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			var targetDirectoryPath = FileController.PathCombine(AssetGraphSettings.IMPORTER_TEMP_PLACE, nodeId);

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				
				// caution if file is exists already.
				var sampleAssetPath = string.Empty;
				if (Directory.Exists(samplingDirectoryPath)) {
					var filesInSampling = FileController.FilePathsInFolder(samplingDirectoryPath);
					switch (filesInSampling.Count) {
						case 0: {
							Debug.LogWarning("no samples found in samplingDirectoryPath:" + samplingDirectoryPath + ", please reload first.");
							return;
						}
						case 1: {
							Debug.Log("using sample:" + filesInSampling[0]);
							sampleAssetPath = filesInSampling[0];
							break;
						}
						default: {
							Debug.LogWarning("too many samples in samplingDirectoryPath:" + samplingDirectoryPath);
							return;
						}
					}
				} else {
					Debug.LogWarning("no samples found in samplingDirectoryPath:" + samplingDirectoryPath + ", please reload first.");
				}

				var samplingAssetImporter = AssetImporter.GetAtPath(sampleAssetPath);
				
				/*
					copy all sources from outside to inside of Unity.
				*/
				InternalSamplingImportAdopter.Attach(samplingAssetImporter);
				foreach (var inputSource in inputSources) {
					var absoluteFilePath = inputSource.absoluteSourcePath;
					var pathUnderSourceBase = inputSource.pathUnderSourceBase;

					var targetFilePath = FileController.PathCombine(targetDirectoryPath, pathUnderSourceBase);

					if (File.Exists(targetFilePath)) {
						Debug.LogError("この時点でファイルがダブってる場合どうしようかな、、事前のエラーでここまで見ても意味はないな。2");
						continue;
					}
					try {
						// copy files into local.

						FileController.CopyFileFromGlobalToLocal(absoluteFilePath, targetFilePath);
					} catch (Exception e) {
						Debug.LogError("IntegratedGUIImporter:" + this + " error:" + e);
					}
				}
				AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
				InternalSamplingImportAdopter.Detach();

				// get files, which are already assets.
				var localFilePathsAfterImport = FileController.FilePathsInFolder(targetDirectoryPath);

				var localFilePathsWithoutTargetDirectoryPath = localFilePathsAfterImport.Select(path => InternalAssetData.GetPathWithoutBasePath(path, targetDirectoryPath)).ToList();
				
				var outputSources = new List<InternalAssetData>();

				// generate matching between source and imported assets.
				foreach (var localFilePathWithoutTargetDirectoryPath in localFilePathsWithoutTargetDirectoryPath) {
					foreach (var inputtedSourceCandidate in inputSources) {
						var pathsUnderSourceBase = inputtedSourceCandidate.pathUnderSourceBase;

						if (localFilePathWithoutTargetDirectoryPath == pathsUnderSourceBase) {
							var localFilePathWithTargetDirectoryPath = InternalAssetData.GetPathWithBasePath(localFilePathWithoutTargetDirectoryPath, targetDirectoryPath);

							var newInternalAssetData = InternalAssetData.InternalAssetDataByImporter(
								inputtedSourceCandidate.traceId,
								inputtedSourceCandidate.absoluteSourcePath,
								inputtedSourceCandidate.sourceBasePath,
								inputtedSourceCandidate.fileNameAndExtension,
								inputtedSourceCandidate.pathUnderSourceBase,
								localFilePathWithTargetDirectoryPath,
								AssetDatabase.AssetPathToGUID(localFilePathWithTargetDirectoryPath),
								AssetGraphInternalFunctions.GetAssetType(localFilePathWithTargetDirectoryPath)
							);
							outputSources.Add(newInternalAssetData);
						}
					}
				}

				/*
					check if new Assets are generated, trace it.
				*/
				var assetPathsWhichAreAlreadyTraced = outputSources.Select(path => path.pathUnderSourceBase).ToList();
				var assetPathsWhichAreNotTraced = localFilePathsWithoutTargetDirectoryPath.Except(assetPathsWhichAreAlreadyTraced);
				foreach (var newAssetPath in assetPathsWhichAreNotTraced) {
					var basePathWithNewAssetPath = InternalAssetData.GetPathWithBasePath(newAssetPath, targetDirectoryPath);
					var newInternalAssetData = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(
						basePathWithNewAssetPath,
						AssetDatabase.AssetPathToGUID(basePathWithNewAssetPath),
						AssetGraphInternalFunctions.GetAssetType(basePathWithNewAssetPath)
					);
					outputSources.Add(newInternalAssetData);
				}


				outputDict[groupKey] = outputSources;
			}

			Output(nodeId, labelToNext, outputDict, alreadyCached);
		}
		
		public Type AssumeTypeFromExtension () {
			return typeof(UnityEngine.Object);
		}
	}
}