using UnityEngine;

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;

namespace AssetBundleGraph {
	public class IntegratedGUILoader : INodeBase {
		private readonly string loadFilePath;
		
		public IntegratedGUILoader (string loadFilePath) {
			this.loadFilePath = loadFilePath;
		}

		public void Setup (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> unused, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			ValidateLoadPath(
				loadFilePath,
				loadFilePath,
				() => {
					throw new Exception("load path is empty.");
				}, 
				() => {
					throw new Exception("directory not found:" + loadFilePath);
				}
			);
			
			// SOMEWHERE_FULLPATH/PROJECT_FOLDER/Assets/
			var assetsFolderPath = Application.dataPath + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
			
			var outputSource = new List<InternalAssetData>();
			try {
				var targetFilePaths = FileController.FilePathsInFolder(loadFilePath);
				
				foreach (var targetFilePath in targetFilePaths) {
					// already contained into Assets/ folder.
					// imported path is Assets/SOMEWHERE_FILE_EXISTS.
					if (targetFilePath.StartsWith(assetsFolderPath)) {
						var importedPath = targetFilePath.Replace(assetsFolderPath, AssetBundleGraphSettings.ASSETS_PATH);
						outputSource.Add(
							InternalAssetData.InternalImportedAssetDataByLoader(
								targetFilePath, 
								loadFilePath,
								importedPath,
								AssetDatabase.AssetPathToGUID(importedPath),
								AssetBundleGraphInternalFunctions.GetAssetType(importedPath)
							)
						);
						continue;
					}
					
					throw new Exception("loader:" + targetFilePath + " is not imported yet, should import before bundlize.");
					
					// outputSource.Add(
					// 	InternalAssetData.InternalAssetDataByLoader(
					// 		targetFilePath, 
					// 		loadFilePath
					// 	)
					// );
				}

				var outputDir = new Dictionary<string, List<InternalAssetData>> {
					{"0", outputSource}
				};

				Output(nodeId, labelToNext, outputDir, new List<string>());
			} catch (Exception e) {
				Debug.LogError("Loader error:" + e);
			}
		}
		
		public void Run (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> unused, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			ValidateLoadPath(
				loadFilePath,
				loadFilePath,
				() => {
					throw new Exception("load path is empty.");
				}, 
				() => {
					throw new Exception("directory not found:" + loadFilePath);
				}
			);
			
			// SOMEWHERE_FULLPATH/PROJECT_FOLDER/Assets/
			var assetsFolderPath = Application.dataPath + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
			
			var outputSource = new List<InternalAssetData>();
			try {
				var targetFilePaths = FileController.FilePathsInFolder(loadFilePath);
				
				foreach (var targetFilePath in targetFilePaths) {
					// already contained into Assets/ folder.
					// imported path is Assets/SOMEWHERE_FILE_EXISTS.
					if (targetFilePath.StartsWith(assetsFolderPath)) {
						var importedPath = targetFilePath.Replace(assetsFolderPath, AssetBundleGraphSettings.ASSETS_PATH);
						outputSource.Add(
							InternalAssetData.InternalImportedAssetDataByLoader(
								targetFilePath, 
								loadFilePath,
								importedPath,
								AssetDatabase.AssetPathToGUID(importedPath),
								AssetBundleGraphInternalFunctions.GetAssetType(importedPath)
							)
						);
						continue;
					}
					
					throw new Exception("loader:" + targetFilePath + " is not imported yet, should import before bundlize.");
					
					// outputSource.Add(
					// 	InternalAssetData.InternalAssetDataByLoader(
					// 		targetFilePath, 
					// 		loadFilePath
					// 	)
					// );
				}
				
				var outputDir = new Dictionary<string, List<InternalAssetData>> {
					{"0", outputSource}
				};

				Output(nodeId, labelToNext, outputDir, new List<string>());
			} catch (Exception e) {
				Debug.LogError("Loader error:" + e);
			}
		}

		public static void ValidateLoadPath (string currentLoadPath, string combinedPath, Action NullOrEmpty, Action NotExist) {
			if (string.IsNullOrEmpty(currentLoadPath)) NullOrEmpty();
			if (!Directory.Exists(combinedPath)) NotExist();
		}
	}
}