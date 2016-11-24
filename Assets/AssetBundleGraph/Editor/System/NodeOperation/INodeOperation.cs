using System;
using System.Collections.Generic;
using UnityEditor;

namespace AssetBundleGraph {
	/**
		interface of all nodes
	*/
	public interface INodeOperation {

		/**
			Setup is the method which validates and perform necessary setups in order to build.
		*/
		void Setup (BuildTarget target, 
			NodeData nodeData, 
			ConnectionData connectionFromInput, 
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output outputFunc);

		/**
			Run is the method which actualy performs the build. It is always called after Setup() is performed.
		*/
		void Run (BuildTarget target, 
			NodeData nodeData, 
			ConnectionData connectionFromInput, 
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output outputFunc);
	}
}