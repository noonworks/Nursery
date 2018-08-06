using Discord.WebSocket;
using FNF.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Nursery.Options;
using Nursery.Utility;
using System.Diagnostics;

namespace Nursery.Plugins {
	public class Message : IMessage {
		public SocketMessage Original { get; }
		public string Content { get; set; }
		public List<string> AppliedPlugins { get; } = new List<string>();
		public ITalkOptions TalkOptions { get; } = new TalkOptions();
		public bool Terminated { get; set; } = false;

		public Message(SocketMessage message) {
			this.Original = message;
			this.Content = message.Content;
		}
	}

	class PluginManager : IPluginManager {

		#region Singleton code

		private static PluginManager instance = null;

		public static PluginManager Instance {
			get {
				if (instance == null) {
					instance = new PluginManager();
				}
				return instance;
			}
		}

		#endregion

		private string[] GetDllPaths(string dir) {
			if (!Directory.Exists(dir)) {
				dir = Path.Combine(Directory.GetCurrentDirectory(), dir);
			}
			if (!Directory.Exists(dir)) {
				// TRANSLATORS: Error message. In PluginManager. {0} is plugin directory.
				throw new Exception(T._("Plugin directory [{0}] is not found.", dir));
			}
			return System.IO.Directory.GetFiles(dir, "*.dll", System.IO.SearchOption.AllDirectories);
		}

		private IPlugin[] Plugins = new IPlugin[] { };
		private bool Loaded = false;
		private string AnnounceLabel = "";
		private string SpeakLabel = "";

		public IMessage ExecutePlugins(IBot bot, SocketMessage message) {
			var mes = new Plugins.Message(message);
			foreach (var p in Plugins) {
				p.Execute(bot, mes);
				if (mes.Terminated) { break; }
			}
			return mes;
		}

		public IPlugin GetPlugin(string PluginName) {
			var plg = Plugins.FirstOrDefault(p => p.Name.Equals(PluginName));
			if (plg != null) { return plg; }
			return null;
		}

		public T LoadConfig<T>(string path) {
			return Config.Instance.LoadConfig<T>(path);
		}

		public T GetPluginSetting<T>(string PluginName) {
			return Config.Instance.GetPluginSetting<T>(PluginName);
		}

		public void SetAnnounceLabel(string label) {
			this.AnnounceLabel = label;
		}

		public void SetSpeakLabel(string label) {
			this.SpeakLabel = label;
		}

		public void Load(IBot bot) {
			if (Loaded) { return; }
			var pNames = Config.Instance.PluginConfig.PluginNames;
			var foundPlugins = new IPlugin[pNames.Length];
			var dllFiles = GetDllPaths(Config.Instance.MainConfig.PluginDir);
			// load asm and check IPlugin classes
			IPlugin dummy = null;
			foreach (var f in dllFiles) {
				Assembly asm;
				var file_ver = "UNKNOWN";
				var prod_ver = "UNKNOWN";
				var asm_ver = "UNKNOWN";
				try {
					var fv = FileVersionInfo.GetVersionInfo(f);
					file_ver = fv.FileVersion;
					prod_ver = fv.ProductVersion;
					asm = Assembly.LoadFrom(f);
					asm_ver = asm.GetName().Version.ToString();
				} catch (Exception) {
					// TRANSLATORS: Log message. In PluginManager. {0} is dll file which could not load.
					Logger.Log(T._("* Could not load [{0}].", f));
					continue;
				}
				// TRANSLATORS: Log message. In PluginManager. {0} is dll file path.
				Logger.Log(T._("  - Load [{0}] ...", f));
				// Log versions
				Logger.DebugLog("    ProductVersion [" + prod_ver + "] FileVersion [" + file_ver + "] AssemblyVersion [" + asm_ver + "]");
				foreach (var t in asm.GetTypes()) {
					if (t.IsInterface) { continue; }
					dummy = null;
					try {
						dummy = Activator.CreateInstance(t) as IPlugin;
					} catch (Exception) {}
					if (dummy != null) {
						var idx = Array.IndexOf(pNames, dummy.Name);
						if (idx >= 0) {
							foundPlugins[idx] = dummy;
							// TRANSLATORS: Log message. In PluginManager. {0} is plugin name. {1} is plugin index.
							Logger.Log(T._("    - {0} [{1}]", dummy.Name, idx));
						} else {
							// TRANSLATORS: Log message. In PluginManager. {0} is plugin name.
							Logger.Log(T._("    - {0} (disabled)", dummy.Name));
						}
					}
				}
			}
			var ret = foundPlugins.Where(i => i != null).ToArray();
			this.Plugins = ret.Select(p => { p.Initialize(this, ret); return p; }).ToArray();
			bot.AnnounceLabel = this.AnnounceLabel;
			bot.SpeakLabel = this.SpeakLabel;
			Loaded = true;
		}
	}
}
