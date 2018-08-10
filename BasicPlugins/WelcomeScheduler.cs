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
		[JsonProperty("send_to_type")]
		public string SendToTypeStr { get; set; } = "default";
		[JsonIgnore]
		public SendToType SendToType { get; set; } = SendToType.defaultChannel;
		[JsonProperty("send_to")]
		public string[] SendTo { get; set; } = new string[] { };
		[JsonProperty("id_text_pairs")]
		public WelcomeSchedulerConfigIdTextPair[] IdTextPairs { get; set; } = new WelcomeSchedulerConfigIdTextPair[] { };

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

		private void SetText() {
			foreach (var itp in this.IdTextPairs) {
				itp.Welcome = itp.Welcome.Replace("${default}", this.DefaultWelcome);
				itp.Bye = itp.Bye.Replace("${default}", this.DefaultBye);
			}
		}

		public void Init() {
			SetSendType();
			SetText();
		}
	}

	[JsonObject("Nursery.BasicPlugins.WelcomeSchedulerConfigIdTextPair")]
	public class WelcomeSchedulerConfigIdTextPair {
		[JsonProperty("user_id")]
		public string UserId { get; set; } = "";
		[JsonProperty("welcome")]
		public string Welcome { get; set; } = "";
		[JsonProperty("bye")]
		public string Bye { get; set; } = "";
	}

	public class WelcomeScheduler : IPlugin {
		public const string NAME = "Nursery.BasicPlugins.WelcomeScheduler";
		public string Name { get; } = NAME;
		// TRANSLATORS: Bot-Help message. WelcomeScheduler plugin.
		public string HelpText { get; } = T._("Scheduler to add welcome messages.");
		Plugins.Type IPlugin.Type => Plugins.Type.Scheduler;
		// TRANSLATORS: Bot message. WelcomeScheduler plugin. ${nickname} is nickname of joined user.
		public static readonly string DEFAULT_WELCOME = T._("${announce} ${nickname} is joined!");
		// TRANSLATORS: Bot message. WelcomeScheduler plugin. ${nickname} is nickname of joined user.
		public static readonly string DEFAULT_BYE = T._("${announce} ${nickname} left.");

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
			var pairs = new List<WelcomeSchedulerConfigIdTextPair>();
			pairs.AddRange(config.IdTextPairs);
			foreach (var f in files) {
				try {
					var c = loader.LoadConfig<WelcomeSchedulerConfigIdTextPair>(f);
					if (c != null) { pairs.Add(c); }
				} catch (Exception e) {
					Logger.DebugLog(e.ToString());
				}
			}
			var checkedPairs = new List<WelcomeSchedulerConfigIdTextPair>();
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
		private const int DebounceMax = 15; // 1500 msec
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
			if (this.DebounceCount >= 0 && this.DebounceCount <= DebounceMax) {
				this.DebounceCount++;
				return false;
			}
			// members are not changed and over debouncing - end debouncing
			if (this.DebounceCount > DebounceMax) {
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
