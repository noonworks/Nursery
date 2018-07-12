using CommandLine;
using System;
using System.Collections.Generic;

namespace Nursery {
	class Program {
		static void Main(string[] args) {
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
