using System.Text.RegularExpressions;

namespace Nursery.Plugins {
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
