using System;
using System.Collections.Generic;
using UnityEditor;

namespace AssetBundleGraph.V2 {
	/**
		interface of all nodes
	*/
	public interface INodeOperation {

		/**
			Prepare is the method which validates and perform necessary setups in order to build.
		*/
		void Prepare (BuildTarget target, 
			V2.NodeData nodeData, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output outputFunc);

		/**
			Build is the method which actualy performs the build. It is always called after Setup() is performed.
		*/
		void Build (BuildTarget target, 
			V2.NodeData nodeData, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output outputFunc,
			Action<V2.NodeData, string, float> progressFunc);
	}
}