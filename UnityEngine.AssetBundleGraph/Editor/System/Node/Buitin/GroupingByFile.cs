
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool
{
	[CustomNode("Group Assets/Group By File", 42)]
	public class GroupingByFile : Node {

		public override string ActiveStyle {
			get {
				return "node 2 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 2";
			}
		}

		public override string Category {
			get {
				return "Group";
			}
		}

		public override void Initialize(Model.NodeData data) {
			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public override Node Clone(Model.NodeData newData) {
            var newNode = new GroupingByFile();

            newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();
			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			EditorGUILayout.HelpBox("Group By File: Create group per individual asset.", MessageType.Info);
			editor.UpdateNodeName(node);
		}

		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
            if(connectionsToOutput == null || Output == null) {
                return;
            }

            var outputDict = new Dictionary<string, List<AssetReference>>();

            if(incoming != null) {
                int i = 0;
                foreach(var ag in incoming) {
                    foreach (var assets in ag.assetGroups.Values) {
                        foreach(var a in assets) {
                            var key = i.ToString ();
                            outputDict[key] = new List<AssetReference>();
                            outputDict [key].Add (a);
                            ++i;
                        }
                    }
                }
            }

            var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
                null : connectionsToOutput.First();
            Output(dst, outputDict);
        }
	}
}