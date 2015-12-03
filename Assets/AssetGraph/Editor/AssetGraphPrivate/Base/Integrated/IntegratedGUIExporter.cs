using UnityEngine;

using System;
using System.IO;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedGUIExporter : INodeBase {
		private readonly string exportFilePath;

		public IntegratedGUIExporter (string exportFilePath) {
			this.exportFilePath = exportFilePath;
		}
		
		public void Setup (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			ValidateExportPath(
				exportFilePath,
				exportFilePath,
				() => {
					throw new Exception("no Export Path set.");
				},
				() => {
					throw new Exception("no Export Path found, exportFilePath:" + exportFilePath);
				}
			);

			Export(nodeId, labelToNext, groupedSources, Output, false);
		}
		
		public void Run (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			ValidateExportPath(
				exportFilePath,
				exportFilePath,
				() => {
					throw new Exception("no Export Path set.");
				},
				() => {
					throw new Exception("no Export Path found, exportFilePath:" + exportFilePath);
				}
			);

			Export(nodeId, labelToNext, groupedSources, Output, true);
		}

		private void Export (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output, bool isRun) {
			var outputDict = new Dictionary<string, List<InternalAssetData>>();
			outputDict["0"] = new List<InternalAssetData>();

			foreach (var groupKey in groupedSources.Keys) {
				var exportedAssets = new List<InternalAssetData>();
				var inputSources = groupedSources[groupKey];

				foreach (var source in inputSources) {
					if (isRun) {
						if (!Directory.Exists(exportFilePath)) Directory.CreateDirectory(exportFilePath);
					}

					var destination = FileController.PathCombine(exportFilePath, source.pathUnderConnectionId);
					var parentDir = Directory.GetParent(destination).ToString();

					if (isRun) {
						if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);
						if (File.Exists(destination)) File.Delete(destination);
						File.Copy(source.importedPath, destination);
					}

					var exportedAsset = InternalAssetData.InternalAssetDataGeneratedByExporter(destination);
					exportedAssets.Add(exportedAsset);
				}
				outputDict["0"].AddRange(exportedAssets);
			}

			Output(nodeId, labelToNext, outputDict, new List<string>());
		}

		public static void ValidateExportPath (string currentExportFilePath, string combinedPath, Action NullOrEmpty, Action NotExist) {
			if (string.IsNullOrEmpty(currentExportFilePath)) NullOrEmpty();
			if (!Directory.Exists(combinedPath)) NotExist();
		}
	}
}