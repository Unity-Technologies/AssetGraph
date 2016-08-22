using UnityEditor;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public class AssetBundleGraphPlatformSettings {
		public static List<string> platforms = new List<string> {
			"Web",
			"Standalone",
			"iPhone",
			"Android",
			// "BlackBerry",
			// "Tizen",
			// "XBox360",
			// "XboxOne",
			// "PS3",
			// "PSP2",
			// "PS4",
			// "StandaloneGLESEmu",
			// "Metro",
			// "WP8",
			"WebGL",
			// "SamsungTV"
		};

		public static string BuildTargetToHumaneString(UnityEditor.BuildTarget t) {

			switch(t) {
			case BuildTarget.Android:
				return "Android";
			case BuildTarget.iOS:
				return "iOS";
			case BuildTarget.Nintendo3DS:
				return "Nintendo 3DS";
			case BuildTarget.PS3:
				return "PlayStation 3";
			case BuildTarget.PS4:
				return "PlayStation 4";
			case BuildTarget.PSM:
				return "PlayStation Mobile";
			case BuildTarget.PSP2:
				return "PlayStation Vita";
			case BuildTarget.SamsungTV:
				return "Samsung TV";
			case BuildTarget.StandaloneLinux:
				return "Linux Standalone";
			case BuildTarget.StandaloneLinux64:
				return "Linux Standalone(64-bit)";
			case BuildTarget.StandaloneLinuxUniversal:
				return "Linux Standalone(Universal)";
			case BuildTarget.StandaloneOSXIntel:
				return "OSX Standalone";
			case BuildTarget.StandaloneOSXIntel64:
				return "OSX Standalone(64-bit)";
			case BuildTarget.StandaloneOSXUniversal:
				return "OSX Standalone(Universal)";
			case BuildTarget.StandaloneWindows:
				return "Windows Standalone";
			case BuildTarget.StandaloneWindows64:
				return "Windows Standalone(64-bit)";
			case BuildTarget.Tizen:
				return "Tizen";
			case BuildTarget.tvOS:
				return "tvOS";
			case BuildTarget.WebGL:
				return "WebGL";
			case BuildTarget.WiiU:
				return "Wii U";
			case BuildTarget.WSAPlayer:
				return "Windows Store Apps";
			case BuildTarget.XBOX360:
				return "Xbox 360";
			case BuildTarget.XboxOne:
				return "Xbox One";
			default:
				return t.ToString() + "(deprecated)";
			}

		}
	}
}