using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.Audio;
using Discord.WebSocket;
using NAudio.Wave;
using Nursery.Utility;

namespace Nursery.AudioConnector {
	class NAudio : IDisposable {
		private const int SAMPLE_RATE = 48000;

		public static bool IsInitialized { get; private set; } = false;

		public static void Initialize(Options.MainConfig config) {
			if (IsInitialized) { return; }
			// get sound device and record device
			// TRANSLATORS: Log message. In AudioManager.
			Logger.Log(T._("- get sound device ..."));
			// TRANSLATORS: Log message. In AudioManager.
			Logger.Log(T._("- get record device ..."));
			Utility.Audio.NAudio.Initialize(config.DeviceSoundEffect, config.DeviceRead);
			if (Utility.Audio.NAudio.Instance.WaveOutDeviceId < 0) {
				// TRANSLATORS: Error message. In AudioManager. {0} is name of sound device. {1} is list of available sound device(s).
				throw new Exception(T._("Sound device \"{0}\" is not found.\nAvailable sound devices:\n{1}", config.DeviceSoundEffect, " * " + string.Join("\n * ", Utility.Audio.NAudio.Instance.WaveOutDevices)));
			}
			if (Utility.Audio.NAudio.Instance.WaveInDeviceId < 0) {
				// TRANSLATORS: Error message. In AudioManager. {0} is name of recording device. {1} is list of available recording device(s).
				throw new Exception(T._("Recording device \"{0}\" is not found.\nAvailable recording devices:\n{1}", config.DeviceRead, " * " + string.Join("\n * ", Utility.Audio.NAudio.Instance.WaveInDevices)));
			}
			IsInitialized = true;
		}
		
		private readonly WaveInEvent input;
		private IAudioClient audioClient = null;
		private AudioOutStream stream;
		private readonly SemaphoreSlim st_lock_sem = new SemaphoreSlim(1, 1);
		private int bitrate = 0;

		public NAudio(Options.MainConfig config) {
			this.input = new WaveInEvent() { DeviceNumber = Utility.Audio.NAudio.Instance.WaveInDeviceId };
			this.input.DataAvailable += new EventHandler<WaveInEventArgs>(InputDataAvailable);
			this.input.WaveFormat = new WaveFormat(SAMPLE_RATE, WaveIn.GetCapabilities(Utility.Audio.NAudio.Instance.WaveInDeviceId).Channels);
		}

		void InputDataAvailable(object sender, WaveInEventArgs e) {
			st_lock_sem.Wait(); // LOCK STREAMS
			try {
				if (this.stream == null) { return; }
				if (e.BytesRecorded == 0) { return; }
				if (!this.stream.CanWrite) { return; }
				// send audio to discord voice
				this.stream.Write(e.Buffer, 0, e.BytesRecorded);
			} catch (Exception ex) {
				// TRANSLATORS: Log message. In AudioManager.
				Logger.Log(T._("Error in audio recording."));
				Logger.DebugLog(ex.ToString());
				this.ReOpenRecordStream();
			} finally {
				st_lock_sem.Release(); // RELEASE STREAMS
			}
		}

		#region Dispose

		protected virtual void Dispose(bool disposing) {
			try {
				Disconnect().Wait();
			} catch (Exception e) {
				// TRANSLATORS: Log message. In AudioManager.
				Logger.Log(T._("Could not disconnect from voice channel."));
				Logger.DebugLog(e.ToString());
			}
			GC.SuppressFinalize(this);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~NAudio() {
			Dispose(false);
		}
		#endregion

		private void ReOpenRecordStream() {
			// stop recording and close audio stream
			this.CloseRecordStream();
			// check connection state
			if (this.audioClient.ConnectionState == Discord.ConnectionState.Connected) {
				// create audio stream and start recording
				this.CreateRecordStream();
			} else {
				// TRANSLATORS: Log message. In AudioManager.
				Logger.Log(T._("==========\n==========\nDisconnected from Discord!\nPlease try to \"bye\" and \"come\" to reconnect.\n==========\n=========="));
			}
		}

		private void CloseRecordStream() {
			// stop recording
			// TRANSLATORS: Log message. In AudioManager.
			Logger.Log(T._("- stop recording ..."));
			this.input.StopRecording();
			// disconnect audio stream
			if (this.stream != null) {
				// TRANSLATORS: Log message. In AudioManager.
				Logger.Log(T._("- close stream ..."));
				this.stream.Close();
				this.stream = null;
			}
			// TRANSLATORS: Log message. In AudioManager.
			Logger.Log(T._("- stream is closed."));
		}

		private void CreateRecordStream() {
			if (this.stream == null) {
				// TRANSLATORS: Log message. In AudioManager.
				Logger.Log(T._("- create stream ..."));
				this.stream = this.audioClient.CreatePCMStream(AudioApplication.Voice, this.bitrate);
			}
			// TRANSLATORS: Log message. In AudioManager.
			Logger.Log(T._("- start recording ..."));
			this.input.StartRecording();
		}

		public async Task Connect(SocketVoiceChannel voiceChannel) {
			if (this.audioClient != null) { await this.Disconnect(); }
			// TRANSLATORS: Log message. In AudioManager.
			Logger.Log(T._("* Connect to voice channel"));
			try {
				// LOCK STREAMS
				await st_lock_sem.WaitAsync().ConfigureAwait(false);
				// stop recording and close audio stream
				this.CloseRecordStream();
				// TRANSLATORS: Log message. In AudioManager.
				Logger.Log(T._("- join voice channel ..."));
				this.audioClient = await voiceChannel.ConnectAsync();
				this.bitrate = voiceChannel.Bitrate;
				// create audio stream and start recording
				this.CreateRecordStream();
				// TRANSLATORS: Log message. In AudioManager.
				Logger.Log(T._("... Done!"));
			} catch (Exception e) {
				Logger.DebugLog(e.ToString());
				// TRANSLATORS: Error message. In AudioManager.
				throw new Exception(T._("Could not connect to voice channel."), e);
			} finally {
				st_lock_sem.Release(); // RELEASE STREAMS
			}
		}

		public async Task Disconnect() {
			// TRANSLATORS: Log message. In AudioManager.
			Logger.Log(T._("* Disconnect from voice channel"));
			try {
				await st_lock_sem.WaitAsync().ConfigureAwait(false); // LOCK STREAMS
				// stop recording and close audio stream
				this.CloseRecordStream();
				// leave voice chat
				if (this.audioClient != null) {
					// TRANSLATORS: Log message. In AudioManager.
					Logger.Log(T._("- leave voice channel ..."));
					await this.audioClient.StopAsync();
					this.audioClient.Dispose();
					this.audioClient = null;
				} else {
					// TRANSLATORS: Log message. In AudioManager.
					Logger.Log(T._("- not in voice channel."));
				}
				// TRANSLATORS: Log message. In AudioManager.
				Logger.Log(T._("... Done!"));
			} catch (Exception e) {
				Logger.DebugLog(e.ToString());
				// TRANSLATORS: Error message. In AudioManager.
				throw new Exception(T._("Could not disconnect from voice channel."), e);
			} finally {
				st_lock_sem.Release(); // RELEASE STREAMS
			}
		}
	}
}
