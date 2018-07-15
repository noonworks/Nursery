using Nursery.Utility;
using System.Threading.Tasks;

namespace Nursery {
	delegate void TickHandler();

	class Timer {
		public int TickMilliSeconds { get; set; } = 100;
		private bool Watching = false;
		private TickHandler Handler = null;

		public Timer(TickHandler handler) {
			this.Handler = handler;
		}

		public void Start() {
			if (!this.Watching) {
				this.Watching = true;
				var t = Task.Run(WatchLoop); // run in other thread
			}
		}

		public void Stop() {
			this.Watching = false;
		}
		
		private async Task WatchLoop() {
			Logger.DebugLog("*** Timer started.");
			while (true) {
				if (!Watching) { break; }
				this.Handler();
				// wait to next loop
				await Task.Delay(TickMilliSeconds).ConfigureAwait(false);
			}
			Logger.DebugLog("*** Timer stopped.");
		}
	}
}
