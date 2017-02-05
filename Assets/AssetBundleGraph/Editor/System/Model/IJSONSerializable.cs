using UnityEngine;
using System.Collections;

namespace UnityEngine.AssetBundles.GraphTool {

	public interface IJSONSerializable {
		/**
		 * Serialize this Modifier to JSON using JsonUtility.
		 */
		string Serialize();
	}

}