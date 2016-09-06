using UnityEditor;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public class AssetBundleGraphPlatformSettings {

		public const BuildTargetGroup DefaultTarget = BuildTargetGroup.Unknown;

		/**
		 *  from build target to human friendly string for display purpose.
		 */ 
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

		/**
		 *  from build target group to human friendly string for display purpose.
		 */ 
		public static string BuildTargetGroupToHumaneString(UnityEditor.BuildTargetGroup g) {

			switch(g) {
			case BuildTargetGroup.Android:
				return "Android";
			case BuildTargetGroup.iOS:
				return "iOS";
			case BuildTargetGroup.Nintendo3DS:
				return "Nintendo 3DS";
			case BuildTargetGroup.PS3:
				return "PlayStation 3";
			case BuildTargetGroup.PS4:
				return "PlayStation 4";
			case BuildTargetGroup.PSM:
				return "PlayStation Mobile";
			case BuildTargetGroup.PSP2:
				return "PlayStation Vita";
			case BuildTargetGroup.SamsungTV:
				return "Samsung TV";
			case BuildTargetGroup.Standalone:
				return "PC/Mac/Linux Standalone";
			case BuildTargetGroup.Tizen:
				return "Tizen";
			case BuildTargetGroup.tvOS:
				return "tvOS";
			case BuildTargetGroup.WebGL:
				return "WebGL";
			case BuildTargetGroup.WiiU:
				return "Wii U";
			case BuildTargetGroup.WSA:
				return "Windows Store Apps";
			case BuildTargetGroup.XBOX360:
				return "Xbox 360";
			case BuildTargetGroup.XboxOne:
				return "Xbox One";
			case BuildTargetGroup.Unknown:
				return "Unknown";
			default:
				return g.ToString() + "(deprecated)";
			}
		}


		public static BuildTargetGroup BuildTargetToBuildTargetGroup(UnityEditor.BuildTarget t) {

			switch(t) {
			case BuildTarget.Android:
				return BuildTargetGroup.Android;
			case BuildTarget.iOS:
				return BuildTargetGroup.iOS;
			case BuildTarget.Nintendo3DS:
				return BuildTargetGroup.Nintendo3DS;
			case BuildTarget.PS3:
				return BuildTargetGroup.PS3;
			case BuildTarget.PS4:
				return BuildTargetGroup.PS4;
			case BuildTarget.PSM:
				return BuildTargetGroup.PSM;
			case BuildTarget.PSP2:
				return BuildTargetGroup.PSP2;
			case BuildTarget.SamsungTV:
				return BuildTargetGroup.SamsungTV;
			case BuildTarget.StandaloneLinux:
			case BuildTarget.StandaloneLinux64:
			case BuildTarget.StandaloneLinuxUniversal:
			case BuildTarget.StandaloneOSXIntel:
			case BuildTarget.StandaloneOSXIntel64:
			case BuildTarget.StandaloneOSXUniversal:
			case BuildTarget.StandaloneWindows:
			case BuildTarget.StandaloneWindows64:
				return BuildTargetGroup.Standalone;
			case BuildTarget.Tizen:
				return BuildTargetGroup.Tizen;
			case BuildTarget.tvOS:
				return BuildTargetGroup.tvOS;
			case BuildTarget.WebGL:
				return BuildTargetGroup.WebGL;
			case BuildTarget.WiiU:
				return BuildTargetGroup.WiiU;
			case BuildTarget.WSAPlayer:
				return BuildTargetGroup.WSA;
			case BuildTarget.XBOX360:
				return BuildTargetGroup.XBOX360;
			case BuildTarget.XboxOne:
				return BuildTargetGroup.XboxOne;
			default:
				return BuildTargetGroup.Unknown;
			}
		}
	
	}
}