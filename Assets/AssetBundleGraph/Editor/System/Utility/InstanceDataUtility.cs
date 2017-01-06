using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AssetBundleGraph {
	public static class InstanceDataUtility<T> where T : class {

		public static T CreateInstance(NodeData node, BuildTargetGroup targetGroup) {

			var data  = node.InstanceData[targetGroup];
			var className = node.ScriptClassName;
			Type dataType = null;

			if(!string.IsNullOrEmpty(className)) {
				dataType = Type.GetType(className);
			}

			if(data != null && dataType != null) {
				return JsonUtility.FromJson(data, dataType) as T;
			}

			return null;
		}
	}
}
