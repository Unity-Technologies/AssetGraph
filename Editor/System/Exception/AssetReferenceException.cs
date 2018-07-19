using System;
using UnityEngine;
using System.Collections;

namespace UnityEngine.AssetGraph {

	public class AssetReferenceException : Exception {
		public readonly string importFrom;
		public AssetReferenceException(string importFrom, string message) : base(message) {
			this.importFrom = importFrom;
		}
	}
}