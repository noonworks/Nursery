using Newtonsoft.Json;
using Nursery.Plugins;
using Nursery.Plugins.Schedules;
using Nursery.Utility;
using System;
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

		public WelcomeTask(WelcomeSchedulerConfig config): base(NAME) {
			this.Config = config;
		}

		protected override ScheduleCheckResult DoCheck(IBot bot) {
			if (!bot.IsJoined) {
				this.LastMembers = null;
				return new ScheduleCheckResult();
			}
			var members = bot.GetUserIdsInVoiceChannel();
			if (this.LastMembers == null) {
				this.LastMembers = members;
				return new ScheduleCheckResult();
			}
			var self = new string[] { bot.IdString };
			this.Leaved = this.LastMembers.Except(members).Except(self).ToArray();
			this.Joined = members.Except(this.LastMembers).Except(self).ToArray();
			if (this.Leaved.Length == 0 && this.Joined.Length == 0) {
				return new ScheduleCheckResult();
			}
			this.LastMembers = members;
			Logger.DebugLog("[WelcomeTask] Member changed: " + this.Leaved.Length + " user(s) left and " + this.Joined.Length + " user(s) joined.");
			return new ScheduleCheckResult() { Established = true };
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
					channels = bot.GetTextChannelIds();
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
