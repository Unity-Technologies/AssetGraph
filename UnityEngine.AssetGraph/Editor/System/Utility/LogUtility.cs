using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

namespace UnityEngine.AssetGraph {

	public class LogUtility {

//		class MyLogHandler : ILogHandler
//		{
//			public void LogFormat (LogType logType, UnityEngine.Object context, string format, params object[] args)
//			{
//				Debug.logger.logHandler.LogFormat (logType, context, format, args);
//			}
//
//			public void LogException (Exception exception, UnityEngine.Object context)
//			{
//				Debug.logger.LogException (exception, context);
//			}
//		}

		public static readonly string kTag = "AssetGraph";

		private static Logger s_logger;

		public static Logger Logger {
			get {
				if(s_logger == null) {
					#if UNITY_2017_1_OR_NEWER
					s_logger = new Logger(Debug.unityLogger.logHandler);
					#else
					s_logger = new Logger(Debug.logger.logHandler);
					#endif
                    ShowVerboseLog (UserPreference.DefaultVerboseLog);
				}

				return s_logger;
			}
		}

        public static void ShowVerboseLog(bool bVerbose) {
            var curValue = (bVerbose)? LogType.Log : LogType.Warning;
            if (curValue != Logger.filterLogType) {
                Logger.filterLogType = curValue;
            }
        }
	}
}
