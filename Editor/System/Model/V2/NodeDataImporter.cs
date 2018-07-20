using UnityEngine;
using System.Collections;

using Unity.AssetGraph;
using V1=AssetBundleGraph;

namespace Unity.AssetGraph.DataModel.Version2 {
	public interface NodeDataImporter {
		void Import(V1.NodeData v1, NodeData v2);
	}
}
