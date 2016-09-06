using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {

	[Serializable] 
	public class SerializableMultiTargetString {

		[Serializable]
		public class Entry {
			[SerializeField] public BuildTargetGroup targetGroup;
			[SerializeField] public string value;

			public Entry(BuildTargetGroup g, string v) {
				targetGroup = g;
				value = v;
			}
		}

		[SerializeField] private List<Entry> m_values;

		public SerializableMultiTargetString(string value) {
			m_values = new List<Entry>();
			this[AssetBundleGraphPlatformSettings.DefaultTarget] = value;
		}

		public SerializableMultiTargetString() {
			m_values = new List<Entry>();
		}

		public SerializableMultiTargetString(MultiTargetProperty<string> property) {
			m_values = new List<Entry>();
			foreach(var k in property.Keys) {
				m_values.Add(new Entry(k, property[k]));
			}
		}

		public List<Entry> Values {
			get {
				return m_values;
			}
		}

		public string this[BuildTargetGroup g] {
			get {
				int i = m_values.FindIndex(v => v.targetGroup == g);
				if(i >= 0) {
					return m_values[i].value;
				} else {
					return String.Empty;
				}
			}
			set {
				int i = m_values.FindIndex(v => v.targetGroup == g);
				if(i >= 0) {
					m_values[i].value = value;
				} else {
					m_values.Add(new Entry(g, value));
				}
			}
		}

		public string this[BuildTarget index] {
			get {
				return this[AssetBundleGraphPlatformSettings.BuildTargetToBuildTargetGroup(index)];
			}
			set {
				this[AssetBundleGraphPlatformSettings.BuildTargetToBuildTargetGroup(index)] = value;
			}
		}

		public string DefaultValue {
			get {
				return this[AssetBundleGraphPlatformSettings.DefaultTarget];
			}
			set {
				this[AssetBundleGraphPlatformSettings.DefaultTarget] = value;
			}
		}

		public string CurrentPlatformValue {
			get {
				return this[EditorUserBuildSettings.selectedBuildTargetGroup];
			}
		}

		public bool ContainsValueOf (BuildTargetGroup group) {
			return m_values.FindIndex(v => v.targetGroup == group) >= 0;
		}

		public void Remove (BuildTargetGroup group) {
			int index = m_values.FindIndex(v => v.targetGroup == group);
			if(index >= 0) {
				m_values.RemoveAt(index);
			}
		}

		public MultiTargetProperty<string> ToProperty () {
			MultiTargetProperty<string> p = new MultiTargetProperty<string>();

			foreach(Entry e in m_values) {
				p.Set(e.targetGroup, e.value);
			}

			return p;
		}
	}
}