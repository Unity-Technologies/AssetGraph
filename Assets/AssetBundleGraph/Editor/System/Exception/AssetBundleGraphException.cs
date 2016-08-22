using System;
using UnityEngine;
using System.Collections;

namespace AssetBundleGraph {

	public class AssetBundleGraphException : Exception {

		public AssetBundleGraphException(string message) : base(message) {
		}
	}

	public class AssetBundleGraphSetupException : AssetBundleGraphException {

		public AssetBundleGraphSetupException(string message) : base(message) {
		}
	}

	public class AssetBundleGraphBuildException : AssetBundleGraphException {
		public AssetBundleGraphBuildException(string message) : base(message) {
		}
	}
}