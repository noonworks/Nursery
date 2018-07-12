using System;
using Un4seen.Bass;

namespace Nursery.Utility {
	public enum PlayState {
		Playing,
		Stopped,
		Paused,
	}

	/*
	 * CAUTION: SoundPlayer is NOT thread-safe.
	 */
	public class SoundPlayer : IDisposable {
		// read only members
		public string Name { get; }
		private SafeBinaryHandle _bin { get; }
		private long _length { get; }
		private double _seconds { get; }
		// modifiable members
		private int _handle = 0;
		private float _volume = 1.0f;
		private PlayState _state = PlayState.Stopped;

		public SoundPlayer(string name, SafeBinaryHandle sbh, float volume) {
			this.Name = name;
			this._bin = sbh;
			this._handle = Bass.BASS_StreamCreateFile(this._bin.RawIntPtr, 0, this._bin.Size, BASSFlag.BASS_DEFAULT);
			this._length = Bass.BASS_ChannelGetLength(this._handle);
			this.Volume = volume;
			this._seconds = Bass.BASS_ChannelBytes2Seconds(this._handle, this._length);
		}

		public PlayState State {
			get {
				if (this._state == PlayState.Playing) {
					if (this._length <= Bass.BASS_ChannelGetPosition(_handle)) {
						this.Stop();
					}
				}
				return this._state;
			}
		}

		public float Volume {
			get => this._volume;
			set {
				this._volume = value;
				Bass.BASS_ChannelSetAttribute(this._handle, BASSAttribute.BASS_ATTRIB_VOL, value);
			}
		}

		public double Seconds { get => this._seconds; }

		public double Time {
			get {
				var pos = Bass.BASS_ChannelGetPosition(this._handle);
				return Bass.BASS_ChannelBytes2Seconds(this._handle, pos);
			}
			set {
				var pos = Bass.BASS_ChannelSeconds2Bytes(this._handle, value);
				Bass.BASS_ChannelSetPosition(this._handle, pos);
			}
		}

		public bool Play(bool Restart = false) {
			if (!Bass.BASS_ChannelPlay(this._handle, Restart)) {
				// TRANSLATORS: Log message. In SoundPlayer. {0} is error code.
				Logger.Log(T._("Could not play sound. Error: {0}", Bass.BASS_ErrorGetCode()));
				return false;
			}
			this._state = PlayState.Playing;
			return false;
		}

		public bool Stop() {
			if (this._state == PlayState.Stopped) { return true; }
			if (!Bass.BASS_ChannelStop(this._handle)) {
				// TRANSLATORS: Log message. In SoundPlayer. {0} is error code.
				Logger.Log(T._("Could not stop sound. Error: {0}", Bass.BASS_ErrorGetCode()));
				return false;
			}
			Bass.BASS_ChannelSetPosition(this._handle, 0.0);
			this._state = PlayState.Stopped;
			return false;
		}

		public bool Pause() {
			if (this._state != PlayState.Playing) { return true; }
			if (!Bass.BASS_ChannelPause(this._handle)) {
				// TRANSLATORS: Log message. In SoundPlayer. {0} is error code.
				Logger.Log(T._("Could not pause sound. Error: {0}", Bass.BASS_ErrorGetCode()));
				return false;
			}
			this._state = PlayState.Paused;
			return true;
		}

		#region Dispose

		protected void Dispose(bool disposing) {
			if (this._handle != 0) {
				this.Stop();
				Bass.BASS_StreamFree(this._handle);
				this._handle = 0;
			}
			GC.SuppressFinalize(this);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~SoundPlayer() {
			Dispose(false);
		}

		#endregion
	}
}
