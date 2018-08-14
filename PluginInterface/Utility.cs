using Discord.WebSocket;
using FNF.Utility;
using System;
using System.Text.RegularExpressions;

namespace Nursery.Plugins {
	public class Utility {
		private const string UserNameRegex = @"\${username([0-9]+)}";
		private const string NickNameRegex = @"\${nickname([0-9]+)}";
		public static string ReplaceDiscordValues(string text, IBot bot, DateTime dt, SocketMessage Original = null) {
			text = Nursery.Utility.Messages.ReplaceGeneralValues(text, dt);
			// replace reply
			if (Original != null) {
				text = text.Replace("${reply}", Original.Author.Mention);
				text = text.Replace("${username}", Original.Author.Username);
				var nn = bot.GetNickName(Original.Author.Id.ToString());
				text = text.Replace("${nickname}", nn);
			}
			// replace username and nickname
			text = Regex.Replace(text, UserNameRegex, m => bot.GetUserName(m.Groups[1].Value));
			text = Regex.Replace(text, NickNameRegex, m => bot.GetNickName(m.Groups[1].Value));
			// replace announce or speak flag
			text = text.Replace("${announce}", bot.AnnounceLabel).Replace("${speak}", bot.SpeakLabel);
			return text;
		}
	}

	public class TalkOptions : ITalkOptions {
		public int Speed { get; set; } = -1;
		public int Tone { get; set; } = -1;
		public int Volume { get; set; } = 50;
		public VoiceType Type { get; set; } = VoiceType.Default;
	}
	
	public abstract class AbstractKeywordCommand : IPlugin {
		protected string[] keywords;
		protected Regex[] regexs;

		abstract public string Name { get; }
		abstract public string HelpText { get; }
		Plugins.Type IPlugin.Type => Plugins.Type.Command;

		protected AbstractKeywordCommand(string[] keywords) {
			this.keywords = keywords;
			this.regexs = new Regex[keywords.Length];
			for (int i = 0; i < this.keywords.Length; i++) {
				this.regexs[i] = new Regex(@"\b" + this.keywords[i] + @"\b");
			}
		}

		abstract public void Initialize(IPluginManager loader, IPlugin[] plugins);

		abstract protected bool DoExecute(int keywordIndex, IBot bot, IMessage message);

		public virtual bool Execute(IBot bot, IMessage message) {
			for (int i = 0; i < this.keywords.Length; i++) {
				if (this.regexs[i].IsMatch(message.Content)) {
					return DoExecute(i, bot, message);
				}
			}
			return false;
		}

		protected string RemoveKeyword(int keywordIndex, IMessage message) {
			return this.regexs[keywordIndex].Replace(message.Content, "");
		}
	}

	public abstract class AbstractMentionKeywordCommand : AbstractKeywordCommand {

		protected AbstractMentionKeywordCommand(string[] keywords) : base(keywords) {}

		public override bool Execute(IBot bot, IMessage message) {
			if (!Nursery.Utility.Messages.IsMentionFor(bot.Id, message.Original)) { return false; }
			return base.Execute(bot, message);
		}
	}
}
