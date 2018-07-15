using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Nursery.Options;
using Nursery.Plugins;
using Nursery.Utility;

namespace Nursery.SoundEffectPlugin {
	public class SoundEffectCommand : IPlugin {
		public string Name { get; } = "Nursery.SoundEffectPlugin.SoundEffectCommand";
		// TRANSLATORS: Bot-Help message. SoundEffectCommand plugin.
		public string HelpText { get; } = T._("Play sounds.") + " ```sound name\nse name```";
		Plugins.Type IPlugin.Type => Plugins.Type.Command;

		private SoundEffectCommandConfig config = null;
		private BinaryPool<UnsafeBinaryFile, SafeBinaryHandle> pool = null;
		private SoundConfig[] _soundconfigs = new SoundConfig[] { };

		public SoundConfig[] SoundConfigs {
			get => _soundconfigs;
		}

		private static Regex SOUND = new Regex(@"([^a-zA-Z0-9]|\b)(?:se|sound)\s+([^\s]+)");

		private string[] GetJsonPaths(string dir) {
			if (!Directory.Exists(dir)) {
				dir = Path.Combine(Directory.GetCurrentDirectory(), dir);
			}
			if (!Directory.Exists(dir)) {
				// TRANSLATORS: Log message. SoundEffectCommand plugin. {0} is directory.
				Logger.Log(T._("Sounds directory [{0}] is not found.", dir));
				return new string[] { };
			}
			return Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories);
		}

		public void Reload() {
			JSWrapper.Instance.Reset();
			// * set types to JS engine before PatternConfig.Initialize().
			JSWrapper.Instance.SetType("JSArgument", typeof(JSArgument));
			JSWrapper.Instance.SetType("ReplaceResult", typeof(ReplaceResult));
			// find music files
			var jsons = GetJsonPaths(this.config.Dir);
			_soundconfigs = jsons.Select(json => {
				SoundConfig c;
				try {
					c = Config.Instance.LoadConfig<SoundConfig>(json);
					c.ConfigFile = json;
					c.File = Path.Combine(Path.GetDirectoryName(json), c.File);
					foreach (var p in c.Patterns) { p.Initialize(); }
					return c;
				} catch (Exception) {
					// TRANSLATORS: Log message. SoundEffectCommand plugin. {0} is file path.
					Logger.Log(T._("* Could not load [{0}].", json));
					return null;
				}
			}).Where(c => c != null).ToArray();
			// load musics
			this.pool.Clear();
			this.pool.AddFiles(_soundconfigs.Select(s => s.File).ToArray());
		}

		public void Stop() {
			PlayQueue.Instance.Stop();
		}

		public void Initialize(IPluginManager loader, IPlugin[] plugins) {
			this.config = loader.GetPluginSetting<SoundEffectCommandConfig>(this.Name);
			this.pool = new BinaryPool<UnsafeBinaryFile, SafeBinaryHandle>();
			this.pool.MemoryMax = this.config.MemoryMax;
			PlayQueue.Instance.MaxPlayInParallel = this.config.MaxPlayInParallel;
			Reload();
		}

		private class SoundIdentifer {
			public string Name = "";
			public int PatternIndex = -1;
			public int IdentifyIndex = 0;
			public SoundConfig SoundConfig = null;

			public override string ToString() {
				var prefix = (PatternIndex >= 0 ? "-p" + PatternIndex + "-" : "-") + IdentifyIndex;
				if (Name.Length > 0) {
					return Name + prefix;
				}
				if (SoundConfig != null) {
					return SoundConfig.File + prefix;
				}
				return "(UNKNOWN)" + prefix;
			}

			public string ToName() {
				if (Name.Length > 0) { return Name; }
				if (SoundConfig != null) { return SoundConfig.File; }
				return "(LOAD_CONFIG_ERROR)";
			}
		}

		#region FindPatterns

		private class MatchResult {
			public string Replaced;
			public int Count = 0;
			public string[] SoundNames = new string[] { };

			public MatchResult(string message) {
				this.Replaced = message;
			}
		}

		private MatchResult FindRegexPattern(PatternConfig p_conf, string message) {
			var ret = new MatchResult(message);
			ret.Replaced = p_conf.RegexPattern.Replace(message, (Match match) => {
				ret.Count++;
				if (p_conf.ReplaceTo == null) {
					return match.Groups[0].Value;
				} else {
					return p_conf.ReplaceTo;
				}
			});
			return ret;
		}

		private MatchResult FindStringPattern(PatternConfig p_conf, string message) {
			var ret = new MatchResult(message);
			var pos = 0;
			while (true) {
				pos = message.IndexOf(p_conf.StringPattern, pos);
				if (pos >= 0) { ret.Count++; pos++; }
				if (pos < 0 || pos >= message.Length) { break; }
			}
			if (ret.Count > 0) {
				if (p_conf.ReplaceTo != null) {
					ret.Replaced = message.Replace(p_conf.StringPattern, p_conf.ReplaceTo);
				}
			}
			return ret;
		}

