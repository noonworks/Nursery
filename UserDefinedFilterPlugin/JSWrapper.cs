using System;
using Nursery.Utility;

namespace Nursery.UserDefinedFilterPlugin {
	class JSWrapper : IJSWrapper {
		private static JSWrapper _instance;
		public static JSWrapper Instance {
			get {
				if (_instance == null) { _instance = new JSWrapper(); }
				return _instance;
			}
		}

		private Utility.IJSWrapper _wrapper = new JintWrapper();

		public void SetType(string Name, Type Type) {
			this._wrapper.SetType(Name, Type);
		}

		public void SetFunction(string Name, string Source) {
			this._wrapper.SetFunction(Name, Source);
		}

		public object ExecuteFunction(string Name, object arg) {
			return this._wrapper.ExecuteFunction(Name, arg);
		}

		public void Reset() {
			this._wrapper.Reset();
		}
	}
}
