using Nursery.Options;
using Nursery.Plugins;
using Nursery.Plugins.Schedules;
using Nursery.Utility;
using System.IO;
using System.Linq;

namespace Nursery.UserDefinedSchedulerPlugin {
	public class UserDefinedScheduler : IPlugin {
		public static readonly string NAME = "Nursery.UserDefinedSchedulerPlugin.UserDefinedScheduler";
		public string Name { get; } = NAME;
		// TRANSLATORS: Bot-Help message. UserDefinedScheduler plugin.
		public string HelpText { get; } = T._("Scheduler manages user-defined schedule tasks.");
		Plugins.Type IPlugin.Type => Plugins.Type.Scheduler;

		public bool Execute(IBot bot, IMessage message) {
			// do nothing.
			return false;
		}

		private UserDefinedSchedulerConfig config = null;
		private UserDefinedScheduledTask[] schedules = new UserDefinedScheduledTask[] { };

		public void Initialize(IPluginManager loader, IPlugin[] plugins) {
			this.config = loader.GetPluginSetting<UserDefinedSchedulerConfig>(this.Name);
			Reload(loader);
		}

		private string[] GetJsons(string dir) {
			if (dir.Length == 0) {
				// TRANSLATORS: Log message. UserDefinedScheduler plugin.
				Logger.Log(T._("Can not load schedulers directory."));
				return new string[] { };
			}
			if (!Directory.Exists(dir)) {
				dir = Path.Combine(Directory.GetCurrentDirectory(), dir);
			}
			if (!Directory.Exists(dir)) {
				// TRANSLATORS: Log message. UserDefinedScheduler plugin. {0} is directory.
				Logger.Log(T._("Schedulers directory [{0}] is not found.", dir));
				return new string[] { };
			}
			return Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories);
		}

		private void DoReload() {
			this.schedules = GetJsons(this.config.Dir).Select<string, UserDefinedScheduledTask>(path => {
				var conf = Config.Instance.LoadConfig<ScheduleConfig>(path);
				if (conf == null) {
					// TRANSLATORS: Log message. UserDefinedScheduler plugin. {0} is file path.
					Logger.Log(T._("Could not load scheduler [{0}].", path));
					return null;
				}
				conf.Init(path);
				if (!conf.Valid) {
					// TRANSLATORS: Log message. UserDefinedScheduler plugin. {0} is file path.
					Logger.Log(T._("Could not load scheduler [{0}].", path));
				}
				if (!conf.Valid) { return null; }
				return new UserDefinedScheduledTask(conf);
			}).Where(c => c!= null && c.Valid).ToArray();
		}

		private void ResetJSWrapper() {
			JSWrapper.Instance.Reset();
			// * set types to JS engine before init schedule configs.
			JSWrapper.Instance.SetType("JSScheduleArgument", typeof(JSScheduleArgument));
			JSWrapper.Instance.SetType("ScheduledMessage", typeof(ScheduledMessage));
		}

		private void Reload(IPluginManager loader) {
			// Remove schedules from bot
			// - there is no schedules becaus this is called on initialize the plugin.
			// Reset JSWrapper
			ResetJSWrapper();
			// Load JSON configs and create schedules
			DoReload();
			// Add schedules to bot
			foreach (var s in this.schedules) {
				loader.AddSchedule(s);
			}
		}

		public void Reload(IBot bot) {
			// Remove schedules from bot
			if (this.schedules.Length > 0) {
				bot.RemoveSchedules(this.schedules);
			}
			// Reset JSWrapper
			ResetJSWrapper();
			// Load JSON configs and create schedules
			DoReload();
			// Add schedules to bot
			if (this.schedules.Length > 0) {
				bot.AddSchedules(this.schedules);
			}
		}
	}
}
