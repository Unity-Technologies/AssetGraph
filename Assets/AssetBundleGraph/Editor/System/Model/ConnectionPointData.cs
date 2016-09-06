using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AssetBundleGraph {

	public class ConnectionPointData {
		private string id;
		private string label;

		public ConnectionPointData(string id, string label) {
			this.id = id;
			this.label = label;
		}

		public string Id {
			get {
				return id;
			}
		}

		public string Label {
			get {
				return label;
			}
		}
	}
}
