using Newtonsoft.Json;
using Nursery.Plugins;
using Nursery.Utility;
using System.Text.RegularExpressions;

namespace Nursery.UserDefinedFilterPlugin {
	[JsonObject("Nursery.UserDefinedFilterPlugin.UserDefinedFilter")]
	public class UserDefinedFilterConfig {
		[JsonProperty("dir")]
		public string Dir { get; set; } = "filters";
		[JsonProperty("config_file")]
		public string ConfigFile { get; set; } = "filters.json";
	}

	[JsonObject("Nursery.UserDefinedFilterPlugin.FiltersConfig")]
	public class FiltersConfig {
		[JsonProperty("filters")]
		public string[] Filters { get; set; } = new string[] { "*" };
		[JsonProperty("disabled")]
		public string[] Disabled { get; set; } = new string[] {};
	}

	[JsonObject("Nursery.UserDefinedFilterPlugin.FilterConfig")]
	public class FilterConfig {
		[JsonProperty("pattern")]
		public string StrPattern { get; set; } = "";
		[JsonProperty("type")]
		public string StrType { get; set; } = "string";
		[JsonProperty("replace_to")]
		public string ReplaceTo { get; set; } = null;
		[JsonProperty("send_message")]
		public string SendMessage { get; set; } = null;
		[JsonProperty("function_name")]
		public string FunctionName { get; set; } = null;

		[JsonIgnore]
		public string Path { get; set; } = "";

		[JsonIgnore]
		public FilterType Type { get; private set; } = FilterType.String;
		[JsonIgnore]
		public Regex RegexPattern { get; private set; } = null;
		[JsonIgnore]
		public string StringPattern { get; private set; } = null;
		[JsonIgnore]
		public ReplaceDelegate FunctionPattern { get; private set; } = null;

		private void SetType() {
			switch (this.StrType.ToLower()) {
				case "regex":
					this.Type = FilterType.Regex;
					break;
				case "function":
					this.Type = FilterType.Function;
					break;
				default:
					this.Type = FilterType.String;
					break;
			}
		}

		private void SetPattern() {
			switch (this.Type) {
				case FilterType.String:
					StringPattern = StrPattern;
					break;
				case FilterType.Regex:
					RegexPattern = new Regex(StrPattern);
					break;
				case FilterType.Function:
					if (FunctionName.Length == 0 || StrPattern.Length == 0) {
						// TRANSLATORS: Log message. UserDefinedFilter plugin.
						Logger.Log(T._("Could not set function."));
						break;
					}
					JSWrapper.Instance.SetFunction(FunctionName, StrPattern);
					FunctionPattern = (JSArgument arg) => {
						try {
							var r = JSWrapper.Instance.ExecuteFunction(FunctionName, arg);
							return r;
						} catch (System.Exception e) {
							// TRANSLATORS: Log message. UserDefinedFilter plugin.
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

		public void Initialize() {
			SetType();
			SetPattern();
		}
	}

	public delegate object ReplaceDelegate(JSArgument arg);

	public enum FilterType {
		String,
		Regex,
		Function,
	}
}
