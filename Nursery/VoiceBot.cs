﻿using Discord;
using Discord.WebSocket;
using FNF.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nursery.Options;
using Nursery.Plugins;
using Nursery.Utility;
using System.Linq;

namespace Nursery {
	class VoiceBot : IDisposable, IBot {
		private static VoiceBot instance = null;

		private object state_lock_object = new object();
		private BotState state;
		private BouyomiChanClient bouyomichan = null;
		private DiscordSocketClient discord = null;
		private Nursery.AudioConnector.NAudio voice = null;
		private Timer timer = null;
		private List<IScheduledTask> Schedules = new List<IScheduledTask>();
		private object schedule_lock_object = new object();

		#region Initialize

		public static async Task<VoiceBot> CreateInstanceAsync(CommandlineOptions opts) {
			if (instance != null) {
				// TRANSLATORS: Log message. Initializing Nursery.
				Logger.Log(T._("Already initialized."));
				return instance;
			}
			// TRANSLATORS: Log message. Initializing Nursery.
			Logger.Log(T._("Load config ..."));
			try {
				Config.Initialize(opts);
				Config.Instance.Load();
				Config.Instance.MainConfig.Debug = opts.Debug;
				Logger.SetDebug(opts.Debug);
			} catch (Exception e) {
				// TRANSLATORS: Log message. Initializing Nursery.
				Logger.Log(T._("Could not load config. Check your config file."));
				Logger.DebugLog(e.ToString());
				return null;
			}
			Program.ShowVersions();
			// TRANSLATORS: Log message. Initializing Nursery.
			Logger.Log(T._("Create VoiceBot ..."));
			try {
				instance = new VoiceBot();
			} catch (Exception e) {
				// TRANSLATORS: Log message. Initializing Nursery.
				Logger.Log(T._("Could not load config. Check your config file."));
				Logger.DebugLog(e.ToString());
				return null;
			}
			// TRANSLATORS: Log message. Initializing Nursery.
			Logger.Log(T._("- set Discord events ..."));
			instance.discord.MessageReceived += instance.Client_MessageReceived;
			instance.discord.LoggedIn += (async () => {
				await Task.Run(() => {
					// TRANSLATORS: Log message. Initializing Nursery.
					Logger.Log(T._("- Logged in!"));
				});
			});
			instance.discord.LoggedOut += (async () => {
				await Task.Run(() => {
					// TRANSLATORS: Log message. Initializing Nursery.
					Logger.Log(T._("- Logged out!"));
				});
			});
			instance.discord.Connected += (async () => {
				await Task.Run(() => {
					// TRANSLATORS: Log message. Initializing Nursery.
					Logger.Log(T._("- Connected!"));
				});
			});
			instance.discord.Disconnected += (async (ex) => {
				await Task.Run(() => {
					// TRANSLATORS: Log message. Initializing Nursery.
					Logger.Log(T._("- Disconnected!"));
					if (ex != null) {
						// TRANSLATORS: DebugLog message. Initializing Nursery.
						Logger.DebugLog(T._("Exception in disconnecting:"));
						Logger.DebugLog(ex.ToString());
					}
				});
			});
			// TRANSLATORS: Log message. Initializing Nursery.
			Logger.Log(T._("- login to Discord ..."));
			try {
				await instance.discord.LoginAsync(TokenType.Bot, Config.Instance.MainConfig.Token);
				await instance.discord.StartAsync();
				await instance.discord.SetGameAsync("Nursery");
			} catch (Exception e) {
				// TRANSLATORS: Log message. Initializing Nursery.
				Logger.Log(T._("Could not login to Discord. Please check bot account and its token."));
				Logger.DebugLog(e.ToString());
				instance.Dispose();
				instance = null;
				return null;
			}
			var timeout = 0;
			while (true) {
				if (instance.discord.LoginState == LoginState.LoggedIn && instance.discord.ConnectionState == ConnectionState.Connected) {
					// TRANSLATORS: Log message. Initializing Nursery.
					Logger.Log(T._("- Logged in and Connected!"));
					break;
				}
				if (timeout > 300) {
					timeout = -1;
					break;
				}
				timeout++;
				await Task.Delay(100);
			}
			if (timeout < 0) {
				// TRANSLATORS: Log message. Initializing Nursery.
				Logger.Log(T._("Could not login to Discord. Login challange was timeouted."));
				instance.Dispose();
				instance = null;
				return null;
			}
			// TRANSLATORS: Log message. Initializing Nursery.
			Logger.Log(T._("Start timer..."));
			instance.timer.Start();
			// TRANSLATORS: Log message. Initializing Nursery.
			Logger.Log(T._("Done!"));
			Logger.Log("/////////////");
			Logger.Log("// Nursery //");
			Logger.Log("/////////////");
			return instance;
		}

