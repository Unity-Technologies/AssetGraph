using UnityEngine;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public class IntegratedGUIExporter : INodeBase {
		private readonly string exportFilePath;

		public IntegratedGUIExporter (string exportFilePath) {
			this.exportFilePath = exportFilePath;
		}
		
		public void Setup (string nodeName, string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {

			try {
				ValidateExportPath(
					exportFilePath,
					exportFilePath,
					() => {
						throw new NodeException(nodeName + ":Export Path is empty.", nodeId);
					},
					() => {
						throw new NodeException(nodeName + ":Directory set to Export Path does not exist. Path:" + exportFilePath, nodeId);
					}
				);
			} catch(NodeException e) {
				AssetBundleGraph.AddNodeException(e);
				return;
			}

			Export(nodeId, labelToNext, groupedSources, Output, false);
		}
		
		public void Run (string nodeName, string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			ValidateExportPath(
				exportFilePath,
				exportFilePath,
				() => {
					throw new AssetBundleGraphBuildException(nodeName + ":Export Path is empty.");
				},
				() => {
					throw new AssetBundleGraphBuildException(nodeName + ":Directory set to Export Path does not exist. Path:" + exportFilePath);
				}
			);

			Export(nodeId, labelToNext, groupedSources, Output, true);
		}

		private void Export (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output, bool isRun) {
			var outputDict = new Dictionary<string, List<InternalAssetData>>();
			outputDict["0"] = new List<InternalAssetData>();

			var failedExports = new List<string>();

			foreach (var groupKey in groupedSources.Keys) {
				var exportedAssets = new List<InternalAssetData>();
				var inputSources = groupedSources[groupKey];

				foreach (var source in inputSources) {
					if (isRun) {
						if (!Directory.Exists(exportFilePath)) Directory.CreateDirectory(exportFilePath);
					}
					
					var destinationSourcePath = source.importedPath;
					
					// in bundleBulider, use platform-package folder for export destination.
					if (destinationSourcePath.StartsWith(AssetBundleGraphSettings.BUNDLEBUILDER_CACHE_PLACE)) {
						var depth = AssetBundleGraphSettings.BUNDLEBUILDER_CACHE_PLACE.Split(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR).Length + 1;
						
						var splitted = destinationSourcePath.Split(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR);
						var reducedArray = new string[splitted.Length - depth];
						
						Array.Copy(splitted, depth, reducedArray, 0, reducedArray.Length);
						var fromDepthToEnd = string.Join(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString(), reducedArray);
						
						destinationSourcePath = fromDepthToEnd;
					}
					
					var destination = FileController.PathCombine(exportFilePath, destinationSourcePath);
					
					var parentDir = Directory.GetParent(destination).ToString();

					if (isRun) {
						if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);
						if (File.Exists(destination)) File.Delete(destination);
						if (string.IsNullOrEmpty(source.importedPath)) {
							failedExports.Add(source.absoluteSourcePath);
							continue;
						}
						File.Copy(source.importedPath, destination);
					}

					var exportedAsset = InternalAssetData.InternalAssetDataGeneratedByExporter(destination);
					exportedAssets.Add(exportedAsset);
				}
				outputDict["0"].AddRange(exportedAssets);
			}

			if (failedExports.Any()) {
				Debug.LogError("[Exporter] Given files failed to export: " + string.Join(", ", failedExports.ToArray()) + " must be imported before export.");
			}

			Output(nodeId, labelToNext, outputDict, new List<string>());
		}

		public static void ValidateExportPath (string currentExportFilePath, string combinedPath, Action NullOrEmpty, Action NotExist) {
			if (string.IsNullOrEmpty(currentExportFilePath)) NullOrEmpty();
			if (!Directory.Exists(combinedPath)) NotExist();
		}
	}
}