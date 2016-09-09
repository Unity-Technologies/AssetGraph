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

		private const string ID = "id";
		private const string LABEL = "label";
		private const string PRIORITY = "orderPriority";
		private const string SHOWLABEL = "showLabel";

		private string id;
		private string label;
//		private int orderPriority;
//		private bool showLabel;

		public ConnectionPointData(string id, string label/*, int orderPriority, bool showLabel */) {
			this.id = id;
			this.label = label;
//			this.orderPriority = orderPriority;
//			this.showLabel = showLabel;
		}

		public ConnectionPointData(ConnectionPointGUI pointGui) {
			this.id = pointGui.pointId;
			this.label = pointGui.label;
//			this.orderPriority = pointGui.orderPriority;
//			this.showLabel = pointGui.showLabel;
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
//		public int OrderPriority {
//			get {
//				return orderPriority;
//			}
//		}
//		public bool ShowLabel {
//			get {
//				return showLabel;
//			}
//		}

		public Dictionary<string, object> ToJsonDictionary() {
			return null;
		}
	}
}
