using System;

namespace Nursery.Utility {
	public class Logger {
		#region Singleton
		private static Logger _instance = null;

		public static Logger Instance {
			get {
				if (_instance == null) {
					_instance = new Logger();
				}
				return _instance;
			}
		}

		private Logger() { }
		#endregion

		public static void SetDebug(bool debug) {
			Instance.Debug = debug;
		}

		public static void Log(string message) {
			Instance.DoLog(message);
		}

		public static void DebugLog(string message) {
			Instance.DoDebugLog(message);
		}

		private bool Debug { get; set; } = true;

		private void DoLog(string message) {
			Console.WriteLine(message);
		}

		private void DoDebugLog(string message) {
			if (Debug) { Console.WriteLine(message); }
		}
	}
}
