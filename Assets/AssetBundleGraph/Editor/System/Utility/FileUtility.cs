using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;


namespace AssetBundleGraph {
	public class FileUtility {
		public static void RemakeDirectory (string localFolderPath) {
			if (Directory.Exists(localFolderPath)) Directory.Delete(localFolderPath, true);
			Directory.CreateDirectory(localFolderPath);
		}
		
		/**
			default is "overwrite same path" by filepath.
		*/
		public static void CopyFileFromGlobalToLocal (string absoluteSourceFilePath, string localTargetFilePath) {
			var parentDirectoryPath = Path.GetDirectoryName(localTargetFilePath);
			Directory.CreateDirectory(parentDirectoryPath);
			File.Copy(absoluteSourceFilePath, localTargetFilePath, true);
		}

		public static void DeleteFileThenDeleteFolderIfEmpty (string localTargetFilePath) {
			
			File.Delete(localTargetFilePath);
			File.Delete(localTargetFilePath + AssetBundleGraphSettings.UNITY_METAFILE_EXTENSION);
			var directoryPath = Directory.GetParent(localTargetFilePath).FullName;
			var restFiles = FilePathsInFolder(directoryPath);
			if (!restFiles.Any()) {
				Directory.Delete(directoryPath, true);
				File.Delete(directoryPath + AssetBundleGraphSettings.UNITY_METAFILE_EXTENSION);
			}
		}

		public static List<string> FilePathsOfFile (string filePath) {
			var folderPath = Path.GetDirectoryName(filePath);
			var results = FilePathsInFolder(folderPath);
			return results;
		}

		public static List<string> FilePathsInFolder (string localFolderPath) {
			var filePaths = new List<string>();
			
			if (string.IsNullOrEmpty(localFolderPath)) return filePaths;
			if (!Directory.Exists(localFolderPath)) return filePaths;

			GetFilePathsRecursive(localFolderPath, filePaths);
			
			return filePaths;
		}

		private static void GetFilePathsRecursive (string localFolderPath, List<string> filePaths) {
			var folders = Directory.GetDirectories(localFolderPath);
			
			foreach (var folder in folders) {
				GetFilePathsRecursive(folder, filePaths);
			}

			var files = FilePathsInFolderOnly1Level(localFolderPath);
			filePaths.AddRange(files);
		}

		public static List<string> FolderPathsInFolder (string path) {
			// change platform-depends folder delimiter -> '/'
			return ConvertSeparater(Directory.GetDirectories(path).ToList());
		}

		/**
			returns file paths which are located in the folder.

			this method is main point for supporting path format of cross platform.

			Usually Unity Editor uses '/' as folder delimter.

			e.g.
				Application.dataPath returns 
					C:/somewhere/projectPath/Assets @ Windows.
						or
					/somewhere/projectPath/Assets @ Mac, Linux.


			but "Directory.GetFiles(localFolderPath + "/")" method returns different formatted path by platform.

			@ Windows:
				localFolderPath + / + somewhere\folder\file.extention

			@ Mac/Linux:
				localFolderPath + / + somewhere/folder/file.extention

			the problem is, "Directory.GetFiles" returns mixed format path of files @ Windows.
			this is the point of failure.

			this method replaces folder delimiters to '/'.
		*/
		public static List<string> FilePathsInFolderOnly1Level (string localFolderPath) {
			// change platform-depends folder delimiter -> '/'
			var filePaths = ConvertSeparater(Directory.GetFiles(localFolderPath)
					.Where(path => !(Path.GetFileName(path).StartsWith(AssetBundleGraphSettings.DOTSTART_HIDDEN_FILE_HEADSTRING)))
					.ToList());

			if (AssetBundleGraphSettings.IGNORE_META) filePaths = filePaths.Where(path => !FileUtility.IsMetaFile(path)).ToList();

			return filePaths;
		}

		public static List<string> ConvertSeparater (List<string> source) {
			return source.Select(filePath => filePath.Replace(Path.DirectorySeparatorChar.ToString(), AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())).ToList();
		}

		/**
			create combination of path.

			delimiter is always '/'.
		*/
		public static string PathCombine (params string[] paths) {
			if (paths.Length < 2) {
				throw new ArgumentException("Argument must contain at least 2 strings to combine.");
			}

			var combinedPath = _PathCombine(paths[0], paths[1]);
			var restPaths = new string[paths.Length-2];

			Array.Copy(paths, 2, restPaths, 0, restPaths.Length);
			foreach (var path in restPaths) combinedPath = _PathCombine(combinedPath, path);

			return combinedPath;
		}

		private static string _PathCombine (string head, string tail) {
			if (!head.EndsWith(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())) head = head + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
			
			if (string.IsNullOrEmpty(tail)) return head;
			if (tail.StartsWith(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())) tail = tail.Substring(1);

			return Path.Combine(head, tail);
		}

		public static string GetPathWithProjectPath (string pathUnderProjectFolder) {
			var assetPath = Application.dataPath;
			var projectPath = Directory.GetParent(assetPath).ToString();
			return FileUtility.PathCombine(projectPath, pathUnderProjectFolder);
		}

		public static string GetPathWithAssetsPath (string pathUnderAssetsFolder) {
			var assetPath = Application.dataPath;
			return FileUtility.PathCombine(assetPath, pathUnderAssetsFolder);
		}

		public static string ProjectPathWithSlash () {
			var assetPath = Application.dataPath;
			return Directory.GetParent(assetPath).ToString() + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
		}

		public static bool IsMetaFile (string filePath) {
			if (filePath.EndsWith(AssetBundleGraphSettings.UNITY_METAFILE_EXTENSION)) return true;
			return false;
		}

		public static bool ContainsHiddenFiles (string filePath) {
			var pathComponents = filePath.Split(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR);
			foreach (var path in pathComponents) {
				if (path.StartsWith(AssetBundleGraphSettings.DOTSTART_HIDDEN_FILE_HEADSTRING)) return true;
			}
			return false;
		}

		public static string EnsurePrefabricatorCacheDirExists(BuildTarget t, NodeData node) {
			var cacheDir = FileUtility.PathCombine(AssetBundleGraphSettings.PREFABRICATOR_CACHE_PLACE, node.Id, SystemDataUtility.GetPathSafeTargetName(t));

			if (!Directory.Exists(cacheDir)) {
				Directory.CreateDirectory(cacheDir);
			}
			if (!cacheDir.EndsWith(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())) {
				cacheDir = cacheDir + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString();
			}
			return cacheDir;
		}


		public static string EnsureAssetBundleCacheDirExists(BuildTarget t, NodeData node) {
			var cacheDir = FileUtility.PathCombine(AssetBundleGraphSettings.BUNDLEBUILDER_CACHE_PLACE, node.Id, SystemDataUtility.GetPathSafeTargetName(t));

			if (!Directory.Exists(cacheDir)) {
				Directory.CreateDirectory(cacheDir);
			}
			return cacheDir;
		}
	}
}