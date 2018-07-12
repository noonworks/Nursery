using System.Text.RegularExpressions;
using Nursery.Plugins;

namespace Nursery.BasicPlugins {
	public class UrlFilter : IPlugin {
		public string Name { get; } = "Nursery.BasicPlugins.UrlFilter";
		// TRANSLATORS: Bot-Help message. UrlFilter plugin.
		public string HelpText { get; } = T._("Filter to replace url to short format.");
		Plugins.Type IPlugin.Type => Plugins.Type.Filter;

		private static Regex URLREGEX = new Regex(@"https?:\/\/(?:.*?\.)?([a-zA-Z0-9\-@:%_\\\+~#=]{2,256})(\.[a-z]{2,6})(\.[a-z]{2,6})?(?:\/[a-zA-Z0-9\-@:%_\\\+~#=\.\?\&\/]*)?");

		public void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		private static string ReplaceDot(string urlpart) {
			// TRANSLATORS: Bot message. UrlFilter plugin. Replacer of ".".
			var replacer = T._(" dot ");
			if (!replacer.Equals(".")) {
				return urlpart.Replace(".", replacer);
			}
			return urlpart;
		}

		public bool Execute(IBot bot, IMessage message) {
			var m = message.Content;
			m = URLREGEX.Replace(m, (Match match) => {
				if (match.Groups.Count < 3) { return match.Groups[0].Value; }
				if (match.Groups.Count == 4) {
					// TRANSLATORS: Bot message. UrlFilter plugin. {0} is URL.
					return T._(" URL {0} ", ReplaceDot(match.Groups[1].Value + match.Groups[2].Value + match.Groups[3].Value));
				}
				// TRANSLATORS: Bot message. UrlFilter plugin. {0} is URL.
				return T._(" URL {0} ", ReplaceDot(match.Groups[1].Value + match.Groups[2].Value));
			});
			if (!m.Equals(message.Content)) {
				message.Content = m;
				message.AppliedPlugins.Add(this.Name);
				return true;
			}
			return false;
		}
	}
}
