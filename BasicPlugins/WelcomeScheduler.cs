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

		public WelcomeSchedulerSingleUserConfig() { }

		public WelcomeSchedulerSingleUserConfig(string userid, WelcomeSchedulerConfig config) {
			this.UserId = userid;
			this.Welcome = "${default}";
			this.Bye = "${default}";
			this.NameWelcome = "${default}";
			this.NameBye = "${default}";
			this.Init(config);
		}

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

		public string ToNameString(bool isJoined) {
			var text = isJoined ? this.NameWelcome : this.NameBye;
			return text.Replace("${username}", "${username" + this.UserId + "}").Replace("${nickname}", "${nickname" + this.UserId + "}");
		}

		public ScheduledMessage ToMessage(bool isJoined, string[] channels) {
			var text = this.ToString(isJoined);
			if (text.Length == 0) { return null; }
			return new ScheduledMessage() {
				Type = ScheduledMessageType.SendMessage,
				TextChannelIds = channels,
				Content = text,
				CutIfTooLong = true
			};
		}
	}

	class MessageBuilder {
		private WelcomeSchedulerConfig Config = null;

		public MessageBuilder(WelcomeSchedulerConfig config) {
			this.Config = config;
		}

		private ScheduledMessage CreateSummarizedMessage(bool isJoined, WelcomeSchedulerSingleUserConfig[] userConfigs, string[] channels) {
			var names = userConfigs.Select(c => c.ToNameString(isJoined)).Where(s => s.Length > 0).ToList();
			var separators = this.Config.Separators.Length > 0 ? this.Config.Separators : new string[] { "" };
			for (int i = separators.Length - 1; i > 0; i--) {
				if (names.Count <= 1) { break; }
				names[names.Count - 2] = names[names.Count - 2] + separators[i] + names[names.Count - 1];
				names.RemoveAt(names.Count - 1);
			}
			var names_str = string.Join(separators[0], names);
			// create message
			var text = isJoined ? this.Config.DefaultWelcome : this.Config.DefaultBye;
			text = text.Replace("${username}", names_str).Replace("${nickname}", names_str);
			return new ScheduledMessage() {
				Type = ScheduledMessageType.SendMessage,
				TextChannelIds = channels,
				Content = text,
				CutIfTooLong = true
			};
		}

		public ScheduledMessage[] GetMessages(bool isJoined, string[] ids, string[] channels) {
			var ret = new List<ScheduledMessage>();
			var configs = ids.Select(id => {
				var user_conf = this.Config.IdTextPairs.FirstOrDefault(itp => itp.UserId == id);
				if (user_conf == null) {
					return new WelcomeSchedulerSingleUserConfig(id, this.Config);
				} else {
					return user_conf;
				}
			});
			// not summarize
			if (!this.Config.Summarize) {
				return configs.Select(c => c.ToMessage(isJoined, channels)).Where(m => m != null).ToArray();
			}
			// summarize
			// check users who can not summarize
			var notSummarizeUsers = configs.Where(c => !c.Summarize).ToArray();
			var summarizeUsers = configs.Except(notSummarizeUsers).ToArray();
			// create messages for users who can not summarize
			foreach (var userConfig in notSummarizeUsers) {
				ret.Add(userConfig.ToMessage(isJoined, channels));
			}
			// create summarized message
			if (summarizeUsers.Length == 1) {
				ret.Add(summarizeUsers[0].ToMessage(isJoined, channels));
			} else if (summarizeUsers.Length > 0) {
				ret.Add(CreateSummarizedMessage(isJoined, summarizeUsers, channels));
			}
			return ret.Where(m => m != null).ToArray();
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
		private MessageBuilder Builder;
		private string[] LastMembers = null;
		private string[] Leaved = null;
		private string[] Joined = null;
		private int DebounceCount = -1;
		private string[] MembersDebounceStart = null;

		public WelcomeTask(WelcomeSchedulerConfig config): base(NAME) {
			this.Config = config;
			this.Builder = new MessageBuilder(this.Config);
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
				var msgs = this.Builder.GetMessages(false, this.Leaved, channels);
				Send(msgs, bot);
			}
			if (this.Config.UseWelcome && this.Joined.Length > 0) {
				var msgs = this.Builder.GetMessages(true, this.Joined, channels);
				Send(msgs, bot);
			}
			if (this.LastMembers.Length <= 1)
			{
                Send(new[] {
                    new ScheduledMessage() {
                    Type=ScheduledMessageType.SendMessage,
                    Content=T._("Bye!"),
                    TextChannelIds=channels,
                } }, bot);
                bot.LeaveChannel(null);
            }
			return null;
		}
	}
}
