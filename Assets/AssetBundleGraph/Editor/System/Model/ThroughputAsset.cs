using System;

namespace AssetBundleGraph {
	public struct DepreacatedThroughputAsset {
		public readonly string path;
		public readonly bool isBundled;

		public DepreacatedThroughputAsset (string path, bool isBundled) {
			this.path = path;
			this.isBundled = isBundled;
		}
	}
}
