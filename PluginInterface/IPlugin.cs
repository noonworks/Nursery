using Discord.WebSocket;
using FNF.Utility;
using System.Collections.Generic;

namespace Nursery.Plugins {
	public enum Type {
		Command,
		Filter
	}

	public interface IPlugin {
		string Name { get; }
		string HelpText { get; }
		Type Type { get; }

		void Initialize(IPluginManager loader, IPlugin[] plugins);
		bool Execute(IBot bot, IMessage message);
	}

	public interface ITalkOptions {
		int Speed { get; set; }
		int Tone { get; set; }
		int Volume { get; set; }
		VoiceType Type { get; set; }
	}

	public interface IMessage {
		SocketMessage Original { get; }
		string Content { get; set; }
		List<string> AppliedPlugins { get; }
		ITalkOptions TalkOptions { get; }
		bool Terminated { get; set; }
	}

	public interface IJSArgument {
		IBot Bot { get; }
		IMessage Message { get; }
		IJSArgumentUser Author { get; }
		string[] MentionedUsers { get; }
		string GuildId { get; }
		string ChannelId { get; }
	}

	public interface IJSArgumentUser {
		string Id { get; }
		string Nickname { get; }
		string Username { get; }
		string[] RoleIds { get; }
	}

	public interface IBot {
		ulong Id { get; }
		string IdString { get; }
		string Nickname { get; }
		string Username { get; }
		ulong[] RoleIds { get; }
		string[] RoleIdStrings { get; }
		bool IsJoined { get; }
		ulong[] TextChannelIds { get; }
		string[] TextChannelIdStrings { get; }
		ulong VoiceChannelId { get; }
		string VoiceChannelIdString { get; }
		JoinChannelResult JoinChannel(IMessage message);
		LeaveChannelResult LeaveChannel(IMessage message);
		AddChannelResult AddChannel(IMessage message);
		RemoveChannelResult RemoveChannel(IMessage message);
		void SendMessageAsync(ISocketMessageChannel channel, string message, bool CutIfTooLong);
		void SendMessageAsync(ISocketMessageChannel channel, SocketUser user, string message, bool CutIfTooLong);
		void AddTalk(string message, ITalkOptions options);
		IPlugin GetPlugin(string PluginName);
		void AddSchedule(IScheduledTask schedule);
		void ClearSchedule();
	}

	public interface IScheduledTask {
		string Name { get; }
		bool Finished { get; }
		IScheduledTask[] Execute(IBot bot);
	}

	public class JoinChannelResult {
		public string VoiceChannelName;
		public JoinChannelState State;
	}

	public enum JoinChannelState {
		AlreadyJoined,
		WhereYouAre,
		Succeed
	}

	public enum LeaveChannelResult {
		NotJoined,
		Succeed
	}

	public enum AddChannelResult {
		NotJoined,
		AlreadyAdded,
		Succeed
	}

	public enum RemoveChannelResult {
		NotJoined,
		AlreadyRemoved,
		Succeed
	}

	public interface IPluginManager {
		T GetPluginSetting<T>(string PluginName);
		T LoadConfig<T>(string path);
		IMessage ExecutePlugins(IBot bot, SocketMessage message);
	}
}
