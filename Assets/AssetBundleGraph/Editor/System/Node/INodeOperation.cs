using System;
using System.Collections.Generic;
using UnityEditor;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	/**
		interface of all nodes
	*/
	public interface INodeOperation {

		/**
			Prepare is the method which validates and perform necessary setups in order to build.
		*/
		void Prepare (BuildTarget target, 
			Model.NodeData nodeData, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output outputFunc);

		/**
			Build is the method which actualy performs the build. It is always called after Setup() is performed.
		*/
		void Build (BuildTarget target, 
			Model.NodeData nodeData, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output outputFunc,
			Action<Model.NodeData, string, float> progressFunc);
	}
}