		private MatchResult FindFunctionPattern(PatternConfig p_conf, IBot bot, IMessage message) {
			var ret = new MatchResult(message.Content);
			var r = JSWrapper.Instance.ExecuteFunction(p_conf.FunctionName, new JSArgument(bot, message));
			var result = r as ReplaceResult;
			if (result == null) { return ret; }
			ret.Replaced = result.Result;
			ret.SoundNames = result.SoundNames;
			return ret;
		}

		private List<SoundIdentifer> FindPatterns(IBot bot, IMessage message) {
			var ret = new List<SoundIdentifer>();
			foreach (var s_conf in this._soundconfigs) {
				for (var p_idx = 0; p_idx < s_conf.Patterns.Count; p_idx++) {
					var p_conf = s_conf.Patterns[p_idx];
					MatchResult result = null;
					switch (p_conf.Type) {
						case PatternType.String:
							result = FindStringPattern(p_conf, message.Content);
							break;
						case PatternType.Regex:
							result = FindRegexPattern(p_conf, message.Content);
							break;
						case PatternType.Function:
							result = FindFunctionPattern(p_conf, bot, message);
							break;
					}
					if (result == null) { continue; }
					for (var i = 0; i < result.Count; i++) {
						ret.Add(new SoundIdentifer() { SoundConfig = s_conf, PatternIndex = p_idx, IdentifyIndex = i });
					}
					for (var i = 0; i < result.SoundNames.Length; i++) {
						if (result.SoundNames[i].Length > 0) {
							ret.Add(new SoundIdentifer() { Name = result.SoundNames[i], PatternIndex = p_idx, IdentifyIndex = i });
						}
					}
					if (result.Count > 0 || result.SoundNames.Length > 0) {
						message.Content = result.Replaced;
					}
				}
			}
			return ret;
		}

		#endregion

		#region FindSoundNames

		private List<SoundIdentifer> FindSoundNames(IMessage message) {
			var ret = new List<SoundIdentifer>();
			var cnt = 0;
			message.Content = SOUND.Replace(message.Content, (Match match) => {
				if (match.Groups.Count != 3) { return match.Groups[0].Value; }
				ret.Add(new SoundIdentifer() { Name = match.Groups[2].Value, IdentifyIndex = cnt });
				cnt++;
				return match.Groups[1].Value;
			});
			return ret;
		}

		#endregion

		public bool Execute(IBot bot, IMessage message) {
			var org_msg = message.Content;
			var sounds = new List<SoundIdentifer>();
			// 1. pattern matching
			sounds.AddRange(FindPatterns(bot, message));
			// 2. get se|sound names
			sounds.AddRange(FindSoundNames(message));
			// 3. check results
			if (sounds.Count == 0) {
				if (!org_msg.Equals(message.Content)) {
					message.AppliedPlugins.Add(this.Name);
					return true;
				}
				return false;
			}
			// 4. play sounds
			var notfound = new List<SoundIdentifer>();
			var cannotplay = new List<SoundIdentifer>();
			foreach (var sidr in sounds) {
				if (sidr.SoundConfig == null) {
					sidr.SoundConfig = this._soundconfigs.FirstOrDefault(s => s.Aliases.Contains(sidr.Name));
					if (sidr.SoundConfig == null) {
						notfound.Add(sidr);
						continue;
					}
				}
				var identifier = sidr.ToString() + DateTime.Now.ToString("-HH:mm:ss.fff");
				try {
					var data = this.pool.GetData(sidr.SoundConfig.File);
					var player = new SoundPlayer(identifier, data, sidr.SoundConfig.Volume);
					PlayQueue.Instance.Add(player);
				} catch (Exception) {
					cannotplay.Add(sidr);
				}
			}
			// 5. error message
			if (notfound.Count > 0) {
				bot.SendMessageAsync(message.Original.Channel, message.Original.Author,
					// TRANSLATORS: Bot message. SoundEffectCommand plugin. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
					T._n("Sound file is not found!", "Sound files are not found!", notfound.Count, notfound.Count) + 
					" `" + String.Join(", ", notfound.Select(e => e.ToName())) + "`", true);
			}
			if (cannotplay.Count > 0) {
				bot.SendMessageAsync(message.Original.Channel, message.Original.Author,
					// TRANSLATORS: Bot message. SoundEffectCommand plugin. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
					T._n("Could not play sound file.", "Could not play sound files.", cannotplay.Count, notfound.Count) +
					" `" + String.Join(", ", cannotplay.Select(e => e.ToName())) + "`", true);
			}
			// 6. set results
			message.AppliedPlugins.Add(this.Name);
			return true;
		}
	}
}
