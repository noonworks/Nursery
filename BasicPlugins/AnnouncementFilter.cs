using Nursery.Plugins;
using Nursery.Utility;
using System.Text.RegularExpressions;

namespace Nursery.BasicPlugins {
	public class AnnouncementFilter : IPlugin {
		public const string NAME = "Nursery.BasicPlugins.AnnouncementFilter";
		public string Name { get; } = NAME;
		// TRANSLATORS: Bot-Help message. AnnouncementFilter plugin.
		public string HelpText { get; } = T._("Filter to read announcement.");
		Plugins.Type IPlugin.Type => Plugins.Type.Filter;

		// TRANSLATORS: Bot message. AnnouncementFilter plugin.
		private static readonly string AnnounceLabel = T._("[ANNOUNCE]");
		private static readonly Regex AnnounceLabelRegex = new Regex(Regex.Escape(AnnounceLabel));
		// TRANSLATORS: Bot message. AnnouncementFilter plugin.
		private static readonly string SpeakLabel = T._("[SPEAK]");
		private static readonly Regex SpeakLabelRegex = new Regex(Regex.Escape(SpeakLabel));

		private static readonly Regex TrimRegex = new Regex("^\\s*");
		private string AnnounceLabelPrefix = null;
		private string SpeakLabelPrefix = null;

		public void Initialize(IPluginManager loader, IPlugin[] plugins) {
			loader.SetAnnounceLabel(AnnounceLabel);
			loader.SetSpeakLabel(SpeakLabel);
		}

		private void LoadLabelReplacer(IBot bot) {
			var ignore_name_prefix = "";
			var ap = bot.GetPlugin(AddNameFilter.NAME);
			if (ap != null) {
				AddNameFilter ap2 = ap as AddNameFilter;
				if (ap2 != null) {
					ignore_name_prefix = ap2.Config.IgnorePrefix;
				}
			}
			// TRANSLATORS: Bot message. AnnouncementFilter plugin. {0} is ignore_name_prefix.
			this.AnnounceLabelPrefix = T._("{0} Announce: ", ignore_name_prefix) + " ";
			this.SpeakLabelPrefix = ignore_name_prefix + " ";
		}

		public bool Execute(IBot bot, IMessage message) {
			if (this.AnnounceLabelPrefix == null) { this.LoadLabelReplacer(bot); }
			if (message.Original.Author.Id != bot.Id) { return false; }
			var trimmed = Messages.TrimMention(message.Content);
			var trimmedMessage = TrimRegex.Replace(trimmed.Trimmed, "");
			if (trimmedMessage.Length == 0) { return false; }
			// check for ANNOUNCE
			if (AnnounceLabel.Length > 0) {
				var ma = AnnounceLabelRegex.Match(trimmedMessage);
				if (ma != null && ma.Success && ma.Index == 0) {
					message.Content = this.AnnounceLabelPrefix + AnnounceLabelRegex.Replace(message.Content, "", 1);
					message.AppliedPlugins.Add(this.Name);
					return true;
				}
			}
			// check for SPEAK
			if (SpeakLabel.Length > 0) {
				var ms = SpeakLabelRegex.Match(trimmedMessage);
				if (ms != null && ms.Success && ms.Index == 0) {
					message.Content = this.SpeakLabelPrefix + SpeakLabelRegex.Replace(message.Content, "", 1);
					message.AppliedPlugins.Add(this.Name);
					return true;
				}
			}
			return false;
		}
	}
}
