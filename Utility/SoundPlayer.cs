using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Nursery.Utility {
	public enum PlayState {
		Playing,
		Stopped,
		Paused,
	}
	
	public class SoundPlayer {
		public readonly string Name;
		public readonly string FilePath;
		private readonly WaveOutEvent outDevice;
		private readonly SampleChannel channel = null;
		private readonly AudioFileReader autoFile = null;

		public float Volume {
			get {
				if (this.channel != null) {
					return this.channel.Volume;
				}
				if (this.channel != null) {
					return this.autoFile.Volume;
				}
				return 0;
			}
			set {
				if (this.channel != null) {
					this.channel.Volume = value;
				}
				if (this.channel != null) {
					this.autoFile.Volume = value;
				}
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
			this.outDevice = new WaveOutEvent() {
				DeviceNumber = Utility.Audio.NAudio.Instance.WaveOutDeviceId,
			};
			stream.Position = 0;
			switch (Path.GetExtension(this.FilePath).ToLower()) {
				case ".wav":
					this.channel = new SampleChannel(new WaveFileReader(stream)) { Volume = volume };
					this.outDevice.Init(this.channel);
					break;
				case ".mp3":
					this.channel = new SampleChannel(new Mp3FileReader(stream)) { Volume = volume };
					this.outDevice.Init(this.channel);
					break;
				case ".aiff":
					this.channel = new SampleChannel(new AiffFileReader(stream)) { Volume = volume };
					this.outDevice.Init(this.channel);
					break;
				default:
					this.autoFile = new AudioFileReader(filepath){ Volume = volume };
					this.outDevice.Init(this.autoFile);
					break;
			}
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
