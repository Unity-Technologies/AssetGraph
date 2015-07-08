using System;

namespace AssetGraph {
	public class AssetGraphSettings {
		
		public enum NodeKind : int {
			SOURCE,
			FILTER,
			IMPORTER,
			PREFABRICATOR,
			BUNDLIZER,
			DESTINATION
		}

		public static NodeKind NodeKindFromString (string val) {
			return (NodeKind)Enum.Parse(typeof(NodeKind), val);
		}
	}
}