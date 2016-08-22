using UnityEngine;

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;

namespace AssetBundleGraph {
	public class IntegratedGUILoader : INodeOperationBase {
		private readonly string loadFilePath;
		
		public IntegratedGUILoader (string loadFilePath) {
			this.loadFilePath = loadFilePath;
		}

		public void Setup (string nodeName, string nodeId, string connectionIdToNextNode, Dictionary<string, List<Asset>> unused, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {

			try {
				ValidateLoadPath(
					loadFilePath,
					loadFilePath,
					() => {
						//throw new NodeException(nodeName + ": Load Path is empty.", nodeId);
					}, 
					() => {
						throw new NodeException(nodeName + ": Directory not found: " + loadFilePath, nodeId);
					}
				);
			} catch(NodeException e) {
				AssetBundleGraphEditorWindow.AddNodeException(e);
				return;
			}
			
			// SOMEWHERE_FULLPATH/PROJECT_FOLDER/Assets/
			var assetsFolderPath = Application.dataPath + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
			
			var outputSource = new List<Asset>();
			var targetFilePaths = FileUtility.FilePathsInFolder(loadFilePath);

			try {	
				foreach (var targetFilePath in targetFilePaths) {

					if(targetFilePath.Contains(AssetBundleGraphSettings.ASSETBUNDLEGRAPH_PATH)) {
						continue;
					}

					// already contained into Assets/ folder.
					// imported path is Assets/SOMEWHERE_FILE_EXISTS.
					if (targetFilePath.StartsWith(assetsFolderPath)) {
						var relativePath = targetFilePath.Replace(assetsFolderPath, AssetBundleGraphSettings.ASSETS_PATH);
						
						var assetType = TypeUtility.GetTypeOfAsset(relativePath);
						if (assetType == typeof(object)) {
							continue;
						}

						outputSource.Add(Asset.CreateNewAssetFromLoader(targetFilePath, relativePath));
						continue;
					}

					throw new NodeException(nodeName + ": Invalid Load Path. Path must start with Assets/", nodeId);
				}
			} catch(NodeException e) {
				AssetBundleGraphEditorWindow.AddNodeException(e);
				return;
			}
			catch (Exception e) {
				Debug.LogError(nodeName + " Error:" + e);
			}

			var outputDir = new Dictionary<string, List<Asset>> {
				{"0", outputSource}
			};

			Output(nodeId, connectionIdToNextNode, outputDir, new List<string>());
		}
		
		public void Run (string nodeName, string nodeId, string connectionIdToNextNode, Dictionary<string, List<Asset>> unused, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			ValidateLoadPath(
				loadFilePath,
				loadFilePath,
				() => {
					//throw new AssetBundleGraphBuildException(nodeName + ": Load Path is empty.");
				}, 
				() => {
					throw new AssetBundleGraphBuildException(nodeName + ": Directory not found: " + loadFilePath);
				}
			);
			
			// SOMEWHERE_FULLPATH/PROJECT_FOLDER/Assets/
			var assetsFolderPath = Application.dataPath + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
			
			var outputSource = new List<Asset>();
			try {
				var targetFilePaths = FileUtility.FilePathsInFolder(loadFilePath);
				
				foreach (var targetFilePath in targetFilePaths) {

					if(targetFilePath.Contains(AssetBundleGraphSettings.ASSETBUNDLEGRAPH_PATH)) {
						continue;
					}

					// already contained into Assets/ folder.
					// imported path is Assets/SOMEWHERE_FILE_EXISTS.
					if (targetFilePath.StartsWith(assetsFolderPath)) {
						var importFrom = targetFilePath.Replace(assetsFolderPath, AssetBundleGraphSettings.ASSETS_PATH);
						
						outputSource.Add(Asset.CreateNewAssetFromLoader(targetFilePath, importFrom));
						continue;
					}
					
					throw new AssetBundleGraphSetupException(nodeName + ": Invalid target file path. Path needs to be set under Assets/ :" + targetFilePath);
				}
				
				var outputDir = new Dictionary<string, List<Asset>> {
					{"0", outputSource}
				};

				Output(nodeId, connectionIdToNextNode, outputDir, new List<string>());
			} catch (Exception e) {
				Debug.LogError(nodeName + " Error:" + e);
			}
		}

		public static void ValidateLoadPath (string currentLoadPath, string combinedPath, Action NullOrEmpty, Action NotExist) {
			if (string.IsNullOrEmpty(currentLoadPath)) NullOrEmpty();
			if (!Directory.Exists(combinedPath)) NotExist();
		}
	}
}