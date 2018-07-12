using Nursery.Plugins;

namespace Nursery.BasicPlugins {
	public class AttachmentFilter : IPlugin {
		public string Name { get; } = "Nursery.BasicPlugins.AttachmentFilter";
		// TRANSLATORS: Bot-Help message. AttachmentFilter plugin.
		public string HelpText { get; } = T._("Filter to add information of attachment files.");
		Plugins.Type IPlugin.Type => Plugins.Type.Filter;

		public void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		public bool Execute(IBot bot, IMessage message) {
			if (message.Original.Attachments.Count == 0) { return false; }
			// TRANSLATORS: Bot message. AttachmentFilter plugin. {0} is number of file(s).
			message.Content = message.Content + T._n(" (with {0} file)", " (with {0} files)", message.Original.Attachments.Count);
			message.AppliedPlugins.Add(this.Name);
			return true;
		}
	}
}
