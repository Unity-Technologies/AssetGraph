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
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUIExporter.Setup");
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

			Export(target, node, connectionFromInput, connectionToOutput, inputGroupAssets, Output, false);
			Profiler.EndSample();
		}
		
		public void Run (BuildTarget target, 
			NodeData node, 
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUIExporter.Run");
			Export(target, node, connectionFromInput, connectionToOutput, inputGroupAssets, Output, true);
			Profiler.EndSample();
		}

		private void Export (BuildTarget target, 
			NodeData node, 
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output Output,
			bool isRun) 
		{
			var outputDict = new Dictionary<string, List<AssetReference>>();
			outputDict["0"] = new List<AssetReference>();

			var exportPath = FileUtility.GetPathWithProjectPath(node.ExporterExportPath[target]);

			if (isRun) {
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
			}

			var failedExports = new List<string>();

			foreach (var groupKey in inputGroupAssets.Keys) {
				var exportedAssets = new List<AssetReference>();
				var inputSources = inputGroupAssets[groupKey];

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

					if (isRun) {
						if (!Directory.Exists(parentDir)) {
							Directory.CreateDirectory(parentDir);
						}
						if (File.Exists(destination)) {
							File.Delete(destination);
						}
						if (string.IsNullOrEmpty(source.importFrom)) {
							failedExports.Add(source.absolutePath);
							continue;
						}
						try {
							File.Copy(source.importFrom, destination);
						} catch(Exception e) {
							failedExports.Add(source.importFrom);
							LogUtility.Logger.LogError(LogUtility.kTag, node.Name + ": Error occured: " + e.Message);
						}
					}

					source.exportTo = destination;
					exportedAssets.Add(source);
				}
				outputDict["0"].AddRange(exportedAssets);
			}

			if (failedExports.Any()) {
				LogUtility.Logger.LogError(LogUtility.kTag, node.Name + ": Failed to export files. All files must be imported before exporting: " + string.Join(", ", failedExports.ToArray()));
			}

			Output(outputDict);
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