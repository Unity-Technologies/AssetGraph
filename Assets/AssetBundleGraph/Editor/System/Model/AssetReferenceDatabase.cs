using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

namespace AssetBundleGraph {
	public class AssetReferenceDatabase : ScriptableObject {

		private const int DB_VERSION = 1;

		private delegate AssetReference CreateReference();

		[SerializeField] private List<AssetReference> m_assets;
		[SerializeField] private int m_version;
		private SortedList<string, AssetReference> m_dictionary;

		private static AssetReferenceDatabase s_database;

		public static AssetReferenceDatabase GetDatabase() {
			if(s_database == null) {
				if(!Load()) {
					// Create vanilla db
					s_database = ScriptableObject.CreateInstance<AssetReferenceDatabase>();
					s_database.m_assets = new List<AssetReference>();
					s_database.m_dictionary = new SortedList<string, AssetReference>();
					s_database.m_version = DB_VERSION;

					var DBDir = FileUtility.PathCombine("Assets/", 
						AssetBundleGraphSettings.ASSETNBUNDLEGRAPH_DATA_PATH);

					if (!Directory.Exists(DBDir)) {
						Directory.CreateDirectory(DBDir);
					}

					AssetDatabase.CreateAsset(s_database, DBPath);
				}
			}

			return s_database;
		}

		private static string DBPath {
			get {
				return FileUtility.PathCombine("Assets/", 
					AssetBundleGraphSettings.ASSETNBUNDLEGRAPH_DATA_PATH,
					AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATABASE_NAME);
				
			}
		}

		private static bool Load() {

			bool loaded = false;

			try {
				var dbPath = DBPath;
				
				if(File.Exists(dbPath)) 
				{
					AssetReferenceDatabase db = AssetDatabase.LoadAssetAtPath<AssetReferenceDatabase>(dbPath);

					if(db != null && db.m_version == DB_VERSION) {
						db.m_dictionary = new SortedList<string, AssetReference>();

						foreach(var r in db.m_assets) {
							db.m_dictionary.Add(r.importFrom, r);
						}

						s_database = db;
						loaded = true;
					}
				}
			} catch(Exception e) {
				LogUtility.Logger.LogWarning(LogUtility.kTag, e);
			}

			return loaded;
		}

		public static void SetDBDirty() {
			EditorUtility.SetDirty(s_database);
		}

		public static AssetReference GetReference(string relativePath) {
			return GetReference(relativePath, () => { return AssetReference.CreateReference(relativePath); });
		}

		public static AssetReference GetPrefabReference(string relativePath) {
			return GetReference(relativePath, () => { return AssetReference.CreatePrefabReference(relativePath); });
		}

		public static AssetReference GetAssetBundleReference(string relativePath) {
			return GetReference(relativePath, () => { return AssetReference.CreateAssetBundleReference(relativePath); });
		}

		public static void DeleteReference(string relativePath) {
			AssetReferenceDatabase db = GetDatabase();

			if(db.m_dictionary.ContainsKey(relativePath)) {
				var r = db.m_dictionary[relativePath];
				db.m_assets.Remove(r);
				db.m_dictionary.Remove(relativePath);
				SetDBDirty();
			}
		}

		public static void MoveReference(string oldPath, string newPath) {
			AssetReferenceDatabase db = GetDatabase();

			if(db.m_dictionary.ContainsKey(oldPath)) {
				var r = db.m_dictionary[oldPath];
				db.m_dictionary.Remove(oldPath);
				db.m_dictionary[newPath]= r;
				r.importFrom = newPath;
			}
		}

		private static AssetReference GetReference(string relativePath, CreateReference creator) {
			AssetReferenceDatabase db = GetDatabase();

			if(db.m_dictionary.ContainsKey(relativePath)) {
				return db.m_dictionary[relativePath];
			} else {
				try {
					AssetReference r = creator();
					db.m_assets.Add(r);
					db.m_dictionary.Add(relativePath, r);
					AssetReferenceDatabase.SetDBDirty();
					return r;
				} catch(AssetReferenceException ) {
					// if give asset is invalid, return null
					return null;
				}
			}
		}
	}
}

