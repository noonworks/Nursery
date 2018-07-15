using Discord.WebSocket;
using Newtonsoft.Json;
using Nursery.Plugins;
using Nursery.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nursery.BasicPlugins {
	public class ListFilter : IPlugin {
		public string Name { get; } = "Nursery.BasicPlugins.ListFilter";
		// TRANSLATORS: Bot-Help message. ListFilter plugin.
		public string HelpText { get; } = T._("Filter to ignore listed users.");
		Plugins.Type IPlugin.Type => Plugins.Type.Filter;

		private BWListConfigs config = null;

		public void Initialize(IPluginManager loader, IPlugin[] plugins) {
			try {
				this.config = loader.GetPluginSetting<BWListConfigs>(this.Name);
			} catch (System.Exception e) {
				Logger.DebugLog(e.ToString());
				this.config = null;
			}
			if (this.config == null) {
				this.config = new BWListConfigs();
			}
			this.config.Initialize();
		}

		public bool Execute(IBot bot, IMessage message) {
			if (this.config.Filters.Length == 0) {
				return false;
			}
			// [default value]
			// if first filter is BlackList : default value is "MATCHED"
			// if first filter is WhiteList : default value is "IGNORED"
			var ignored = (this.config.Filters[0].Type == ListType.WhiteList);
			Logger.DebugLog("* Start ListFilter");
			Logger.DebugLog(" - ignored: " + ignored.ToString());
			foreach (var filter in this.config.Filters) {
				if (filter.Execute(bot, message)) {
					if (filter.Type == ListType.BlackList) { ignored = true; }
					if (filter.Type == ListType.WhiteList) { ignored = false; }
					Logger.DebugLog(" - apply " + filter.Name + " - ignored: " + ignored.ToString());
					if (filter.Terminate) { break; }
				}
			}
			Logger.DebugLog(" - ignored: " + ignored.ToString());
			if (ignored) {
				message.Content = "";
				message.Terminated = true;
			}
			message.AppliedPlugins.Add(this.Name);
			Logger.DebugLog("* End ListFilter");
			return true;
		}
	}

	[JsonObject("Nursery.BasicPlugins.BWListConfigs")]
	public class BWListConfigs {
		[JsonProperty("filters")]
		public BWListConfig[] Filters { get; set; } = new BWListConfig[] { };

		public void Initialize() {
			foreach (var filter in Filters) {
				filter.Initialize();
			}
		}
	}

	[JsonObject("Nursery.BasicPlugins.BWListConfig")]
	public class BWListConfig {
		[JsonProperty("name")]
		public string Name { get; set; } = "";
		[JsonProperty("type")]
		public string StrType { get; set; } = "blacklist";
		[JsonProperty("terminate")]
		public bool Terminate { get; set; } = false;
		[JsonIgnore]
		public ListType Type { get; private set; } = ListType.BlackList;
		[JsonProperty("values")]
		public string[] Values { get; set; } = new string[] { };
		[JsonIgnore]
		private List<MatchDlegate> matchers = new List<MatchDlegate>();

		private static Regex BOT_REGEX = new Regex(@"bot");
		private static Regex MENTION_BOT_REGEX = new Regex(@"Mbot");
		private static Regex USER_REGEX = new Regex(@"U([0-9]+)");
		private static Regex MENTION_USER_REGEX = new Regex(@"Mu([0-9]+)");
		private static Regex ROLE_REGEX = new Regex(@"R([0-9]+)");
		private static Regex MENTION_ROLE_REGEX = new Regex(@"Mr([0-9]+)");
		private static Regex CHANNEL_REGEX = new Regex(@"C([0-9]+)");

		// U297594993340841995 => user id 297594993340841995
		// R275580301839826944 => users in role id 297594993340841995
		// bot => bots
		// Mu297594993340841995 => mention to user id 297594993340841995
		// Mr275580301839826944 => mention to users in role id 297594993340841995
		// Mbots => mention to bots
		// U297594993340841995C276350195871252480 => user id 297594993340841995 on channel id 276350195871252480
		// R275580301839826944C276350195871252480 => users in role id 297594993340841995 on channel id 276350195871252480
		// botC276350195871252480 => bots on channel id 276350195871252480

		public void Initialize() {
			if (this.StrType == "whitelist") { this.Type = ListType.WhiteList; }
			matchers = new List<MatchDlegate>();
			foreach (var v in Values) {
				List<MatchDlegate> funcs = new List<MatchDlegate>();
				// mentions to bots
				if (MENTION_BOT_REGEX.IsMatch(v)) {
					funcs.Add((IBot bot, IMessage message) => {
						Logger.DebugLog(" --- message.Original.MentionedUsers");
						foreach (var user in message.Original.MentionedUsers) {
							Logger.DebugLog(" ----- user(" + user.Id + ").IsBot: " + user.IsBot.ToString());
							if (user.IsBot) { return true; }
						}
						return false;
					});
				} else {
					// message from bots
					if (BOT_REGEX.IsMatch(v)) {
						funcs.Add((IBot bot, IMessage message) => {
							Logger.DebugLog(" --- message.Original.Author.IsBot: " + message.Original.Author.IsBot.ToString());
							return message.Original.Author.IsBot;
						});
					}
				}
				// messages from user
				var mu = USER_REGEX.Match(v);
				if (mu != null && mu.Length > 0) {
					funcs.Add((IBot bot, IMessage message) => {
						Logger.DebugLog(" --- message.Original.Author.Id: " + message.Original.Author.Id.ToString() + " / " + mu.Groups[1].Value);
						return (mu.Groups[1].Value == message.Original.Author.Id.ToString());
					});
				}
				// mention to user
				var mmu = MENTION_USER_REGEX.Match(v);
				if (mmu != null && mmu.Length > 0) {
					funcs.Add((IBot bot, IMessage message) => {
						Logger.DebugLog(" --- message.Original.MentionedUsers");
						foreach (var user in message.Original.MentionedUsers) {
							Logger.DebugLog(" ----- user: " + user.Id + " / " + mmu.Groups[1].Value);
							if (user.Id.ToString() == mmu.Groups[1].Value) { return true; }
						}
						return false;
					});
				}
				// message from role
				var mr = ROLE_REGEX.Match(v);
				if (mr != null && mr.Length > 0) {
					funcs.Add((IBot bot, IMessage message) => {
						var user = message.Original.Author as SocketGuildUser;
						var mached_roles_count = user.Roles.Count(r => r.Id.ToString() == mr.Groups[1].Value);
						Logger.DebugLog(" --- message.Original.Author.Roles: " + string.Join(",", user.Roles.Select(r => r.Id.ToString())) + " / " + mu.Groups[1].Value);
						return (mached_roles_count > 0);
					});
				}
				// mention to user in role
				var mmr = MENTION_ROLE_REGEX.Match(v);
				if (mmr != null && mmr.Length > 0) {
					funcs.Add((IBot bot, IMessage message) => {
						Logger.DebugLog(" --- message.Original.MentionedUsers");
						foreach (var user in message.Original.MentionedUsers) {
							var guilduser = user as SocketGuildUser;
							var mached_roles_count = guilduser.Roles.Count(r => r.Id.ToString() == mmr.Groups[1].Value);
							Logger.DebugLog(" ----- user.Roles: " + string.Join(",", guilduser.Roles.Select(r => r.Id.ToString())) + " / " + mmr.Groups[1].Value);
							if (mached_roles_count > 0) { return true; }
						}
						return false;
					});

				}
				// message in channel
				var mc = CHANNEL_REGEX.Match(v);
				if (mc != null && mc.Length > 0) {
					funcs.Add((IBot bot, IMessage message) => {
						Logger.DebugLog(" --- message.Original.Channel.Id: " + message.Original.Channel.Id.ToString() + " / " + mu.Groups[1].Value);
						return (mc.Groups[1].Value == message.Original.Channel.Id.ToString());
					});
				}
				if (funcs.Count() == 0) { continue; }
				// merge functions
				matchers.Add((IBot bot, IMessage message) => {
					foreach (var f in funcs) {
						if (! f(bot, message)) { return false; }
					}
					return true;
				});
			}
		}

		public bool Execute(IBot bot, IMessage message) {
			foreach (var matcher in matchers) {
				if (matcher(bot, message)) {
					return true;
				}
			}
			return false;
		}
		
		delegate bool MatchDlegate(IBot bot, IMessage message);
	}

	public enum ListType {
		BlackList,
		WhiteList
	}
}