		public static void FreeInstance() {
			if (instance == null) { return; }
			instance.Disconnect();
			instance.Dispose();
			instance = null;
		}

		private static DiscordSocketClient CreateDiscordClient() {
			// Windows Vista = 6.0
			// Windows 7 = 6.1
			// Windows 8 = 6.2
			// Windows 8.1 = 6.3
			// Windows 10 = 10.0
			var os = Environment.OSVersion.Version;
			if (os.Major == 6 && os.Minor == 1) {
				Logger.DebugLog("* Windows 7 - Use Discord.Net.Providers.WS4Net.WS4NetProvider");
				return new DiscordSocketClient(
					new DiscordSocketConfig() {
						WebSocketProvider = Discord.Net.Providers.WS4Net.WS4NetProvider.Instance,
						GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
                    }
                );
			}
			if (os.Major < 6 || (os.Major == 6 && os.Minor < 1)) {
				// TRANSLATORS: Log message. Initializing Nursery.
				Logger.Log(T._("Error: This OS is not supported."));
				return null;
			}
			return new DiscordSocketClient(
				new DiscordSocketConfig() {
					GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
				});
		}

		private void CheckBouyomichan() {
			Exception err = null;
			try {
				// TRANSLATORS: Log message. Initializing Nursery.
				Logger.Log(T._("  - Check Bouyomi-chan ..."));
				this.bouyomichan.ClearTalkTasks();
				return;
			} catch (Exception e) {
				err = e;
			}
			if (Config.Instance.MainConfig.BouyomichanPath == null || Config.Instance.MainConfig.BouyomichanPath.Length == 0) {
				// TRANSLATORS: Error message. Initializing Nursery.
				throw new Exception(T._("Could not load Bouyomi-chan. Please check Bouyomi-chan awaking."), err);
			}
			try {
				// TRANSLATORS: Log message. Initializing Nursery.
				Logger.Log(T._("  - Executing Bouyomi-chan ..."));
				System.Diagnostics.Process p = System.Diagnostics.Process.Start(Config.Instance.MainConfig.BouyomichanPath);
				var timeout_exec = 10;
				while (timeout_exec > 0) {
					if (p.WaitForInputIdle(1000)) { break; }
					timeout_exec--;
				}
			} catch (Exception e) {
				// TRANSLATORS: Error message. Initializing Nursery.
				throw new Exception(T._("Could not execute Bouyomi-chan."), e);
			}
			// TRANSLATORS: Log message. Initializing Nursery.
			Logger.Log(T._("  - Check Bouyomi-chan ..."));
			var timeout = 10;
			while (timeout > 0) {
				try {
					this.bouyomichan.ClearTalkTasks();
					return;
				} catch (Exception e) {
					err = e;
				}
				System.Threading.Thread.Sleep(1000);
				timeout--;
			}
			this.Dispose();
			if (err != null) {
				// TRANSLATORS: Error message. Initializing Nursery.
				throw new Exception(T._("Could not load Bouyomi-chan. Please check Bouyomi-chan awaking."), err);
			}
		}

		private VoiceBot() {
			this.state = new BotState();
			// TRANSLATORS: Log message. Initializing Nursery.
			Logger.Log(T._("- initialize Bouyomi-chan ..."));
			this.bouyomichan = new BouyomiChanClient();
			this.CheckBouyomichan();
			// TRANSLATORS: Log message. Initializing Nursery.
			Logger.Log(T._("- initialize sound devices ..."));
			try {
				Nursery.AudioConnector.NAudio.Initialize(Config.Instance.MainConfig);
			} catch (Exception e) {
				this.Dispose();
				// TRANSLATORS: Error message. Initializing Nursery.
				throw new Exception(T._("Could not initialize sound devices. Please check virtual devices installed and valid name set."), e);
			}
			try {
				this.voice = new Nursery.AudioConnector.NAudio(Config.Instance.MainConfig);
			} catch (Exception e) {
				this.Dispose();
				// TRANSLATORS: Error message. Initializing Nursery.
				throw new Exception(T._("Could not initialize recording devices. Please check virtual devices installed and valid name set."), e);
			}
			// TRANSLATORS: Log message. Initializing Nursery.
			Logger.Log(T._("- initialize Discord client ..."));
			this.discord = CreateDiscordClient();
			// TRANSLATORS: Log message. Initializing Nursery.
			Logger.Log(T._("- initialize Timer ..."));
			this.timer = new Timer(this.TickHandler);
			// TRANSLATORS: Log message. Initializing Nursery.
			Logger.Log(T._("- load plugins ..."));
			PluginManager.Instance.Load(this);
		}

