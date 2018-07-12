using Nursery.Options;
using Nursery.Plugins;
using Nursery.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nursery.UserDefinedFilterPlugin {
	public class UserDefinedFilter : IPlugin {
		public string Name { get; } = "Nursery.UserDefinedFilterPlugin.UserDefinedFilter";
		// TRANSLATORS: Bot-Help message. UserDefinedFilter plugin.
		public string HelpText { get; } = T._("Filter with user-defined patterns.");
		Plugins.Type IPlugin.Type => Plugins.Type.Filter;

		private UserDefinedFilterConfig config = null;
		private FilterConfig[] filters = null;

		public void Initialize(IPluginManager loader, IPlugin[] plugins) {
			this.config = loader.GetPluginSetting<UserDefinedFilterConfig>(this.Name);
			Reload();
		}

		private Filters GetFilters(string dir, string configfile) {
			if (!Directory.Exists(dir)) {
				dir = Path.Combine(Directory.GetCurrentDirectory(), dir);
			}
			if (!Directory.Exists(dir)) {
				// TRANSLATORS: Log message. UserDefinedFilter plugin. {0} is directory.
				Logger.Log(T._("Filters directory [{0}] is not found.", dir));
				return null;
			}
			var ret = new Filters();
			var config = Path.Combine(dir, configfile);
			if (!File.Exists(config)) {
				// TRANSLATORS: Log message. UserDefinedFilter plugin. {0} is file path.
				Logger.Log(T._("Filters config file [{0}] is not found.", config));
				ret.Config = new FiltersConfig();
			} else {
				try {
					ret.Config = Config.Instance.LoadConfig<FiltersConfig>(config);
				} catch (Exception e) {
					// TRANSLATORS: Log message. UserDefinedFilter plugin. {0} is file path.
					Logger.Log(T._("Could not load filters config [{0}].", config));
					Logger.DebugLog(e.ToString());
					ret.Config = new FiltersConfig();
				}
			}
			var dir_fullpath = Path.GetFullPath(dir);
			if (dir_fullpath.LastIndexOf(Path.DirectorySeparatorChar) != dir_fullpath.Length - 1) {
				dir_fullpath = dir_fullpath + Path.DirectorySeparatorChar;
			}
			ret.FilterList = Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories).Where(p => !p.Equals(config)).Select(path => {
				var filename = Path.GetFileNameWithoutExtension(path);
				if (ret.Config.Disabled.Contains(filename)) { return null; }
				var extention = Path.GetExtension(path);
				var fullpath = Path.GetFullPath(path);
				var relative = fullpath.Replace(dir_fullpath, "").Replace(extention, "");
				try {
					var f = Config.Instance.LoadConfig<FilterConfig>(path);
					f.Path = relative;
					f.Initialize();
					return f;
				} catch (Exception e) {
					// TRANSLATORS: Log message. UserDefinedFilter plugin. {0} is file path.
					Logger.Log(T._("Could not load filter config [{0}].", path));
					Logger.DebugLog(e.ToString());
					return null;
				}
			}).Where(f => f != null).ToList();
			return ret;
		}

		public void Reload() {
			this.filters = null;
			JSWrapper.Instance.Reset();
			// * set types to JS engine before PatternConfig.Initialize().
			JSWrapper.Instance.SetType("JSArgument", typeof(JSArgument));
			// find filter files and load them
			var fs = GetFilters(this.config.Dir, this.config.ConfigFile);
			if (fs == null) { return; }
			// sort filters
			var before_asterisk = new List<FilterConfig>();
			var after_asterisk = new List<FilterConfig>();
			var asterisk_found = false;
			foreach (var orderitem in fs.Config.Filters) {
				if (orderitem.Equals("*")) {
					asterisk_found = true;
					continue;
				}
				var f = fs.FilterList.FirstOrDefault(filter => filter.Path == orderitem);
				if (f == null) { continue; }
				if (asterisk_found) {
					after_asterisk.Add(f);
				} else {
					before_asterisk.Add(f);
				}
			}
			var l = new List<FilterConfig>();
			l.AddRange(before_asterisk);
			if (asterisk_found) {
				var asterisk = fs.FilterList.Where(f => !before_asterisk.Contains(f)).Where(f => !after_asterisk.Contains(f)).ToList();
				l.AddRange(asterisk);
			}
			l.AddRange(after_asterisk);
			this.filters = l.ToArray();
		}

		public bool Execute(IBot bot, IMessage message) {
			if (this.filters == null || this.filters.Length == 0) { return false; }
			var original_message = "" + message.Content;
			foreach (var filter in this.filters) {
				switch (filter.Type) {
					case FilterType.String:
						message.Content = message.Content.Replace(filter.StringPattern, filter.ReplaceTo);
						break;
					case FilterType.Regex:
						message.Content = filter.RegexPattern.Replace(message.Content, filter.ReplaceTo);
						break;
					case FilterType.Function:
						var ret = filter.FunctionPattern(new JSArgument(bot, message));
						if (ret == null) { continue; }
						message.Content = ret;
						break;
					default:
						continue;
				}
			}
			if (!original_message.Equals(message.Content)) {
				message.AppliedPlugins.Add(this.Name);
				return true;
			}
			return false;
		}
	}

	class Filters {
		public FiltersConfig Config;
		public List<FilterConfig> FilterList;
	}
}
