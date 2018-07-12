using Jint.Runtime.Interop;
using System;
using System.Collections.Generic;

namespace Nursery.Utility {
	public class JintWrapper : IJSWrapper {
		private Jint.Engine _engine = new Jint.Engine();
		private List<string> _types = new List<string>();
		private Dictionary<string, Jint.Native.JsValue> _functions = new Dictionary<string, Jint.Native.JsValue>();

		public void SetType(string Name, System.Type Type) {
			if (this._types.Contains(Name)) { return; }
			try {
				this._engine.SetValue(Name, TypeReference.CreateTypeReference(_engine, Type));
				this._types.Add(Name);
			} catch (Exception e) {
				// TRANSLATORS: Log message. In JintWrapper. {0} is name of Type.
				Logger.Log(T._("Could not set Type [{0}].", Name));
				Logger.DebugLog(e.ToString());
			}
		}

		public void SetFunction(string Name, string Source) {
			try {
				var func = this._engine.Execute(Source).GetValue(Name);
				this._functions[Name] = func;
			} catch (Exception e) {
				// TRANSLATORS: Log message. In JintWrapper. {0} is name of Function.
				Logger.Log(T._("Could not compile function [{0}].", Name));
				Logger.DebugLog(e.ToString());
			}
		}

		public object ExecuteFunction(string Name, object arg) {
			if (!this._functions.ContainsKey(Name)) {
				// TRANSLATORS: Log message. In JintWrapper. {0} is name of Function.
				Logger.Log(T._("Could not find function [{0}].", Name));
				return null;
			}
			try {
				this._engine.SetValue(Name + "__wrapper__arg__", arg);
				var arg_jsval = this._engine.GetValue(Name + "__wrapper__arg__");
				if (arg_jsval.IsUndefined()) { return null; }
				return this._functions[Name].Invoke(arg_jsval).ToObject();
			} catch (Exception e) {
				// TRANSLATORS: Log message. In JintWrapper. {0} is name of Function.
				Logger.Log(T._("Could not execute function [{0}].", Name));
				Logger.DebugLog(e.ToString());
				return null;
			}
		}

		public void Reset() {
			this._types.Clear();
			this._functions.Clear();
			this._engine = new Jint.Engine();
		}
	}
}
