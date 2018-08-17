using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nursery.Utility;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nursery.Options {
	public class CommandlineOptions {
		[Option('c', "config", Required = false, HelpText = "Path of config file.")]
		public string ConfigFile { get; set; }

		[Option('d', "debug", Required = false, HelpText = "Debug flag.")]
		public bool Debug { get; set; } = false;
	}

	[JsonObject("config")]
	public class MainConfig {
		[JsonProperty("token")]
		public string Token { get; set; } = "";
		[JsonProperty("device_read")]
		public string DeviceRead { get; set; } = "";
		[JsonProperty("device_sound_effect")]
		public string DeviceSoundEffect { get; set; } = "";
		[JsonProperty("bouyomichan_path")]
		public string BouyomichanPath { get; set; } = "";
		[JsonProperty("plugins_dir")]
		public string PluginDir { get; set; } = "plugins";
		[JsonIgnore]
		public bool Debug { get; set; } = false;
	}

	[JsonObject("config")]
	public class PluginConfig {
		[JsonProperty("plugins")]
		public string[] PluginNames { get; set; } = new string[]{};
	}

	public class Config {
		private const string DEFAULT_CONFIG_PATH = ".\\config.json";
		private string path;

		public MainConfig MainConfig { get; set; } = null;
		public PluginConfig PluginConfig { get; set; } = null;

		#region Singleton code

		private static Config instance = null;

		public static Config Instance {
			get {
				if (instance == null) {
					// TRANSLATORS: Error message. In Config.
					throw new Exception(T._("Please Initialize before get instance."));
				}
				return instance;
			}
		}

		public static void Initialize(CommandlineOptions opts) {
			if (instance == null) {
				instance = new Config(opts.ConfigFile);
			}
		}

		#endregion

		private Config(string filepath) {
			if (filepath == null || filepath.Length == 0) { filepath = DEFAULT_CONFIG_PATH; }
			if (!File.Exists(filepath)) {
				filepath = Path.Combine(Directory.GetCurrentDirectory(), filepath);
			}
			if (!File.Exists(filepath)) {
				// TRANSLATORS: Error message. In Config. {0} is file path.
				throw new Exception(T._("Config file [{0}] is not found.", filepath));
			}
			this.path = filepath;
		}

		public TConfig LoadConfig<TConfig>(string path) {
			if (!File.Exists(path)) {
				path = Path.Combine(Directory.GetCurrentDirectory(), path);
			}
			if (!File.Exists(path)) {
				// TRANSLATORS: Error message. In Config. {0} is file path.
				throw new FileNotFoundException(T._("Config file [{0}] is not found.", path));
			}
			using (StreamReader sr = new StreamReader(path, new System.Text.UTF8Encoding(true))) {
				return JsonConvert.DeserializeObject<TConfig>(sr.ReadToEnd());
			}
		}

		public void Load() {
			this.MainConfig = LoadConfig<MainConfig>(this.path);
			string path = Path.Combine(this.MainConfig.PluginDir, "plugins.json");
			try {
				this.PluginConfig = LoadConfig<PluginConfig>(path);
			} catch (Exception e) {
				Logger.Log(e.ToString());
				this.PluginConfig = new PluginConfig();
			}
		}

		public TConfig GetPluginSetting<TConfig>(string PluginName) {
			string path = Path.Combine(this.MainConfig.PluginDir, PluginName + ".json");
			return LoadConfig<TConfig>(path);
		}
	}
}
