
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using AddressableAssets;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
	[CustomNode("Configure Bundle/Asset Address", 40)]
	public class Addressable : Node {

        [SerializeField] private bool m_isAddressable;
        [SerializeField] private string m_matchPattern;
        [SerializeField] private string m_addressPattern;

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

		public override void Initialize(Model.NodeData data) {
            m_isAddressable = true;
            m_matchPattern = string.Empty;
            m_addressPattern = string.Empty;

			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public override Node Clone(Model.NodeData newData) {
            var newNode = new Addressable();
            newNode.m_isAddressable = m_isAddressable;
            newNode.m_matchPattern = m_matchPattern;
            newNode.m_addressPattern = m_addressPattern;

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();
			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			EditorGUILayout.HelpBox("Asset Address: Configure Asset Address with pattern.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

            var newAddressable = EditorGUILayout.ToggleLeft("Addressable", m_isAddressable);
            if (newAddressable != m_isAddressable) {
                m_isAddressable = newAddressable;
                onValueChanged ();
            }

            var newMatchPattern = EditorGUILayout.TextField("Match Pattern", m_matchPattern);
            if (newMatchPattern != m_matchPattern) {
                m_matchPattern = newMatchPattern;
                onValueChanged ();
            }

            using (new EditorGUI.DisabledScope (string.IsNullOrEmpty (newMatchPattern))) {
                var newAddressPattern = EditorGUILayout.TextField("Address Pattern", m_addressPattern);
                if (newAddressPattern != m_addressPattern) {
                    m_addressPattern = newAddressPattern;
                    onValueChanged ();
                }
            }

            EditorGUILayout.HelpBox(
                "You can use regular expression to patterns.", 
                MessageType.Info);
		}

		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
            var aaSettings = AddressableAssetSettings.GetDefault(false, false);

            if (aaSettings == null) {
                throw new NodeException ("Addressable Asset Settings not found.", "Create Addressable Asset Settings object from Addressables window.");
            }
		}

        public override void Build (BuildTarget target, 
            Model.NodeData node, 
            IEnumerable<PerformGraph.AssetGroups> incoming, 
            IEnumerable<Model.ConnectionData> connectionsToOutput, 
            PerformGraph.Output Output,
            Action<Model.NodeData, string, float> progressFunc) 
        {
            ConfigureAddress(target, node, incoming, connectionsToOutput, Output);
        }


		private void ConfigureAddress (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
            Dictionary<string, List<AssetReference>> outputDict = null;
            if (Output != null) {
                outputDict = new Dictionary<string, List<AssetReference>>();
            }

            var aaSettings = AddressableAssetSettings.GetDefault(false, false);
            AddressableAssetSettings.AssetGroup.AssetEntry entry = null;

            Regex match = null;

            if (!string.IsNullOrEmpty (m_matchPattern)) {
                match = new Regex (m_matchPattern);
            }

			if(incoming != null) {
				foreach(var ag in incoming) {
                    foreach (var g in ag.assetGroups.Keys) {
                        var assets = ag.assetGroups [g];
						foreach(var a in assets) {

                            var guid = a.assetDatabaseId;

                            if (m_isAddressable) {
                                entry = aaSettings.FindAssetEntry(guid);
                                if (entry == null) {
                                    entry = aaSettings.CreateOrMoveEntry (guid, aaSettings.DefaultGroup);
                                }

                                if (match != null) {
                                    if (match.IsMatch (a.importFrom)) {
                                        entry.address = match.Replace (a.importFrom, m_addressPattern);
                                    }
                                } else {
                                    entry.address = entry.assetPath;
                                }
                            } else {
                                aaSettings.RemoveAssetEntry (guid);
                            }

                            if (outputDict != null) {
                                if (!outputDict.ContainsKey(g)) {
                                    outputDict[g] = new List<AssetReference>();
                                }
                                outputDict[g].Add(a);
                            }
						}
					}
				}
			}

            if (outputDict != null) {
                var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
                    null : connectionsToOutput.First();
                Output(dst, outputDict);
            }
		}
    }
}