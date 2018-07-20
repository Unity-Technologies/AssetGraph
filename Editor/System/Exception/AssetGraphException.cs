using System;
using UnityEngine;
using System.Collections;

namespace Unity.AssetGraph {

	public class AssetGraphException : Exception {
		public AssetGraphException(string message) : base(message) {
		}
	}
}