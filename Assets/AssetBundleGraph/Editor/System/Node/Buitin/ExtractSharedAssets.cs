using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine.AssetBundles.GraphTool;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

/**
	ImportSetting is the class for apply specific setting to already imported files.
*/
[CustomNode("Configure Bundle/Extract Shared Assets", 71)]
public class ExtractSharedAssets : Node {

	[SerializeField] private string m_bundleNameTemplate;

	public override string ActiveStyle {
		get {
			return "node 3 on";
		}
	}

	public override string InactiveStyle {
		get {
			return "node 3";
		}
	}

	public override string Category {
		get {
			return "Configure";
		}
	}

	public override Model.NodeOutputSemantics NodeInputType {
		get {
			return Model.NodeOutputSemantics.AssetBundleConfigurations;
		}
	}

	public override Model.NodeOutputSemantics NodeOutputType {
		get {
			return Model.NodeOutputSemantics.AssetBundleConfigurations;
		}
	}

	public override void Initialize(Model.NodeData data) {
		m_bundleNameTemplate = "shared_*";
		data.AddDefaultInputPoint();
		data.AddDefaultOutputPoint();
	}

	public override Node Clone(Model.NodeData newData) {
		var newNode = new ExtractSharedAssets();
		newNode.m_bundleNameTemplate = m_bundleNameTemplate;
		newData.AddDefaultInputPoint();
		newData.AddDefaultOutputPoint();
		return newNode;
	}

	public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

		EditorGUILayout.HelpBox("Extract Shared Assets: Extract shared assets between asset bundles and add bundle configurations.", MessageType.Info);
		editor.UpdateNodeName(node);

		GUILayout.Space(10f);

		var newValue = EditorGUILayout.TextField("Bundle Name Template", m_bundleNameTemplate);
		if(newValue != m_bundleNameTemplate) {
			using(new RecordUndoScope("Bundle Name Template Change", node, true)) {
				m_bundleNameTemplate = newValue;
				onValueChanged();
			}
		}

		EditorGUILayout.HelpBox("Bundle Name Template replaces \'*\' with number.", MessageType.Info);
	}

	/**
	 * Prepare is called whenever graph needs update. 
	 */ 
	public override void Prepare (BuildTarget target, 
		Model.NodeData node, 
		IEnumerable<PerformGraph.AssetGroups> incoming, 
		IEnumerable<Model.ConnectionData> connectionsToOutput, 
		PerformGraph.Output Output) 
	{
		if(string.IsNullOrEmpty(m_bundleNameTemplate)) {
			throw new NodeException(node.Name + ":Bundle Name Template is empty.", node.Id);
		}

		// Pass incoming assets straight to Output
		if(Output != null) {
			var destination = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();

			if(incoming != null) {

				var dependencyCollector = new Dictionary<string, List<string>>(); // [asset path:group name]
				var sharedDependency = new Dictionary<string, List<AssetReference>>();
				var groupNameMap = new Dictionary<string, string>();

				// build dependency map
				foreach(var ag in incoming) {
					foreach (var key in ag.assetGroups.Keys) {
						var assets = ag.assetGroups[key];

						foreach(var a in assets) {
							CollectDependencies(key, new string[] { a.importFrom }, ref dependencyCollector);
						}
					}
				}

				foreach(var entry in dependencyCollector) {
					if(entry.Value != null && entry.Value.Count > 1) {
						var joinedName = string.Join("-", entry.Value.ToArray());
						if(!groupNameMap.ContainsKey(joinedName)) {
							var count = groupNameMap.Count;
							var newName = m_bundleNameTemplate.Replace("*", count.ToString());
							if(newName == m_bundleNameTemplate) {
								newName = m_bundleNameTemplate + count.ToString();
							}
							groupNameMap.Add(joinedName, newName);
						}
						var groupName = groupNameMap[joinedName];
						if(!sharedDependency.ContainsKey(groupName)) {
							sharedDependency[groupName] = new List<AssetReference>();
						}
						sharedDependency[groupName].Add( AssetReference.CreateReference(entry.Key) );
					}
				}

				if(sharedDependency.Keys.Count > 0) {
					foreach(var ag in incoming) {
						Output(destination, new Dictionary<string, List<AssetReference>>(ag.assetGroups));
					}
					Output(destination, sharedDependency);
				} else {
					foreach(var ag in incoming) {
						Output(destination, ag.assetGroups);
					}
				}

			} else {
				// Overwrite output with empty Dictionary when no there is incoming asset
				Output(destination, new Dictionary<string, List<AssetReference>>());
			}
		}
	}

	private void CollectDependencies(string groupKey, string[] assetPaths, ref Dictionary<string, List<string>> collector) {
		var dependencies = AssetDatabase.GetDependencies(assetPaths);
		bool collectedAny = false;
		foreach(var d in dependencies) {
			if(!collector.ContainsKey(d)) {
				collector[d] = new List<string>();
			}
			if(!collector[d].Contains(groupKey)) {
				collector[d].Add(groupKey);
				collector[d].Sort();
				collectedAny = true;
			}
		}

		if(collectedAny) {
			CollectDependencies(groupKey, dependencies, ref collector);
		}
	}
}
