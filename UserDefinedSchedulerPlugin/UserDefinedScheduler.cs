using Nursery.Plugins;

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

		public void Initialize(IPluginManager loader, IPlugin[] plugins) {
			this.config = loader.GetPluginSetting<UserDefinedSchedulerConfig>(this.Name);
			Reload();
		}

		private void Reload() {
		}
	}
}
