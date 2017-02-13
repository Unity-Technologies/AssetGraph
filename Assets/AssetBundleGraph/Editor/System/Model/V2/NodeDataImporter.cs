using UnityEngine;
using System.Collections;

using UnityEngine.AssetBundles.GraphTool;
using V1=AssetBundleGraph;

namespace UnityEngine.AssetBundles.GraphTool.DataModel.Version2 {
	public interface NodeDataImporter {
		void Import(V1.NodeData v1, NodeData v2);
	}
}
