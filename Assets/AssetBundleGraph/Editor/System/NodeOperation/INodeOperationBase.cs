using System;
using System.Collections.Generic;
using UnityEditor;

namespace AssetBundleGraph {
	/**
		interface of all nodes
	*/
	public interface INodeOperationBase {

		/**
			Setup is the method which validates and perform necessary setups in order to build.
		*/
		void Setup (BuildTarget target, 
			NodeData nodeData, 
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output);

		/**
			Run is the method which actualy performs the build. It is always called after Setup() is performed.
		*/
		void Run (BuildTarget target, 
			NodeData nodeData, 
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output);
	}
}