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

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				foreach (var source in inputSources) {
					if (!Directory.Exists(exportFilePath)) Directory.CreateDirectory(exportFilePath);
					var destination = FileController.PathCombine(exportFilePath, source.pathUnderConnectionId);
					var parentDir = Directory.GetParent(destination).ToString();

					if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

					if (File.Exists(destination)) File.Delete(destination);
					File.Copy(source.importedPath, destination);
				}
			}

			// there is no output from this node.
		}

		public static void ValidateExportPath (string currentExportFilePath, string combinedPath, Action NullOrEmpty, Action NotExist) {
			if (string.IsNullOrEmpty(currentExportFilePath)) NullOrEmpty();
			if (!Directory.Exists(combinedPath)) NotExist();
		}
	}
}