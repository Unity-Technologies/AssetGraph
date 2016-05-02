using System;
using System.Collections.Generic;

namespace AssetBundleGraph {
	/**
		interface of all nodes
	*/
	public interface INodeBase {

		/**
			fire when setup.
		*/
		void Setup (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output);

		/**
			fire when build.
		*/
		void Run (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output);
	}
}