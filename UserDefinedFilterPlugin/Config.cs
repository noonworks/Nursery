using Newtonsoft.Json;
using Nursery.Options;
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
		public string[] Disabled { get; set; } = new string[] { };
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
		[JsonProperty("function_file")]
		public string FunctionFile { get; set; } = null;

		[JsonIgnore]
		public string Path { get; set; } = "";

		[JsonIgnore]
		private FilterType Type { get; set; } = FilterType.String;
		[JsonIgnore]
		private Regex RegexPattern { get; set; } = null;
		[JsonIgnore]
		private string StringPattern { get; set; } = null;
		[JsonIgnore]
		private ReplaceDelegate FunctionPattern { get; set; } = null;

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
					if (StrPattern.Length > 0) {
						RegexPattern = new Regex(StrPattern);
					}
					break;
				case FilterType.Function:
					var file = Config.LoadFile(this.FunctionFile);
					if (file.Length > 0) { this.StrPattern = file; }
					if (FunctionName.Length == 0 || StrPattern.Length == 0) {
						// TRANSLATORS: Log message. UserDefinedFilter plugin.
						Logger.Log(T._("Could not set function."));
						break;
					}
					JSWrapper.Instance.SetFunction(FunctionName, StrPattern);
					FunctionPattern = (JSArgument arg) => {
						try {
							var r = JSWrapper.Instance.ExecuteFunction(FunctionName, arg);
							return new JSReturnValue(r);
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

		private FilterResult DoFilterString(string content) {
			if (this.StringPattern == null || this.StringPattern.Length == 0) { return new FilterResult(); }
			var ret = new FilterResult();
			if (!content.Contains(this.StringPattern)) { return ret; }
			if (this.ReplaceTo != null) {
				ret.Content = content.Replace(this.StringPattern, this.ReplaceTo);
			}
			if (this.SendMessage != null) {
				ret.SendMessage = this.SendMessage;
			}
			return ret;
		}

		private FilterResult DoFilterRegex(string content) {
			if (this.RegexPattern == null) { return new FilterResult(); }
			var ret = new FilterResult();
			Match match = this.RegexPattern.Match(content);
			if (match == null || !match.Success) { return ret; }
			if (this.ReplaceTo != null) {
				ret.Content = this.RegexPattern.Replace(content, this.ReplaceTo);
			}
			if (this.SendMessage != null) {
				ret.SendMessage = match.Result(this.SendMessage);
			}
			return ret;
		}

		private FilterResult DoFilterFunction(IBot bot, IMessage message) {
			if (this.FunctionPattern == null) { return new FilterResult(); }
			var ret = this.FunctionPattern(new JSArgument(bot, message));
			switch (ret.Type) {
				case JSReturnValueType.StringValue:
					return new FilterResult() { Content = ret.StringValue };
				case JSReturnValueType.StringArrayValue:
					return new FilterResult() { Content = ret.StringArrayValue[0], SendMessage = ret.StringArrayValue[1] };
				case JSReturnValueType.Unknown:
				default:
					return new FilterResult();
			}
		}

		public FilterResult DoFilter(IBot bot, IMessage message) {
			switch (this.Type) {
				case FilterType.Regex:
					return DoFilterRegex(message.Content);
				case FilterType.Function:
					return DoFilterFunction(bot, message);
				case FilterType.String:
				default:
					return DoFilterString(message.Content);
			}
		}
	}

	public class FilterResult {
		public string Content = null;
		public string SendMessage = null;
	}

	enum JSReturnValueType {
		Unknown,
		StringValue,
		StringArrayValue,
	}

	class JSReturnValue {
		public JSReturnValueType Type { get; } = JSReturnValueType.Unknown;
		public string StringValue { get; } = null;
		public string[] StringArrayValue { get; } = null;

		public JSReturnValue(object val) {
			if (val == null) { return; }
			if (val.GetType() == typeof(string)) {
				this.Type = JSReturnValueType.StringValue;
				this.StringValue = (string)val;
			}
			if (val.GetType().IsArray) {
				var arr = (object[])val;
				if (arr.Length != 2) { return; }
				if (arr[0].GetType() == typeof(string) && arr[1].GetType() == typeof(string)) {
					this.Type = JSReturnValueType.StringArrayValue;
					this.StringArrayValue = new string[] { (string)arr[0], (string)arr[1] };
				}
			}
		}
	}

	delegate JSReturnValue ReplaceDelegate(JSArgument arg);

	enum FilterType {
		String,
		Regex,
		Function,
	}
}
