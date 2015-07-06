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
	}
}