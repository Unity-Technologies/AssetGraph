using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	public class BatchBuildConfig : ScriptableObject {

		private const int VERSION = 1;

		[Serializable]
		public class GraphCollection {
			[SerializeField] private string m_name;
			[SerializeField] private List<string> m_graphGuids;

			public GraphCollection(string name) {
				m_name = name;
				m_graphGuids = new List<string>();
			}

			public string Name {
				get {
					return m_name;
				}
				set {
					m_name = value;
				}
			}

			public List<string> GraphGUIDs {
				get {
					return m_graphGuids;
				}
			}
		}

		[SerializeField] private List<GraphCollection> m_collections;
		[SerializeField] private int m_version;

		private static BatchBuildConfig s_config;

		public static BatchBuildConfig GetConfig() {
			if(s_config == null) {
				if(!Load()) {
					// Create vanilla db
					s_config = ScriptableObject.CreateInstance<BatchBuildConfig>();
					s_config.m_collections = new List<GraphCollection>();
					s_config.m_version = VERSION;

                    var SettingDir = Model.Settings.Path.SettingFilePath;

					if (!Directory.Exists(SettingDir)) {
						Directory.CreateDirectory(SettingDir);
					}

                    AssetDatabase.CreateAsset(s_config, Model.Settings.Path.BatchBuildConfigPath);
				}
			}

			return s_config;
		}

		private static bool Load() {

			bool loaded = false;

			try {
                var configPath = Model.Settings.Path.BatchBuildConfigPath;
				
				if(File.Exists(configPath)) 
				{
					BatchBuildConfig db = AssetDatabase.LoadAssetAtPath<BatchBuildConfig>(configPath);
					if(db.m_version == VERSION) {
						s_config = db;
						loaded = true;
					}
				}
			} catch(Exception e) {
				LogUtility.Logger.LogWarning(LogUtility.kTag, e);
			}

			return loaded;
		}

		public static void SetConfigDirty() {
			EditorUtility.SetDirty(s_config);
		}

		public List<GraphCollection> GraphCollections {
			get {
				return m_collections;
			}
		}

		public GraphCollection Find(string name) {
			return m_collections.Find(c => c.Name == name);
		}
	}
}

