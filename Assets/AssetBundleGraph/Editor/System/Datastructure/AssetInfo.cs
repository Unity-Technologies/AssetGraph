using System;

namespace AssetBundleGraph {
	public class DepreacatedAssetInfo {
		public readonly string assetName;
		public readonly Type assetType;
		public readonly string assetPath;
		public readonly string assetDatabaseId;

		public DepreacatedAssetInfo (string assetName, Type assetType, string assetPath, string assetDatabaseId) {
			this.assetName = assetName;
			this.assetType = assetType;
			this.assetPath = assetPath;
			this.assetDatabaseId = assetDatabaseId;
		}
		
		public override string ToString () {
			return "assetName:" + assetName + " assetType:" + assetType + " assetPath:" + assetPath + " assetDatabaseId:" + assetDatabaseId;
		}
	}
}