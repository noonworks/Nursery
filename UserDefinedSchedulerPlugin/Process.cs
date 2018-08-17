using Newtonsoft.Json;
using Nursery.Options;
using Nursery.Plugins.Schedules;
using Nursery.Utility;
using System.Collections.Generic;
using System.Linq;

namespace Nursery.UserDefinedSchedulerPlugin {
	public enum ProcessType {
		Unknown,
		SendMessage,
		Talk,
		Function,
	}

	public enum SendToType {
		defaultChannel,
		allChannels,
		specifiedChannels,
	}

	[JsonObject("Nursery.UserDefinedSchedulerPlugin.Process")]
	public class Process {
		[JsonProperty("type")]
		public string TypeStr { get; set; } = "";
		[JsonIgnore]
		public ProcessType Type { get; private set; } = ProcessType.Unknown;
		[JsonIgnore]
		public bool Valid { get; private set; } = false;

		#region SendMessage or Talk
		[JsonProperty("values")]
		public string[] Values { get; set; } = new string[] { };
		#endregion

		#region SendMessage
		[JsonProperty("send_to_type")]
		public string SendToTypeStr { get; set; } = "default";
		[JsonIgnore]
		public SendToType SendToType { get; set; } = SendToType.defaultChannel;
		[JsonProperty("send_to")]
		public string[] SendTo { get; set; } = new string[] { };
		[JsonProperty("cut_if_too_long")]
		public bool CutIfTooLong { get; set; } = true;
		#endregion

		#region Function
		[JsonProperty("function_str")]
		public string FunctionStr { get; set; } = "";
		[JsonProperty("function_name")]
		public string FunctionName { get; set; } = "";
		[JsonProperty("function_file")]
		public string FunctionFile { get; set; } = "";
		[JsonIgnore]
		public JSScheduleProcessFunction Function { get; private set; } = null;
		#endregion

		private void SetType() {
			switch (this.TypeStr.ToLower()) {
				case "send_message":
					this.Type = ProcessType.SendMessage;
					break;
				case "talk":
					this.Type = ProcessType.Talk;
					break;
				case "function":
					this.Type = ProcessType.Function;
					break;
				default:
					this.Type = ProcessType.Unknown;
					break;
			}
		}

		private void SetSendTo() {
			switch (this.SendToTypeStr.ToLower()) {
				case "all":
					this.SendToType = SendToType.allChannels;
					this.SendTo = new string[] { "all" };
					break;
				case "channels":
					if (this.SendTo.Length == 0) {
						this.SendToType = SendToType.defaultChannel;
						this.SendTo = new string[] { "default" };
					} else {
						this.SendToType = SendToType.specifiedChannels;
					}
					break;
				case "default":
				default:
					this.SendToType = SendToType.defaultChannel;
					this.SendTo = new string[] { "default" };
					break;
			}
		}

		private bool SetFunction() {
			var file = Config.LoadFile(this.FunctionFile);
			if (file.Length > 0) { this.FunctionStr = file; }
			if (this.FunctionName.Length == 0 || this.FunctionStr.Length == 0) {
				return false;
			}
			JSWrapper.Instance.SetFunction(this.FunctionName, this.FunctionStr);
			this.Function = (JSScheduleArgument arg) => {
				try {
					var r = JSWrapper.Instance.ExecuteFunction(FunctionName, arg);
					if (r == null) { return null; }
					var ret = new List<ScheduledMessage>();
					if (r.GetType().IsArray) {
						var arr = (object[])r;
						foreach (var item in arr) {
							if (item.GetType() == typeof(ScheduledMessage)) {
								ret.Add((ScheduledMessage)item);
							}
						}
						return ret.ToArray();
					}
					return null;
				} catch (System.Exception e) {
					// TRANSLATORS: Log message. UserDefinedScheduler plugin.
					Logger.Log(T._("Could not get JS result."));
					Logger.DebugLog(e.ToString());
					return null;
				}
			};
			return true;

		}

		public void Init() {
			SetType();
			switch (this.Type) {
				case ProcessType.SendMessage:
					SetSendTo();
					this.Values = this.Values.Where(v => v.Length > 0).ToArray();
					this.Valid = this.Values.Length > 0;
					break;
				case ProcessType.Talk:
					this.Values = this.Values.Where(v => v.Length > 0).ToArray();
					this.Valid = this.Values.Length > 0;
					break;
				case ProcessType.Function:
					this.Valid = SetFunction();
					break;
				case ProcessType.Unknown:
				default:
					this.Valid = false;
					break;
			}
		}
	}

	public delegate ScheduledMessage[] JSScheduleProcessFunction(JSScheduleArgument arg);
}
