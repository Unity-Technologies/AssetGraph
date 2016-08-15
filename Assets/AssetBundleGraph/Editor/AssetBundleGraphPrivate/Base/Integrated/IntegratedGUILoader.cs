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

		public void Setup (string nodeName, string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> unused, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {

			try {
				ValidateLoadPath(
					loadFilePath,
					loadFilePath,
					() => {
						throw new NodeException(nodeName + ": Load Path is empty.", nodeId);
					}, 
					() => {
						throw new NodeException(nodeName + ": Directory not found: " + loadFilePath, nodeId);
					}
				);
			} catch(NodeException e) {
				AssetBundleGraph.AddNodeException(e);
				return;
			}
			
			// SOMEWHERE_FULLPATH/PROJECT_FOLDER/Assets/
			var assetsFolderPath = Application.dataPath + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
			
			var outputSource = new List<InternalAssetData>();
			var targetFilePaths = FileController.FilePathsInFolder(loadFilePath);

			try {	
				foreach (var targetFilePath in targetFilePaths) {
					// already contained into Assets/ folder.
					// imported path is Assets/SOMEWHERE_FILE_EXISTS.
					if (targetFilePath.StartsWith(assetsFolderPath)) {
						var importedPath = targetFilePath.Replace(assetsFolderPath, AssetBundleGraphSettings.ASSETS_PATH);
						
						var assetType = AssetBundleGraphInternalFunctions.GetAssetType(importedPath);
						if (assetType == typeof(object)) continue;

						outputSource.Add(
							InternalAssetData.InternalImportedAssetDataByLoader(
								targetFilePath, 
								loadFilePath,
								importedPath,
								AssetDatabase.AssetPathToGUID(importedPath),
								assetType
							)
						);
						continue;
					}

					throw new NodeException(nodeName + ": Invalid target file path. Path needs to be set under Assets/ :" + targetFilePath, nodeId);
				}
			} catch(NodeException e) {
				AssetBundleGraph.AddNodeException(e);
				return;
			}
			catch (Exception e) {
				Debug.LogError("Loader error:" + e);
			}

			var outputDir = new Dictionary<string, List<InternalAssetData>> {
				{"0", outputSource}
			};

			Output(nodeId, labelToNext, outputDir, new List<string>());
		}
		
		public void Run (string nodeName, string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> unused, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			ValidateLoadPath(
				loadFilePath,
				loadFilePath,
				() => {
					throw new AssetBundleGraphBuildException(nodeName + ": Load Path is empty.");
				}, 
				() => {
					throw new AssetBundleGraphBuildException(nodeName + ": Directory not found: " + loadFilePath);
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
					
					throw new AssetBundleGraphSetupException(nodeName + ": Invalid target file path. Path needs to be set under Assets/ :" + targetFilePath);
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