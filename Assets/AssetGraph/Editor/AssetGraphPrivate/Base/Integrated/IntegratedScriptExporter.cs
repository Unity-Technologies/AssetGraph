using UnityEngine;

using System;
using System.IO;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedScriptExporter : INodeBase {
		public readonly string exportFilePath;
		
		public IntegratedScriptExporter (string exportFilePath) {
			this.exportFilePath = exportFilePath;
		}

		public void Setup (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>> Output) {
			if (string.IsNullOrEmpty(exportFilePath)) {
				Debug.LogWarning("no Export Path set.");
				return;
			}

			if (!Directory.Exists(exportFilePath)) {
				Debug.LogError("no Export Path found, exportFilePath:" + exportFilePath);
				return;
			}
		}
		
		public void Run (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>> Output) {
			if (string.IsNullOrEmpty(exportFilePath)) {
				Debug.LogWarning("no Export Path set.");
				return;
			}

			if (!Directory.Exists(exportFilePath)) {
				Debug.LogError("no Export Path found, exportFilePath:" + exportFilePath);
				return;
			}

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				foreach (var source in inputSources) {
					if (!Directory.Exists(exportFilePath)) Directory.CreateDirectory(exportFilePath);
					var destination = Path.Combine(exportFilePath, source.pathUnderConnectionId);
					var parentDir = Directory.GetParent(destination).ToString();

					if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

					if (File.Exists(destination)) File.Delete(destination);
					File.Copy(source.importedPath, destination);
				}
			}
		}
	}
}