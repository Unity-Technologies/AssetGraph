using System;

namespace AssetGraph {
	public class AssetInfo {
		public readonly string assetName;
		public readonly Type assetType;
		public readonly string assetPath;
		public readonly string assetId;

		public AssetInfo (string assetName, Type assetType, string assetPath, string assetId) {
			this.assetName = assetName;
			this.assetType = assetType;
			this.assetPath = assetPath;
			this.assetId = assetId;
		}
		
		public override string ToString () {
			return "assetName:" + assetName + " assetType:" + assetType + " assetPath:" + assetPath + " assetId:" + assetId;
		}
	}
}