		#endregion

		#region Dispose

		private void Disconnect() {
			try {
				// TRANSLATORS: Log message. Disconnect Nursery.
				Logger.Log(T._("- stop Timer ..."));
				this.timer.Stop();
				// TRANSLATORS: Log message. Disconnect Nursery.
				Logger.Log(T._("- disconnect from Voice channel ..."));
				if (this.voice != null) {
					this.voice.Disconnect().Wait();
					this.voice.Dispose();
					this.voice = null;
				}
				// TRANSLATORS: Log message. Disconnect Nursery.
				Logger.Log(T._("- disconnect from discord ..."));
				if (this.discord != null) {
					var t = Task.Run(async () => {
						await instance.discord.SetGameAsync("");
						await this.discord.StopAsync();
						await this.discord.LogoutAsync();
					});
					var timeout = 0;
					while (true) {
						if (this.discord.ConnectionState == ConnectionState.Disconnected && this.discord.LoginState == LoginState.LoggedOut) {
							// TRANSLATORS: Log message. Disconnect Nursery.
							Logger.Log(T._("- Disconnected and Logged out!"));
							break;
						}
						if (timeout > 300) {
							timeout = -1;
							break;
						}
						timeout++;
						Task.Delay(100).Wait();
					}
					if (timeout < 0) {
						// TRANSLATORS: Log message. Disconnect Nursery.
						Logger.Log(T._("Could not stop Discord. Stop challange was timeouted."));
					}
				}
			} catch (Exception e) {
				// TRANSLATORS: Log message. Disconnect Nursery.
				Logger.Log(T._("Could not disconnect."));
				Logger.DebugLog(e.ToString());
			}
		}

