using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public class IntegratedGUIExporter : INodeOperation {
		public void Setup (BuildTarget target, 
			NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			ValidateExportPath(
				node.ExporterExportPath[target],
				FileUtility.GetPathWithProjectPath(node.ExporterExportPath[target]),
				() => {
					throw new NodeException(node.Name + ":Export Path is empty.", node.Id);
				},
				() => {
					if( node.ExporterExportOption[target] == (int)ExporterExportOption.ErrorIfNoExportDirectoryFound ) {
						throw new NodeException(node.Name + ":Directory set to Export Path does not exist. Path:" + node.ExporterExportPath[target], node.Id);
					}
				}
			);
		}
		
		public void Run (BuildTarget target, 
			NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output,
			Action<NodeData, string, float> progressFunc) 
		{
			Export(target, node, incoming, connectionsToOutput, progressFunc);
		}

		private void Export (BuildTarget target, 
			NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			Action<NodeData, string, float> progressFunc) 
		{
			if(incoming == null) {
				return;
			}

			var exportPath = FileUtility.GetPathWithProjectPath(node.ExporterExportPath[target]);

			if(node.ExporterExportOption[target] == (int)ExporterExportOption.DeleteAndRecreateExportDirectory) {
				if (Directory.Exists(exportPath)) {
					Directory.Delete(exportPath, true);
				}
			}

			if(node.ExporterExportOption[target] != (int)ExporterExportOption.ErrorIfNoExportDirectoryFound) {
				if (!Directory.Exists(exportPath)) {
					Directory.CreateDirectory(exportPath);
				}
			}

			var report = new ExportReport(node);

			foreach(var ag in incoming) {
				foreach (var groupKey in ag.assetGroups.Keys) {
					var inputSources = ag.assetGroups[groupKey];

					foreach (var source in inputSources) {					
						var destinationSourcePath = source.importFrom;

						// in bundleBulider, use platform-package folder for export destination.
						if (destinationSourcePath.StartsWith(AssetBundleGraphSettings.BUNDLEBUILDER_CACHE_PLACE)) {
							var depth = AssetBundleGraphSettings.BUNDLEBUILDER_CACHE_PLACE.Split(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR).Length + 1;

							var splitted = destinationSourcePath.Split(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR);
							var reducedArray = new string[splitted.Length - depth];

							Array.Copy(splitted, depth, reducedArray, 0, reducedArray.Length);
							var fromDepthToEnd = string.Join(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString(), reducedArray);

							destinationSourcePath = fromDepthToEnd;
						}

						var destination = FileUtility.PathCombine(exportPath, destinationSourcePath);

						var parentDir = Directory.GetParent(destination).ToString();

						if (!Directory.Exists(parentDir)) {
							Directory.CreateDirectory(parentDir);
						}
						if (File.Exists(destination)) {
							File.Delete(destination);
						}
						if (string.IsNullOrEmpty(source.importFrom)) {
							report.AddErrorEntry(source.absolutePath, destination, "Source Asset import path is empty; given asset is not imported by Unity.");
							continue;
						}
						try {
							if(progressFunc != null) progressFunc(node, string.Format("Copying {0}", source.fileNameAndExtension), 0.5f);
							File.Copy(source.importFrom, destination);
							report.AddExportedEntry(source.importFrom, destination);
						} catch(Exception e) {
							report.AddErrorEntry(source.importFrom, destination, e.Message);
						}

						source.exportTo = destination;
					}
				}
			}

			AssetBundleBuildReport.AddExportReport(report);
		}

		public static bool ValidateExportPath (string currentExportFilePath, string combinedPath, Action NullOrEmpty, Action DoesNotExist) {
			if (string.IsNullOrEmpty(currentExportFilePath)) {
				NullOrEmpty();
				return false;
			}
			if (!Directory.Exists(combinedPath)) {
				DoesNotExist();
				return false;
			}
			return true;
		}
	}
}