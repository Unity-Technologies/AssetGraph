using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;


namespace AssetGraph {
	public class FileController {
		public static void RemakeDirectory (string localFolderPath) {
			if (Directory.Exists(localFolderPath)) Directory.Delete(localFolderPath, true);
			Directory.CreateDirectory(localFolderPath);
		}
		
		public static void CopyFileFromGlobalToLocal (string absoluteSourceFilePath, string localTargetFilePath) {
			var parentDirectoryPath = Path.GetDirectoryName(localTargetFilePath);
			Directory.CreateDirectory(parentDirectoryPath);
			FileUtil.CopyFileOrDirectory(absoluteSourceFilePath, localTargetFilePath);
		}


		public static List<string> FilePathsInFolderWithoutMeta (string localFolderPath) {
			var filePaths = new List<string>();
			
			if (string.IsNullOrEmpty(localFolderPath)) return filePaths;

			GetFilePathsRecursive(localFolderPath, filePaths);
			return filePaths
				.Where(path => !path.EndsWith(AssetGraphSettings.UNITY_METAFILE_EXTENSION))
				.Where(path => !(Path.GetFileName(path).StartsWith(AssetGraphSettings.DOTSTART_HIDDEN_FILE_HEADSTRING)))
				.ToList();
		}

		public static List<string> FilePathsInFolderWithoutMetaOnly1Level (string localFolderPath) {
			var filePaths = Directory.GetFiles(localFolderPath);
			return filePaths
				.Where(path => !path.EndsWith(AssetGraphSettings.UNITY_METAFILE_EXTENSION))
				.Where(path => !(Path.GetFileName(path).StartsWith(AssetGraphSettings.DOTSTART_HIDDEN_FILE_HEADSTRING)))
				.ToList();
		}

		private static void GetFilePathsRecursive (string localFolderPath, List<string> filePaths) {
			var folders = Directory.GetDirectories(localFolderPath);
			foreach (var folder in folders) {
				GetFilePathsRecursive(folder, filePaths);
			}

			var files = Directory.GetFiles(localFolderPath);
			filePaths.AddRange(files);
		}

		/**
			create path combination.

			delimiter is always '/'. and '\' will be replaced with '/'.

			in windows, Application.dataPath returns PATH_OF_PROJECT/Assets with slashes(/). 
			like below.
				C:/SOMEWHERE/PROJECT_FOLDER/Assets

			we follow that.
		*/
		public static string PathCombine (params string[] paths) {
			if (paths.Length < 2) throw new Exception("failed to combine paths: only 1 path.");

			var combinedPath = Path.Combine(paths[0], paths[1]);
			var restPaths = new string[paths.Length-2];

			Array.Copy(paths, 2, restPaths, 0, restPaths.Length);
			foreach (var path in restPaths) combinedPath = _PathCombine(combinedPath, path);

			return combinedPath;
		}

		private static string _PathCombine (string head, string tail) {
			if (!head.EndsWith(AssetGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())) head = head + AssetGraphSettings.UNITY_FOLDER_SEPARATOR;

			if (tail.Contains("\\")) tail = tail.Replace("\\", AssetGraphSettings.UNITY_FOLDER_SEPARATOR.ToString());
			if (tail.StartsWith(AssetGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())) tail = tail.Substring(1);

			return Path.Combine(head, tail);
		}
	}
}