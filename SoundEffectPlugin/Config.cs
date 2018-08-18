using Newtonsoft.Json;
using Nursery.Options;
using Nursery.Plugins;
using Nursery.Utility;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Nursery.SoundEffectPlugin {
	[JsonObject("Nursery.SoundEffectPlugin.SoundEffectCommand")]
	public class SoundEffectCommandConfig {
		[JsonProperty("dir")]
		public string Dir { get; set; } = "sounds";
		[JsonProperty("memory_max")]
		public long MemoryMax { get; set; } = 100 * 1024 * 1024;
		[JsonProperty("parallel_max")]
		public int MaxPlayInParallel { get; set; } = 10;
	}

	[JsonObject("Nursery.SoundEffectPlugin.SoundConfig")]
	public class SoundConfig : PathHolderConfig {
		[JsonProperty("file")]
		public string File { get; set; } = "";
		[JsonProperty("volume")]
		public float Volume { get; set; } = 0.4f;
		[JsonProperty("names")]
		public List<string> Aliases { get; set; } = new List<string>();
		[JsonProperty("patterns")]
		public List<PatternConfig> Patterns { get; set; } = new List<PatternConfig>();
	}

	[JsonObject("Nursery.SoundEffectPlugin.PatternConfig")]
	public class PatternConfig {
		[JsonProperty("pattern")]
		public string StrPattern { get; set; } = "";
		[JsonProperty("type")]
		public string StrType { get; set; } = "string";
		[JsonProperty("replace_to")]
		public string ReplaceTo { get; set; } = null;
		[JsonProperty("function_name")]
		public string FunctionName { get; set; } = null;
		[JsonProperty("function_file")]
		public string FunctionFile { get; set; } = null;

		[JsonIgnore]
		public PatternType Type { get; private set; } = PatternType.String;
		[JsonIgnore]
		public Regex RegexPattern { get; private set; } = null;
		[JsonIgnore]
		public string StringPattern { get; private set; } = null;
		[JsonIgnore]
		public ReplaceDelegate FunctionPattern { get; private set; } = null;

		private void SetType() {
			switch (this.StrType.ToLower()) {
				case "regex":
					this.Type = PatternType.Regex;
					break;
				case "function":
					this.Type = PatternType.Function;
					break;
				default:
					this.Type = PatternType.String;
					break;
			}
		}

		private void SetPattern(SoundConfig parent) {
			switch (this.Type) {
				case PatternType.String:
					StringPattern = StrPattern;
					break;
				case PatternType.Regex:
					RegexPattern = new Regex(StrPattern);
					break;
				case PatternType.Function:
					var file = Config.LoadFile(this.FunctionFile, parent.ConfigFileDir);
					if (file.Length > 0) { this.StrPattern = file; }
					if (FunctionName.Length == 0 || StrPattern.Length == 0) {
						// TRANSLATORS: Log message. SoundEffectCommand plugin.
						Logger.Log(T._("Could not set function."));
						break;
					}
					JSWrapper.Instance.SetFunction(FunctionName, StrPattern);
					FunctionPattern = (JSArgument arg) => {
						try {
							var r = JSWrapper.Instance.ExecuteFunction(FunctionName, arg);
							return r as ReplaceResult;
						} catch (System.Exception e) {
							// TRANSLATORS: Log message. SoundEffectCommand plugin.
							Logger.Log(T._("Could not get JS result."));
							Logger.DebugLog(e.ToString());
							return null;
						}
					};
					break;
				default:
					break;
			}
		}

		public void Initialize(SoundConfig parent) {
			SetType();
			SetPattern(parent);
		}
	}

	public delegate ReplaceResult ReplaceDelegate(JSArgument arg);

	public class ReplaceResult {
		public string Result { get; }
		public string[] SoundNames { get; }

		public ReplaceResult(string Result, string[] SoundNames) {
			this.Result = Result;
			this.SoundNames = SoundNames;
		}
	}

	public enum PatternType {
		String,
		Regex,
		Function,
	}
}
