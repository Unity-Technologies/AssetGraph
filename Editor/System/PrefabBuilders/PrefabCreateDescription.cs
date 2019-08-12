using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

using Model=Unity.AssetGraph.DataModel.Version2;
using Object = UnityEngine.Object;

namespace Unity.AssetGraph {

	public class PrefabCreateDescription
	{
		/// <summary>
		/// Asset path to creating prefab.
		/// </summary>
		public string prefabName;
		
		/// <summary>
		/// Additional objects to take into account other than given objects from node, such as objects assigned via inspector.
		/// </summary>
		public List<UnityEngine.Object> additionalObjects;

		public PrefabCreateDescription()
		{
			additionalObjects = new List<UnityEngine.Object>(32);
		}

		public void Reset()
		{
			prefabName = string.Empty;
			additionalObjects.Clear();
		}
	}
}