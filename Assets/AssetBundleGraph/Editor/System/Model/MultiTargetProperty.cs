using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AssetBundleGraph {
	/*
	 * Generic container class to support property per BuildTargetGroup
	 */
	public class MultiTargetProperty<T> {
		private Dictionary<BuildTargetGroup, T> m_property;

		public MultiTargetProperty() {
			m_property = new Dictionary<BuildTargetGroup, T>();
		}

		public MultiTargetProperty(Dictionary<string, object> json) {
			m_property = new Dictionary<BuildTargetGroup, T>();
			foreach (var buildTargetName in json.Keys) {
				try {
					BuildTargetGroup g =  (BuildTargetGroup)Enum.Parse(typeof(BuildTargetGroup), buildTargetName, true);
					T val = (T)json[buildTargetName];
					m_property.Add(g, val);
				} catch(Exception e) {
					Debug.LogWarning("Failed to retrieve MultiTagetProperty. skipping entry - " + buildTargetName + ":" + json[buildTargetName] + " error:" + e.Message);
				}
			}
		}

		public Dictionary<BuildTargetGroup, T>.KeyCollection Keys {
			get {
				return m_property.Keys;
			}
		}

		public Dictionary<BuildTargetGroup, T>.ValueCollection Values {
			get {
				return m_property.Values;
			}
		}

		public T this[BuildTargetGroup index] {
			get {
				if( !m_property.ContainsKey(index) ) {
					return DefaultValue;
				}
				return m_property[index];
			}
			set {
				m_property[index] = value;
			}
		}

		public T this[BuildTarget index] {
			get {
				return this[AssetBundleGraphPlatformSettings.BuildTargetToBuildTargetGroup(index)];
			}
			set {
				this[AssetBundleGraphPlatformSettings.BuildTargetToBuildTargetGroup(index)] = value;
			}
		}

		public T DefaultValue {
			get {
				if( !m_property.ContainsKey(AssetBundleGraphPlatformSettings.DefaultTarget) ) {
					m_property.Add(AssetBundleGraphPlatformSettings.DefaultTarget, default (T));
				}
				return m_property[AssetBundleGraphPlatformSettings.DefaultTarget];
			}
			set {
				m_property[AssetBundleGraphPlatformSettings.DefaultTarget] = value;
			}
		}

		public T CurrentPlatformValue {
			get {
				return m_property[EditorUserBuildSettings.selectedBuildTargetGroup];
			}
		}

		public bool HasOverrideValue(BuildTargetGroup g) {
			return m_property.ContainsKey(g);
		}

		public void Set(BuildTargetGroup g, T value) {
			m_property[g] = value;
		}

		public Dictionary<string, object> ToJsonDictionary() {
			Dictionary<string, object> dic = new Dictionary<string, object>();
			foreach(var v in m_property) {
				dic.Add(v.Key.ToString(), v.Value);
			}
			return dic;
		}
	}
}

//
//public static string GetPlatformValue (Dictionary<string, string> packageDict, string platform) {
//	var key = CreateKeyNameFromString(platform);
//	if (packageDict.ContainsKey(key)) {
//		return packageDict[key];
//	}
//
//	if (packageDict.ContainsKey(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME)) {
//		return packageDict[AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME];
//	}
//
//	throw new AssetBundleGraphException("Default setting not found.");
//}
//
//public static List<string> GetPlatformValue (Dictionary<string, List<string>> packageDict, string platform) {
//	var key = CreateKeyNameFromString(platform);
//	if (packageDict.ContainsKey(key)) {
//		return packageDict[key];
//	}
//
//	if (packageDict.ContainsKey(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME)) {
//		return packageDict[AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME];
//	}
//
//	throw new AssetBundleGraphException("Default setting not found.");
//}
//
//public static List<string> GetCurrentPlatformValue (Dictionary<string, List<string>> packageDict) {
//	var platformPackageKeyCandidate = GetCurrentPlatformKey();
//
//	if (packageDict.ContainsKey(platformPackageKeyCandidate)) {
//		return packageDict[platformPackageKeyCandidate];
//	}
//
//	if (packageDict.ContainsKey(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME)) {
//		return packageDict[AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME];
//	}
//
//	throw new AssetBundleGraphException("Default setting not found.");
//}
//
//public static string GetCurrentPlatformValue (Dictionary<string, string> packageDict) {
//	var platformPackageKeyCandidate = GetCurrentPlatformKey();
//	/*
//				check best match for platform + pacakge.
//			*/
//	if (packageDict.ContainsKey(platformPackageKeyCandidate)) {
//		return packageDict[platformPackageKeyCandidate];
//	}
//
//	/*
//				check next match for defaultPlatform + package.
//			*/
//	var defaultPlatformAndCurrentPackageCandidate = GetDefaultPlatformKey();
//	if (packageDict.ContainsKey(defaultPlatformAndCurrentPackageCandidate)) {
//		return packageDict[defaultPlatformAndCurrentPackageCandidate];
//	}
//
//	/*
//				check default platform.
//			*/
//	if (packageDict.ContainsKey(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME)) {
//		return packageDict[AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME];
//	}
//
//	throw new AssetBundleGraphException("Default setting not found.");
//}
