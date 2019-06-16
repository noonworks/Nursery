using System;
using System.IO;
using NAudio.Wave;

namespace Nursery.Utility {
	public enum PlayState {
		Playing,
		Stopped,
		Paused,
	}
	
	public class SoundPlayer {
		public readonly string Name;
		public readonly string FilePath;
		private readonly WaveStream audioFile;
		private readonly WaveOutEvent outDevice;

		public float Volume {
			get => this.outDevice.Volume;
			set {
				this.outDevice.Volume = value;
			}
		}

		public PlayState State {
			get {
				switch (this.outDevice.PlaybackState) {
					case PlaybackState.Paused:
						return PlayState.Paused;
					case PlaybackState.Playing:
						return PlayState.Playing;
					case PlaybackState.Stopped:
					default:
						return PlayState.Stopped;
				}
			}
		}

		public SoundPlayer(string identifer, string filepath, MemoryStream stream, float volume) {
			this.Name = identifer;
			this.FilePath = filepath;
			var ext = Path.GetExtension(this.FilePath).ToLower();
			stream.Position = 0;
			switch (ext) {
				case ".wav":
					this.audioFile = new WaveFileReader(stream);
					break;
				case ".mp3":
					this.audioFile = new Mp3FileReader(stream);
					break;
				case ".aiff":
					this.audioFile = new AiffFileReader(stream);
					break;
				default:
					this.audioFile = null;
					break;
			}
			this.outDevice = new WaveOutEvent() {
				DeviceNumber = Utility.Audio.NAudio.Instance.WaveOutDeviceId,
				Volume = volume,
			};
			this.outDevice.Init(this.audioFile);
		}

		public bool Play(bool Restart = false) {
			this.outDevice.Play();
			return false;
		}

		public bool Stop() {
			this.outDevice.Stop();
			return false;
		}

		public bool Pause() {
			this.outDevice.Pause();
			return true;
		}
	}
}
