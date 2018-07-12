using Discord;
using Discord.WebSocket;
using FNF.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nursery.Options;
using Nursery.Plugins;
using Nursery.Utility;

namespace Nursery {
	class BotState {
		public bool Joined { get; set; } = false;
		public List<ulong> TextChannelIds { get; set; } = new List<ulong>();
		public ulong VoiceChannelId { get; set; } = 0;
	}

	class VoiceBot : IDisposable, IBot {
		private static VoiceBot instance = null;

		private object state_lock_bject = new object();
		private BotState state;
		private BouyomiChanClient bouyomichan = null;
		private DiscordSocketClient discord = null;
		private VoiceChat voice = null;

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

		private VoiceBot() {
			this.state = new BotState();
			// TRANSLATORS: Log message. Initializing Nursery.
			Logger.Log(T._("- initialize Bouyomi-chan ..."));
			this.bouyomichan = new BouyomiChanClient();
			try {
				this.bouyomichan.ClearTalkTasks();
			} catch (Exception e) {
				this.Dispose();
				// TRANSLATORS: Error message. Initializing Nursery.
				throw new Exception(T._("Could not load Bouyomi-chan. Please check Bouyomi-chan awaking."), e);
			}
			// TRANSLATORS: Log message. Initializing Nursery.
			Logger.Log(T._("- initialize sound devices ..."));
			try {
				BassWrapper.Initialize(Config.Instance.MainConfig);
			} catch (Exception e) {
				this.Dispose();
				// TRANSLATORS: Error message. Initializing Nursery.
				throw new Exception(T._("Could not initialize sound devices. Please check virtual devices installed and valid name set."), e);
			}
			try {
				this.voice = new VoiceChat(Config.Instance.MainConfig);
			} catch (Exception e) {
				this.Dispose();
				// TRANSLATORS: Error message. Initializing Nursery.
				throw new Exception(T._("Could not initialize recording devices. Please check virtual devices installed and valid name set."), e);
			}
			// TRANSLATORS: Log message. Initializing Nursery.
			Logger.Log(T._("- initialize Discord client ..."));
			this.discord = new DiscordSocketClient();
			// TRANSLATORS: Log message. Initializing Nursery.
			Logger.Log(T._("- load plugins ..."));
			PluginManager.Instance.Load(this);
		}

		private void Disconnect() {
			try {
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
				BassWrapper.Free();
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

		public ulong Id {
			get {
				if (this.discord == null || this.discord.CurrentUser == null) { return 0; }
				return this.discord.CurrentUser.Id;
			}
		}

		public string Username {
			get {
				if (this.discord == null || this.discord.CurrentUser == null) { return ""; }
				return this.discord.CurrentUser.Username;
			}
		}

		public IPlugin GetPlugin(string PluginName) {
			return PluginManager.Instance.GetPlugin(PluginName);
		}

		public void SendMessageAsync(ISocketMessageChannel channel, string message) {
			channel.SendMessageAsync(message);
		}

		public void AddTalk(string message, Plugins.ITalkOptions options) {
			try {
				this.bouyomichan.AddTalkTask(message, options.Speed, options.Tone, options.Volume, options.Type);
			} catch (Exception e) {
				Logger.Log(e.ToString());
			}
		}

		public JoinChannelResult JoinChannel(Plugins.IMessage message) {
			var voicech = (message.Original.Author as SocketGuildUser).VoiceChannel;
			lock (state_lock_bject) { // LOCK STATE
				if (this.state.TextChannelIds.Count > 0 || this.state.VoiceChannelId != 0) {
					return new JoinChannelResult() { State = JoinChannelState.AlreadyJoined };
				}
				if (voicech == null) {
					return new JoinChannelResult() { State = JoinChannelState.WhereYouAre };
				}
				var t = this.voice.Connect(voicech);
				this.state.TextChannelIds.Add(message.Original.Channel.Id);
				this.state.VoiceChannelId = voicech.Id;
				this.state.Joined = true;
				return new JoinChannelResult() { State = JoinChannelState.Succeed, VoiceChannelName = voicech.Name };
			}
		}

		public LeaveChannelResult LeaveChannel(Plugins.IMessage message) {
			lock(state_lock_bject) { // LOCK STATE
				var t = this.voice.Disconnect();
				var ret = this.state.Joined ? LeaveChannelResult.Succeed : LeaveChannelResult.NotJoined;
				this.state.TextChannelIds = new List<ulong>();
				this.state.VoiceChannelId = 0;
				this.state.Joined = false;
				return ret;
			}
		}

		public AddChannelResult AddChannel(Plugins.IMessage message) {
			lock (state_lock_bject) { // LOCK STATE
				if (!this.state.Joined) {
					return AddChannelResult.NotJoined;
				}
				if (!this.state.TextChannelIds.Contains(message.Original.Channel.Id)) {
					this.state.TextChannelIds.Add(message.Original.Channel.Id);
					return AddChannelResult.Succeed;
				}
				return AddChannelResult.AlreadyAdded;
			}
		}

		public RemoveChannelResult RemoveChannel(Plugins.IMessage message) {
			lock (state_lock_bject) { // LOCK STATE
				if (!this.state.Joined) {
					return RemoveChannelResult.NotJoined;
				}
				if (this.state.TextChannelIds.Contains(message.Original.Channel.Id)) {
					this.state.TextChannelIds.Remove(message.Original.Channel.Id);
					return RemoveChannelResult.Succeed;
				}
				return RemoveChannelResult.AlreadyRemoved;
			}
		}

		async Task Client_MessageReceived(SocketMessage message) {
			var mes = PluginManager.Instance.ExecutePlugins(this, message);
			// TRANSLATORS: DebugLog message. {0} is message content.
			Logger.DebugLog(T._("* Message [{0}]", mes.Content));
			// TRANSLATORS: DebugLog message. {0} is applied plugin names list.
			Logger.DebugLog(T._("* Applied plugins: {0}", String.Join(", ", mes.AppliedPlugins)));
			if (mes.Content.Length == 0) { return; }
			await Task.Run((Action)(() => { this.AddTalk(mes.Content, mes.TalkOptions); }));
		}

		public bool IsJoined {
			get {
				lock (state_lock_bject) { // LOCK STATE
					return this.state.Joined;
				}
			}
		}

		public List<ulong> TextChannelIds {
			get {
				lock (state_lock_bject) { // LOCK STATE
					return this.state.TextChannelIds;
				}
			}
		}
		
		public ulong VoiceChannelId {
			get {
				lock (state_lock_bject) { // LOCK STATE
					return this.state.VoiceChannelId;
				}
			}
		}
	}
}
