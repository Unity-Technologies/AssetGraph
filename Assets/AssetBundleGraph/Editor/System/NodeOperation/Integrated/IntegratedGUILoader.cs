using UnityEngine;

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;

namespace AssetBundleGraph {
	public class IntegratedGUILoader : INodeOperationBase {
		public void Setup (BuildTarget target, NodeData node, string connectionIdToNextNode, Dictionary<string, List<Asset>> unused, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {

			try {
				ValidateLoadPath(
					node.LoaderLoadPath[target],
					node.LoaderLoadPath[target],
					() => {
						//throw new NodeException(node.Name + ": Load Path is empty.", node.Id);
					}, 
					() => {
						throw new NodeException(node.Name + ": Directory not found: " + node.LoaderLoadPath[target], node.Id);
					}
				);
			} catch(NodeException e) {
				AssetBundleGraphEditorWindow.AddNodeException(e);
				return;
			}
			
			// SOMEWHERE_FULLPATH/PROJECT_FOLDER/Assets/
			var assetsFolderPath = Application.dataPath + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
			
			var outputSource = new List<Asset>();
			var targetFilePaths = FileUtility.FilePathsInFolder(node.LoaderLoadPath[target]);

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

					throw new NodeException(node.Name + ": Invalid Load Path. Path must start with Assets/", node.Name);
				}
			} catch(NodeException e) {
				AssetBundleGraphEditorWindow.AddNodeException(e);
				return;
			}
			catch (Exception e) {
				Debug.LogError(node.Name + " Error:" + e);
			}

			var outputDir = new Dictionary<string, List<Asset>> {
				{"0", outputSource}
			};

			Output(node.Id, connectionIdToNextNode, outputDir, new List<string>());
		}
		
		public void Run (BuildTarget target, NodeData node, string connectionIdToNextNode, Dictionary<string, List<Asset>> unused, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			ValidateLoadPath(
				node.LoaderLoadPath[target],
				node.LoaderLoadPath[target],
				() => {
					//throw new AssetBundleGraphBuildException(node.Name + ": Load Path is empty.");
				}, 
				() => {
					throw new AssetBundleGraphBuildException(node.Name + ": Directory not found: " + node.LoaderLoadPath[target]);
				}
			);
			
			// SOMEWHERE_FULLPATH/PROJECT_FOLDER/Assets/
			var assetsFolderPath = Application.dataPath + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
			
			var outputSource = new List<Asset>();
			try {
				var targetFilePaths = FileUtility.FilePathsInFolder(node.LoaderLoadPath[target]);
				
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
					
					throw new AssetBundleGraphSetupException(node.Name + ": Invalid target file path. Path needs to be set under Assets/ :" + targetFilePath);
				}
				
				var outputDir = new Dictionary<string, List<Asset>> {
					{"0", outputSource}
				};

				Output(node.Id, connectionIdToNextNode, outputDir, new List<string>());
			} catch (Exception e) {
				throw new NodeException(e.Message, node.Id);
			}
		}

		public static void ValidateLoadPath (string currentLoadPath, string combinedPath, Action NullOrEmpty, Action NotExist) {
			if (string.IsNullOrEmpty(currentLoadPath)) NullOrEmpty();
			if (!Directory.Exists(combinedPath)) NotExist();
		}
	}
}