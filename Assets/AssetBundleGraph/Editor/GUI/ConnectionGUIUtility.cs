using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	public class ConnectionGUIUtility {

		public static Action<ConnectionEvent> ConnectionEventHandler {
			get {
				return ConnectionGUISingleton.s.emitAction;
			}
			set {
				ConnectionGUISingleton.s.emitAction = value;
			}
		}

		private class ConnectionGUISingleton {
			public Action<ConnectionEvent> emitAction;

			private static ConnectionGUISingleton s_singleton;

			public static ConnectionGUISingleton s {
				get {
					if( s_singleton == null ) {
						s_singleton = new ConnectionGUISingleton();
					}

					return s_singleton;
				}
			}
		}
	}
}
