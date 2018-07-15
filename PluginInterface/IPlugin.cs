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

	public interface IUser {
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
		JoinChannelResult JoinChannel(IMessage message);
		LeaveChannelResult LeaveChannel(IMessage message);
		AddChannelResult AddChannel(IMessage message);
		RemoveChannelResult RemoveChannel(IMessage message);
		void SendMessageAsync(ISocketMessageChannel channel, string message, bool CutIfToLong);
		void SendMessageAsync(ISocketMessageChannel channel, SocketUser user, string message, bool CutIfToLong);
		bool IsJoined { get; }
		List<ulong> TextChannelIds { get; }
		ulong VoiceChannelId { get; }
		IPlugin GetPlugin(string PluginName);
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
