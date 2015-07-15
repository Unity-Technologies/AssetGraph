using System;

namespace AssetGraph {
	public struct Asset {
		public string assetName;
		public Type assetType;
		public string assetPath;
		public string assetId;

		public Asset (string assetName, Type assetType, string assetPath, string assetId) {
			this.assetName = assetName;
			this.assetType = assetType;
			this.assetPath = assetPath;
			this.assetId = assetId;
		}
	}
}