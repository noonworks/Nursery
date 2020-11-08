using CommandLine;
using Nursery.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Nursery {
	class Program {
		static void Main(string[] args) {
			Logger.Log("Nursery " + Common.PRODUCT_VERSION);
			Logger.Log("--------------------");
			Console.CancelKeyPress += (s, e) => {
				VoiceBot.FreeInstance();
				Environment.Exit(0);
			};
			CommandLine.Parser.Default.ParseArguments<Options.CommandlineOptions>(args)
			  .WithParsed<Options.CommandlineOptions>(opts => RunOptionsAndReturnExitCode(opts))
			  .WithNotParsed<Options.CommandlineOptions>((errs) => HandleParseError(errs));
			while (true) {
				var command = Console.ReadLine();
				if (command != null && command.ToLower() == "exit") {
					VoiceBot.FreeInstance();
					Environment.Exit(0);
				}
			}
		}

		static void ShowAssemblyVersion(Type t) {
			try {
				var asm = t.Assembly;
				var asmname = asm.GetName();
				var path = (new Uri(asm.CodeBase)).LocalPath;
				var fv = FileVersionInfo.GetVersionInfo(path);
				Logger.DebugLog(Path.GetFileName(path));
				Logger.DebugLog("  ProductVersion [" + fv.ProductVersion + "] FileVersion [" + fv.FileVersion + "] AssemblyVersion [" + asmname.Version.ToString() + "]");
			} catch (Exception) {
				Logger.DebugLog("  COULD NOT GET ASSEMBLY OF [" + t.Name + "]");
			}
		}

		public static void ShowVersions() {
			Logger.DebugLog("--------------------");
			Logger.DebugLog("DEBUG MODE");
			Logger.DebugLog("--------------------");
			ShowAssemblyVersion(typeof(Program));
			ShowAssemblyVersion(typeof(FNF.Utility.BouyomiChanClient));
			ShowAssemblyVersion(typeof(Nursery.Options.MainConfig));
			ShowAssemblyVersion(typeof(Nursery.Plugins.IPlugin));
			ShowAssemblyVersion(typeof(Nursery.Utility.Common));
			Logger.DebugLog("--------------------");
		}

		private static void RunOptionsAndReturnExitCode(Options.CommandlineOptions opts) {
			var t = VoiceBot.CreateInstanceAsync(opts);
		}

		private static object HandleParseError(object errs) {
			foreach(Error e in (IEnumerable<Error>)errs) {
				Console.Error.WriteLine(e.ToString());
			}
			return null;
		}
	}
}
