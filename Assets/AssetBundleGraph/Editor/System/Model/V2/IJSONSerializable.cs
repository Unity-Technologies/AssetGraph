using UnityEngine;
using System.Collections;

using AssetBundleGraph;

namespace AssetBundleGraph.V2 {

	public interface IJSONSerializable {
		/**
		 * Serialize this Modifier to JSON using JsonUtility.
		 */
		string Serialize();
	}

}