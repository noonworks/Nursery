using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nursery.Utility;

namespace Nursery.SoundEffectPlugin {
	class PlayQueue {
		#region Singleton
		private static PlayQueue _instance = null;

		public static PlayQueue Instance {
			get {
				if (_instance == null) {
					_instance = new PlayQueue();
				}
				return _instance;
			}
		}

		private PlayQueue() { }
		#endregion

		private Queue<SoundPlayer> _queue = new Queue<SoundPlayer>();
		private List<SoundPlayer> _playing = new List<SoundPlayer>();
		private bool _watching = false;
		private SemaphoreSlim collections_lock_sem = new SemaphoreSlim(1, 1);

		public int MaxPlayInParallel { get; set; } = 10;

		public void Stop() {
			foreach (var player in this._playing) {
				player.Stop();
			}
		}

		public void Add(SoundPlayer player) {
			try {
				collections_lock_sem.Wait(); // LOCK COLLECTIONS
				this._queue.Enqueue(player);
				if (!this._watching) {
					var t = Task.Run(WatchLoop); // run in other thread
					this._watching = true;
				}
			} finally {
				collections_lock_sem.Release(); // RELEASE COLLECTIONS
			}
		}

		private async Task WatchLoop() {
			Logger.DebugLog("* Start SE watching.");
			while (true) {
				try {
					// LOCK COLLECTIONS
					collections_lock_sem.Wait();
					// check finished player
					if (this._playing.Count > 0) {
						this._playing = this._playing.Where(player => {
							if (player.State == PlayState.Stopped) {
								Logger.DebugLog(" - " + player.Name + " is finished.");
								return false;
							}
							return true;
						}).ToList();
					}
					// add new player from queue
					if (this._queue.Count > 0 && (this.MaxPlayInParallel <= 0 || this._playing.Count < this.MaxPlayInParallel)) {
						var player = this._queue.Dequeue();
						Logger.DebugLog(" - Start [" + player.Name + "]");
						player.Play();
						this._playing.Add(player);
					}
					// stop waching if no queue and no playing
					if (this._queue.Count == 0 && this._playing.Count == 0) {
						this._watching = false;
						Logger.DebugLog("* Stop SE watching.");
						return;
					}
				} finally {
					// RELEASE COLLECTIONS
					collections_lock_sem.Release();
				}
				// wait to next loop
				await Task.Delay(100).ConfigureAwait(false);
			}
		}
	}
}
