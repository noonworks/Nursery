using Newtonsoft.Json;
using Nursery.Plugins;
using System;

namespace Nursery.UserDefinedSchedulerPlugin {
	[JsonObject("Nursery.UserDefinedSchedulerPlugin.UserDefinedSchedulerConfig")]
	public class UserDefinedSchedulerConfig {
		[JsonProperty("dir")]
		public string Dir { get; set; } = "schedules";
	}

	[JsonObject("Nursery.UserDefinedSchedulerPlugin.ScheduleConfig")]
	public class ScheduleConfig {
		[JsonProperty("name")]
		public string Name { get; set; } = "";
		[JsonProperty("conditions")]
		public Condition[] Conditions { get; set; } = new Condition[] { };
		[JsonProperty("processes")]
		public Process[] Processes { get; set; } = new Process[] { };
		[JsonIgnore]
		public bool Valid { get; private set; } = false;
		[JsonIgnore]
		public string Path { get; private set; } = "";

		public void Init(string path) {
			this.Path = path;
			this.Valid = (this.Conditions.Length > 0 && this.Processes.Length > 0);
		}
	}

	public class JSScheduleArgument {
		public IBot Bot { get; }
		public IJSScheduler Scheduler { get; }

		public JSScheduleArgument(IBot bot, IJSScheduler scheduler) {
			this.Bot = bot;
			this.Scheduler = scheduler;
		}
	}

	public interface IJSScheduler {
		bool Valid { get; }
		DateTime CheckedAt { get; }
		string AdditionalData { get; set; }
		DateTime LastExecute { get; }
		long TotalCount { get; }
		long Count { get; }
	}
}
