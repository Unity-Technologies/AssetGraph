using System;
using System.Collections.Generic;

namespace AssetBundleGraph {
	/**
		interface of all nodes
	*/
	public interface INodeOperationBase {

		/**
			fire when setup.
		*/
		void Setup (string nodeName, string connectionIdToNextNode, string labelToNext, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output);

		/**
			fire when build.
		*/
		void Run (string nodeName, string connectionIdToNextNode, string labelToNext, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output);
	}
}