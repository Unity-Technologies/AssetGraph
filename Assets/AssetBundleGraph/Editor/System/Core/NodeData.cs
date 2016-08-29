using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AssetBundleGraph {
	public class NodeData {
		public readonly string nodeName;
		public readonly string nodeId;
		public readonly AssetBundleGraphSettings.NodeKind nodeKind;
		public readonly List<string> outputPointIds;

		// for All script nodes & prefabricator, bundlizer GUI.
		public readonly string scriptClassName;

		// for Loader Script
		public readonly Dictionary<string, string> loadFilePath;

		// for Exporter Script
		public readonly Dictionary<string, string> exportFilePath;

		// for Filter GUI data
		public readonly List<string> containsKeywords;
		public readonly List<string> containsKeytypes;

		// for Importer GUI data
		public readonly Dictionary<string, string> importerPackages;
		
		// for Modifier GUI data
		public readonly Dictionary<string, string> modifierPackages;

		// for Grouping GUI data
		public readonly Dictionary<string, string> groupingKeyword;

		// for Bundlizer GUI data
		public readonly Dictionary<string, string> bundleNameTemplate;

		// for BundleBuilder GUI data
		public readonly Dictionary<string, List<string>> enabledBundleOptions;

		
		public List<ConnectionData> connectionToParents = new List<ConnectionData>();

		private bool done;

		public NodeData (
			string nodeId, 
			AssetBundleGraphSettings.NodeKind nodeKind,
			string nodeName,
			List<string> outputPointIds,
			string scriptClassName = null,
			Dictionary<string, string> loadPath = null,
			Dictionary<string, string> exportTo = null,
			List<string> filterContainsKeywords = null,
			List<string> filterContainsKeytypes = null,
			Dictionary<string, string> importerPackages = null,
			Dictionary<string, string> modifierPackages = null,
			Dictionary<string, string> groupingKeyword = null,
			Dictionary<string, string> bundleNameTemplate = null,
			Dictionary<string, List<string>> enabledBundleOptions = null
		) {
			this.nodeId = nodeId;
			this.nodeKind = nodeKind;
			this.nodeName = nodeName;
			this.outputPointIds = outputPointIds;

			this.scriptClassName = null;
			this.loadFilePath = null;
			this.exportFilePath = null;
			this.containsKeywords = null;
			this.importerPackages = null;
			this.modifierPackages = null;
			this.groupingKeyword = null;
			this.bundleNameTemplate = null;
			this.enabledBundleOptions = null;

			switch (nodeKind) {
				case AssetBundleGraphSettings.NodeKind.LOADER_GUI: {
					this.loadFilePath = loadPath;
					break;
				}
				case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
					this.exportFilePath = exportTo;
					break;
				}

				case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI: {
					this.scriptClassName = scriptClassName;
					break;
				}

				case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
					this.containsKeywords = filterContainsKeywords;
					this.containsKeytypes = filterContainsKeytypes;
					break;
				}

				case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
					this.importerPackages = importerPackages;
					break;
				}

				case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI: {
					this.modifierPackages = modifierPackages;
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
					this.groupingKeyword = groupingKeyword;
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.bundleNameTemplate = bundleNameTemplate;
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.enabledBundleOptions = enabledBundleOptions;
					break;
				}

				default: {
					Debug.LogError(nodeName + " is defined as unknown kind of node. value:" + nodeKind);
					break;
				}
			}
		}

		public void AddConnectionToParent (ConnectionData connection) {
			connectionToParents.Add(new ConnectionData(connection));
		}

		public void Done () {
			done = true;
		}

		public bool IsAlreadyDone () {
			return done;
		}
	}
}
