using Newtonsoft.Json;
using Nursery.Plugins;
using Nursery.Plugins.Schedules;
using Nursery.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nursery.BasicPlugins {
	public enum SendToType {
		defaultChannel,
		allChannels,
		specifiedChannels,
	}

	[JsonObject("Nursery.BasicPlugins.WelcomeSchedulerConfig")]
	public class WelcomeSchedulerConfig {
		[JsonProperty("use_welcome")]
		public bool UseWelcome { get; set; } = true;
		[JsonProperty("default_welcome")]
		public string DefaultWelcome { get; set; } = WelcomeScheduler.DEFAULT_WELCOME;
		[JsonProperty("use_bye")]
		public bool UseBye { get; set; } = true;
		[JsonProperty("default_bye")]
		public string DefaultBye { get; set; } = WelcomeScheduler.DEFAULT_BYE;
		[JsonProperty("debounce_tick")]
		public int DebounceTick { get; set; } = 15;
		[JsonProperty("send_to_type")]
		public string SendToTypeStr { get; set; } = "default";
		[JsonIgnore]
		public SendToType SendToType { get; set; } = SendToType.defaultChannel;
		[JsonProperty("send_to")]
		public string[] SendTo { get; set; } = new string[] { };
		[JsonProperty("summarize")]
		public bool Summarize { get; set; } = true;
		[JsonProperty("default_name_welcome")]
		public string DefaultNameWelcome { get; set; } = WelcomeScheduler.DEFAULT_SINGLE_USER_NAME;
		[JsonProperty("default_name_bye")]
		public string DefaultNameBye { get; set; } = WelcomeScheduler.DEFAULT_SINGLE_USER_NAME;
		[JsonProperty("separators")]
		public string[] Separators { get; set; } = new string[] { WelcomeScheduler.DEFAULT_SEPARATOR_1, WelcomeScheduler.DEFAULT_SEPARATOR_2 };
		[JsonProperty("id_text_pairs")]
		public WelcomeSchedulerSingleUserConfig[] IdTextPairs { get; set; } = new WelcomeSchedulerSingleUserConfig[] { };

		private void SetSendType() {
			switch (this.SendToTypeStr) {
				case "default":
					this.SendToType = SendToType.defaultChannel;
					break;
				case "all":
					this.SendToType = SendToType.allChannels;
					break;
				case "channels":
					this.SendToType = SendToType.specifiedChannels;
					break;
				default:
					this.SendToType = SendToType.defaultChannel;
					break;
			}
		}

		public void Init() {
			SetSendType();
			foreach (var itp in this.IdTextPairs) {
				itp.Init(this);
			}
		}
	}

	[JsonObject("Nursery.BasicPlugins.WelcomeSchedulerSingleUserConfig")]
	public class WelcomeSchedulerSingleUserConfig {
		[JsonProperty("user_id")]
		public string UserId { get; set; } = "";
		[JsonProperty("welcome")]
		public string Welcome { get; set; } = "";
		[JsonProperty("bye")]
		public string Bye { get; set; } = "";
		[JsonProperty("summarize")]
		public bool Summarize { get; set; } = true;
		[JsonProperty("name_welcome")]
		public string NameWelcome { get; set; } = "";
		[JsonProperty("name_bye")]
		public string NameBye { get; set; } = "";

		private void SetText(WelcomeSchedulerConfig config) {
			this.Welcome = this.Welcome.Replace("${default}", config.DefaultWelcome);
			this.Bye = this.Bye.Replace("${default}", config.DefaultBye);
			this.NameWelcome = this.NameWelcome.Replace("${default}", config.DefaultNameWelcome);
			this.NameBye = this.NameBye.Replace("${default}", config.DefaultNameBye);
		}

		public void Init(WelcomeSchedulerConfig config) {
			SetText(config);
		}

		public string ToString(bool isJoined) {
			var text = isJoined ? this.Welcome : this.Bye;
			return text.Replace("${username}", "${username" + this.UserId + "}").Replace("${nickname}", "${nickname" + this.UserId + "}");
		}
	}

	public class WelcomeScheduler : IPlugin {
		public const string NAME = "Nursery.BasicPlugins.WelcomeScheduler";
		public string Name { get; } = NAME;
		// TRANSLATORS: Bot-Help message. WelcomeScheduler plugin.
		public string HelpText { get; } = T._("Scheduler to add welcome messages.");
		Plugins.Type IPlugin.Type => Plugins.Type.Scheduler;
		// TRANSLATORS: Bot message. WelcomeScheduler plugin.
		public static readonly string DEFAULT_WELCOME = T._("${announce} ${nickname} is joined!");
		// TRANSLATORS: Bot message. WelcomeScheduler plugin.
		public static readonly string DEFAULT_BYE = T._("${announce} ${nickname} left.");
		// TRANSLATORS: Bot message. WelcomeScheduler plugin.
		public static readonly string DEFAULT_SINGLE_USER_NAME = T._("${nickname}");
		// TRANSLATORS: Bot message. WelcomeScheduler plugin.
		public static readonly string DEFAULT_SEPARATOR_1 = T._(", ");
		// TRANSLATORS: Bot message. WelcomeScheduler plugin.
		public static readonly string DEFAULT_SEPARATOR_2 = T._(" and ");

		public bool Execute(IBot bot, IMessage message) {
			// do nothing.
			return false;
		}

		private static string[] GetAdditionalConfigFiles(IPluginManager loader) {
			var dir = Path.Combine(loader.GetPluginDir(), NAME);
			if (!Directory.Exists(dir)) { return new string[] { }; }
			return Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories);
		}

		private static void LoadAdditionalConfig(IPluginManager loader, WelcomeSchedulerConfig config) {
			var files = GetAdditionalConfigFiles(loader);
			if (files.Length == 0) { return; }
			var pairs = new List<WelcomeSchedulerSingleUserConfig>();
			pairs.AddRange(config.IdTextPairs);
			foreach (var f in files) {
				try {
					var c = loader.LoadConfig<WelcomeSchedulerSingleUserConfig>(f);
					if (c != null) { pairs.Add(c); }
				} catch (Exception e) {
					Logger.DebugLog(e.ToString());
				}
			}
			var checkedPairs = new List<WelcomeSchedulerSingleUserConfig>();
			foreach (var pair in pairs) {
				checkedPairs.RemoveAll(p => p.UserId == pair.UserId);
				checkedPairs.Add(pair);
			}
			config.IdTextPairs = checkedPairs.ToArray();
		}

		private static WelcomeSchedulerConfig LoadCofig(IPluginManager loader) {
			WelcomeSchedulerConfig config;
			try {
				config = loader.GetPluginSetting<WelcomeSchedulerConfig>(NAME);
			} catch (System.IO.FileNotFoundException) {
				config = null;
			} catch (System.Exception e) {
				Logger.DebugLog(e.ToString());
				config = null;
			}
			if (config == null) {
				config = new WelcomeSchedulerConfig();
			}
			LoadAdditionalConfig(loader, config);
			config.Init();
			return config;
		}

		public void Initialize(IPluginManager loader, IPlugin[] plugins) {
			WelcomeSchedulerConfig config = LoadCofig(loader);
			loader.AddSchedule(new WelcomeTask(config));
		}
	}

	public class WelcomeTask : ScheduledTaskBase {
		private const string NAME = "Nursery.BasicPlugins.WelcomeTask";
		private WelcomeSchedulerConfig Config;
		private string[] LastMembers = null;
		private string[] Leaved = null;
		private string[] Joined = null;
		private int DebounceCount = -1;
		private string[] MembersDebounceStart = null;

		public WelcomeTask(WelcomeSchedulerConfig config): base(NAME) {
			this.Config = config;
		}

		protected override bool DoCheck(IBot bot) {
			// bot leaved
			if (!bot.IsJoined) {
				this.MembersDebounceStart = null;
				this.LastMembers = null;
				this.DebounceCount = -1;
				return false;
			}
			// bot joined
			if (this.LastMembers == null) {
				this.LastMembers = bot.GetUserIdsInVoiceChannel();
				return false;
			}
			// check members
			var members = bot.GetUserIdsInVoiceChannel();
			var self = new string[] { bot.IdString };
			var leaved = this.LastMembers.Except(members).Except(self).ToArray();
			var joined = members.Except(this.LastMembers).Except(self).ToArray();
			var original_last = this.LastMembers;
			this.LastMembers = members;
			// members are changed
			if (leaved.Length > 0 || joined.Length > 0) {
				if (this.DebounceCount < 0) {
					// start debouncing
					Logger.DebugLog("[WelcomeTask] Start member cheking...");
					this.MembersDebounceStart = original_last;
				} else {
					// reset debouncing
					Logger.DebugLog("[WelcomeTask] Reset member cheking...");
				}
				this.DebounceCount = 0;
				return false;
			}
			// members are not changed and not in debouncing - do nothing
			if (this.DebounceCount < 0) {
				return false;
			}
			// members are not changed and in debouncing - only increment debounce count
			if (this.DebounceCount >= 0 && this.DebounceCount <= this.Config.DebounceTick) {
				this.DebounceCount++;
				return false;
			}
			// members are not changed and over debouncing - end debouncing
			if (this.DebounceCount > this.Config.DebounceTick) {
				Logger.DebugLog("[WelcomeTask] End member cheking.");
				this.DebounceCount = -1;
				// get changed members from MembersDebounceStart
				this.Leaved = this.MembersDebounceStart.Except(members).Except(self).ToArray();
				this.Joined = members.Except(this.MembersDebounceStart).Except(self).ToArray();
				// execute if members changed
				if (this.Leaved.Length > 0 || this.Joined.Length > 0) {
					Logger.DebugLog("[WelcomeTask] Member changed: " + this.Leaved.Length + " user(s) left and " + this.Joined.Length + " user(s) joined.");
					return true;
				}
			}
			return false;
		}

		private ScheduledMessage CreateMessage(string user_id, string[] channels, bool isJoined) {
			var msg = new ScheduledMessage() {
				Type = ScheduledMessageType.SendMessage,
				TextChannelIds = channels,
				CutIfTooLong = true
			};
			var user_text = this.Config.IdTextPairs.FirstOrDefault(itp => itp.UserId == user_id);
			if (user_text == null) {
				msg.Content = isJoined ? this.Config.DefaultWelcome : this.Config.DefaultBye;
			} else {
				msg.Content = isJoined ? user_text.Welcome : user_text.Bye;
			}
			msg.Content = msg.Content.Replace("${username}", "${username" + user_id + "}").Replace("${nickname}", "${nickname" + user_id + "}");
			return msg;
		}

		protected override IScheduledTask[] DoExecute(IBot bot) {
			string[] channels = new string[] { };
			switch (this.Config.SendToType) {
				case SendToType.allChannels:
					channels = bot.TextChannelIdStrings;
					break;
				case SendToType.specifiedChannels:
					channels = this.Config.SendTo;
					break;
				case SendToType.defaultChannel:
					break;
				default:
					break;
			}
			if (this.Config.UseBye && this.Leaved.Length > 0) {
				var msgs = this.Leaved.Select(uid => CreateMessage(uid, channels, false)).ToArray();
				Send(msgs, bot);
			}
			if (this.Config.UseWelcome && this.Joined.Length > 0) {
				var msgs = this.Joined.Select(uid => CreateMessage(uid, channels, true)).ToArray();
				Send(msgs, bot);
			}
			return null;
		}
	}
}