		protected virtual void Dispose(bool disposing) {
			try {
				// TRANSLATORS: Log message. Dispose Nursery.
				Logger.Log(T._("Clean up ..."));
				// TRANSLATORS: Log message. Dispose Nursery.
				Logger.Log(T._("- unload Bouyomi-chan ..."));
				if (this.bouyomichan != null) {
					this.bouyomichan.Dispose();
					this.bouyomichan = null;
				}
				// TRANSLATORS: Log message. Dispose Nursery.
				Logger.Log(T._("- unload sound devices ..."));
				// TRANSLATORS: Log message. Dispose Nursery.
				Logger.Log(T._("- unload discord ..."));
				if (this.discord != null) {
					this.discord.Dispose();
					this.discord = null;
				}
				// TRANSLATORS: Log message. Dispose Nursery.
				Logger.Log(T._("Disposing Done!"));
				GC.SuppressFinalize(this);
			} catch (Exception e) {
				// TRANSLATORS: Log message. Dispose Nursery.
				Logger.Log(T._("Could not clean up VoiceBot instance."));
				Logger.DebugLog(e.ToString());
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~VoiceBot() {
			Dispose(false);
		}

		#endregion

		#region IBot Properties

		public ulong Id {
			get {
				if (this.discord == null || this.discord.CurrentUser == null) { return 0; }
				return this.discord.CurrentUser.Id;
			}
		}

		public string IdString {
			get {
				return this.Id.ToString();
			}
		}

		public string Nickname {
			get {
				lock (state_lock_object) { // LOCK STATE
					return this.state.Nickname;
				}
			}
		}

		public string Username {
			get {
				if (this.discord == null || this.discord.CurrentUser == null) { return ""; }
				return this.discord.CurrentUser.Username;
			}
		}

		public ulong[] RoleIds {
			get {
				lock (state_lock_object) { // LOCK STATE
					return this.state.RoleIds;
				}
			}
		}

		public string[] RoleIdStrings {
			get {
				return this.RoleIds.Select(rid => rid.ToString()).ToArray();
			}
		}

		public bool IsJoined {
			get {
				lock (state_lock_object) { // LOCK STATE
					return this.state.Joined;
				}
			}
		}

		public ulong[] TextChannelIds {
			get {
				lock (state_lock_object) { // LOCK STATE
					return this.state.TextChannelIds.ToArray();
				}
			}
		}

		public string[] TextChannelIdStrings {
			get {
				return this.TextChannelIds.Select(i => i.ToString()).ToArray();
			}
		}

		public ulong VoiceChannelId {
			get {
				lock (state_lock_object) { // LOCK STATE
					return this.state.VoiceChannelId;
				}
			}
		}

		public string VoiceChannelIdString {
			get {
				return this.VoiceChannelId.ToString();
			}
		}

		#endregion

		#region IBot Methods

		public JoinChannelResult JoinChannel(Plugins.IMessage message) {
			var gu = message.Original.Author as SocketGuildUser;
			var gc = (message.Original.Channel as SocketGuildChannel);
			if (gu == null || gc == null) {
				return new JoinChannelResult() { State = JoinChannelState.WhereYouAre };
			}
			var voicech = gu.VoiceChannel;
			lock (state_lock_object) { // LOCK STATE
				if (this.state.Joined) {
					return new JoinChannelResult() { State = JoinChannelState.AlreadyJoined };
				}
				if (voicech == null) {
					return new JoinChannelResult() { State = JoinChannelState.WhereYouAre };
				}
				var t = this.voice.Connect(voicech);
				this.state.Join(message.Original.Channel, voicech, gc.Guild, this.discord.CurrentUser.Id);
				return new JoinChannelResult() { State = JoinChannelState.Succeed, VoiceChannelName = voicech.Name };
			}
		}

		public LeaveChannelResult LeaveChannel(Plugins.IMessage message) {
			lock(state_lock_object) { // LOCK STATE
				var t = this.voice.Disconnect();
				var ret = this.state.Joined ? LeaveChannelResult.Succeed : LeaveChannelResult.NotJoined;
				this.state.Leave();
				return ret;
			}
		}

		public AddChannelResult AddChannel(Plugins.IMessage message) {
			lock (state_lock_object) { // LOCK STATE
				if (!this.state.Joined) {
					return AddChannelResult.NotJoined;
				}
				if (this.state.AddTextChannel(message.Original.Channel)) {
					return AddChannelResult.Succeed;
				}
				return AddChannelResult.AlreadyAdded;
			}
		}

		public RemoveChannelResult RemoveChannel(Plugins.IMessage message) {
			lock (state_lock_object) { // LOCK STATE
				if (!this.state.Joined) {
					return RemoveChannelResult.NotJoined;
				}
				if (this.state.RemoveTextChannel(message.Original.Channel)) {
					return RemoveChannelResult.Succeed;
				}
				return RemoveChannelResult.AlreadyRemoved;
			}
		}

		public void SendMessageAsync(string[] TextChannelIds, string messageForFirst, string messageForOthers, bool CutIfTooLong) {
			if (TextChannelIds == null || TextChannelIds.Length == 0) {
				SendMessageAsync(this.state.DefaultTextChannel, null, messageForFirst, CutIfTooLong);
				return;
			}
			var sentChannels = new List<ulong>();
			foreach (var tcid_s in TextChannelIds) {
				ulong tcid;
				SocketTextChannel tc = null;
				if (ulong.TryParse(tcid_s, out tcid) && this.state.Guild != null) {
					if (sentChannels.Contains(tcid)) { continue; }
					tc = this.state.Guild.GetTextChannel(tcid);
				}
				if (tc == null) {
					// TRANSLATORS: Log message. SendMessageAsync. {0} is id of text channel.
					Logger.Log(T._("Could not find text channel id [{0}].", tcid_s));
					continue;
				}
				SendMessageAsync(tc, null, sentChannels.Count == 0 ? messageForFirst : messageForOthers, CutIfTooLong);
				sentChannels.Add(tcid);
			}
		}
		
		public void SendMessageAsync(ISocketMessageChannel channel, string message, bool CutIfTooLong) {
			SendMessageAsync(channel, null, message, CutIfTooLong);
		}

		public void SendMessageAsync(ISocketMessageChannel channel, SocketUser user, string message, bool CutIfTooLong) {
			if (channel == null) {
				channel = this.state.DefaultTextChannel;
			}
			if (channel == null) {
				Logger.DebugLog("No text channel found.");
				return;
			}
			var prefix = (user == null ? "" : user.Mention + " ");
			var msg = message;
			while (true) {
				if ((prefix + msg).Length <= Common.DISCORD_MAX_MESSAGE_LENGTH) {
					channel.SendMessageAsync(prefix + msg);
					break;
				}
				channel.SendMessageAsync(prefix + msg.Substring(0, Common.DISCORD_MAX_MESSAGE_LENGTH - prefix.Length));
				if (CutIfTooLong) { break; }
				msg = msg.Substring(Common.DISCORD_MAX_MESSAGE_LENGTH - prefix.Length);
			}
		}

		public void AddTalk(string message, Plugins.ITalkOptions options) {
			try {
				this.bouyomichan.AddTalkTask(message, options.Speed, options.Tone, options.Volume, options.Type);
			} catch (Exception e) {
				Logger.Log(e.ToString());
			}
		}

		public IPlugin GetPlugin(string PluginName) {
			return PluginManager.Instance.GetPlugin(PluginName);
		}

		public string AnnounceLabel { get; set; } = "";
		public string SpeakLabel { get; set; } = "";

		public string GetUserName(string UserId) {
			if (this.state.Guild == null) { return ""; }
			ulong uid;
			if (!ulong.TryParse(UserId, out uid)) { return ""; }
			var u = this.state.Guild.GetUser(uid);
			if (u == null) { return ""; }
			return u.Username;
		}
		
		public string GetNickName(string UserId) {
			if (this.state.Guild == null) { return ""; }
			ulong uid;
			if (!ulong.TryParse(UserId, out uid)) { return ""; }
			var u = this.state.Guild.GetUser(uid);
			if (u == null) { return ""; }
			if (u.Nickname != null && u.Nickname.Length > 0) { return u.Nickname; }
			return u.Username;
		}

		public string[] GetUserIdsInVoiceChannel() {
			if (!this.state.Joined || this.state.Guild == null || this.state.VoiceChannelId < 0) { return new string[] { }; }
			var vc = this.state.Guild.GetVoiceChannel(this.state.VoiceChannelId);
			if (vc == null) { return new string[] { }; }
			return vc.Users.Select(u => u.Id.ToString()).Distinct().OrderBy(s => s).ToArray();
		}
		
		public void AddSchedules(IScheduledTask[] schedules) {
			this.timer.Stop();
			lock (schedule_lock_object) { // LOCK SCHEDULE
				this.Schedules.AddRange(schedules);
			}
			this.timer.Start();
		}

		public void RemoveSchedules(IScheduledTask[] schedules) {
			this.timer.Stop();
			lock (schedule_lock_object) { // LOCK SCHEDULE
				foreach (var s in schedules) {
					this.Schedules.Remove(s);
				}
			}
			this.timer.Start();
		}

		public void ClearSchedule() {
			this.timer.Stop();
			lock (schedule_lock_object) { // LOCK SCHEDULE
				this.Schedules.Clear();
			}
			this.timer.Start();
		}

		#endregion

		#region Events

		async Task Client_MessageReceived(SocketMessage message) {
			var mes = PluginManager.Instance.ExecutePlugins(this, message);
			// TRANSLATORS: DebugLog message. {0} is message content.
			Logger.DebugLog(T._("* Message [{0}]", mes.Content));
			// TRANSLATORS: DebugLog message. {0} is applied plugin names list.
			Logger.DebugLog(T._("* Applied plugins: {0}", String.Join(", ", mes.AppliedPlugins)));
			if (mes.Content.Length == 0) { return; }
			await Task.Run((Action)(() => { this.AddTalk(mes.Content, mes.TalkOptions); }));
		}

		private void TickHandler() {
			lock (schedule_lock_object) { // LOCK SCHEDULE
				if (this.Schedules.Count > 0) {
					var newschedules = new List<IScheduledTask>();
					foreach (var s in this.Schedules) {
						var ret = s.Execute(this);
						if (ret != null && ret.Length > 0) {
							newschedules.AddRange(ret);
						}
					}
					this.Schedules = this.Schedules.Where(s => !s.Finished).ToList();
					this.Schedules.AddRange(newschedules);
				}
			}
		}

		#endregion
	}
}
