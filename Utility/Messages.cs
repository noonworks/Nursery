using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Nursery.Utility {
	public class Messages {
		private static Regex mentionregex = new Regex(@"\s*(\<\@!?[0-9]+\>)\s*");

		public static bool IsMentionFor(ulong BotId, SocketMessage Message) {
			foreach (var user in Message.MentionedUsers) {
				if (user.Id == BotId) {
					return true;
				}
			}
			return false;
		}

		public class TrimResult {
			public TrimResult(string trimmed, string[] mentions) {
				this.Trimmed = trimmed;
				this.MentionsTo = mentions;
			}
			public string Trimmed { get; }
			public string[] MentionsTo { get; }
		}

		public static TrimResult TrimMention(string content) {
			var m = mentionregex.Matches(content);
			if (m.Count > 0) {
				var mentions = new List<string>();
				for(var i = 0; i < m.Count; i++) {
					for (var j = 0; j < m[i].Groups.Count - j; j++) {
						mentions.Add(m[i].Groups[j + 1].Value);
					}
				}
				return new TrimResult(mentionregex.Replace(content, ""), mentions.ToArray());
			}
			return new TrimResult(content, new string[0]);
		}

		private static readonly string[] DTFormatStrings = new string[] { "yyyy", "yy", "MMMM", "MM", "M", "dddd", "ddd", "dd", "d", "HH", "H", "mm", "m" };
		public static string ReplaceGeneralValues(string text, DateTime dt) {
			// replace now datetime
			foreach (var dts in DTFormatStrings) {
				text = text.Replace("${" + dts + "}", dt.ToString(dts.Length == 1 ? "%" + dts : dts));
			}
			return text;
		}
	}
}
