using Discord.Audio;
using Discord.WebSocket;
using Nursery.Utility;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Un4seen.Bass;

namespace Nursery {
	class VoiceChat : IDisposable {
		private const int SAMPLE_RATE = 48000;
		private const int CHANNEL_COUNT = 2;

		private string deviceName = "";
		private IAudioClient audioClient = null;
		private AudioOutStream stream;
		private int recordHandle { get; } = 0;
		private RECORDPROC recordProc;

		private SemaphoreSlim st_lock_sem = new SemaphoreSlim(1, 1);

		public VoiceChat(Options.MainConfig config) {
			this.deviceName = config.DeviceRead;
			this.recordProc = new RECORDPROC(recordDevice_audioReceived);
			this.recordHandle = Bass.BASS_RecordStart(SAMPLE_RATE, CHANNEL_COUNT, BASSFlag.BASS_RECORD_PAUSE, this.recordProc, IntPtr.Zero);
		}

		protected virtual void Dispose(bool disposing) {
			try {
				Disconnect().Wait();
				Bass.BASS_StreamFree(this.recordHandle);
			} catch (Exception e) {
				// TRANSLATORS: Log message. In VoiceChat.
				Logger.Log(T._("Could not disconnect from voice channel."));
				Logger.DebugLog(e.ToString());
			}
			GC.SuppressFinalize(this);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~VoiceChat() {
			Dispose(false);
		}

		private bool recordDevice_audioReceived(int handle, IntPtr buffer, int length, IntPtr user) {
			st_lock_sem.Wait(); // LOCK STREAMS
			try {
				if (this.stream == null) { return true; }
				// send audio to discord voice
				using (var st = OpenBuffer(buffer, length, FileAccess.Read)) {
					st.CopyTo(this.stream);
				}
				return true;
			} catch (OperationCanceledException) {
				// TRANSLATORS: Log message. In VoiceChat.
				Logger.Log(T._("(Audio recording canceled.)"));
				return false;
			} catch (Exception ex) {
				// TRANSLATORS: Log message. In VoiceChat.
				Logger.Log(T._("Error in audio recording."));
				Logger.DebugLog(ex.ToString());
				return false;
			} finally {
				st_lock_sem.Release(); // RELEASE STREAMS
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe UnmanagedMemoryStream OpenBuffer(IntPtr buffer, int length, FileAccess access) {
			return new UnmanagedMemoryStream((byte*)buffer, length, length, access);
		}

		public async Task Connect(SocketVoiceChannel voiceChannel) {
			if (this.audioClient != null) { await this.Disconnect(); }
			// TRANSLATORS: Log message. In VoiceChat.
			Logger.Log(T._("* Connect to voice channel"));
			try {
				// LOCK STREAMS
				await st_lock_sem.WaitAsync().ConfigureAwait(false);
				// TRANSLATORS: Log message. In VoiceChat.
				Logger.Log(T._("- join voice channel ..."));
				this.audioClient = await voiceChannel.ConnectAsync();
				// TRANSLATORS: Log message. In VoiceChat.
				Logger.Log(T._("- create stream ..."));
				this.stream = this.audioClient.CreatePCMStream(AudioApplication.Voice, voiceChannel.Bitrate, 1000);
				// TRANSLATORS: Log message. In VoiceChat.
				Logger.Log(T._("- start recording ..."));
				if (Bass.BASS_ChannelIsActive(this.recordHandle) != BASSActive.BASS_ACTIVE_PLAYING) {
					if (!Bass.BASS_ChannelPlay(this.recordHandle, false)) {
						// TRANSLATORS: Log message. In VoiceChat. {0} is error code.
						Logger.Log(T._("Could not start recording. Error code: {0}", Bass.BASS_ErrorGetCode()));
						return;
					}
				} else {
					// TRANSLATORS: Log message. In VoiceChat.
					Logger.Log(T._("- already in recording."));
				}
				// TRANSLATORS: Log message. In VoiceChat.
				Logger.Log(T._("... Done!"));
			} catch (Exception e) {
				Logger.DebugLog(e.ToString());
				// TRANSLATORS: Error message. In VoiceChat.
				throw new Exception(T._("Could not connect to voice channel."), e);
			} finally {
				st_lock_sem.Release(); // RELEASE STREAMS
			}
		}

		public async Task Disconnect() {
			// TRANSLATORS: Log message. In VoiceChat.
			Logger.Log(T._("* Disconnect from voice channel"));
			try {
				await st_lock_sem.WaitAsync().ConfigureAwait(false); // LOCK STREAMS
				// disconnect audio stream
				if (this.stream != null) {
					// TRANSLATORS: Log message. In VoiceChat.
					Logger.Log(T._("- close stream ..."));
					this.stream.Close();
					this.stream = null;
				} else {
					// TRANSLATORS: Log message. In VoiceChat.
					Logger.Log(T._("- stream is closed."));
				}
				// leave voice chat
				if (this.audioClient != null) {
					// TRANSLATORS: Log message. In VoiceChat.
					Logger.Log(T._("- leave voice channel ..."));
					await this.audioClient.StopAsync();
					this.audioClient.Dispose();
					this.audioClient = null;
				} else {
					// TRANSLATORS: Log message. In VoiceChat.
					Logger.Log(T._("- not in voice channel."));
				}
				// stop recording
				if (Bass.BASS_ChannelIsActive(recordHandle) != BASSActive.BASS_ACTIVE_STOPPED && Bass.BASS_ChannelIsActive(recordHandle) != BASSActive.BASS_ACTIVE_PAUSED) {
					// TRANSLATORS: Log message. In VoiceChat.
					Logger.Log(T._("- stop recording ..."));
					if (!Bass.BASS_ChannelPause(this.recordHandle)) {
						// TRANSLATORS: Log message. In VoiceChat. {0} is error code.
						Logger.Log(T._("Could not stop recording. ErrorCode: {0}", Bass.BASS_ErrorGetCode()));
					}
				} else {
					// TRANSLATORS: Log message. In VoiceChat.
					Logger.Log(T._("- not in recording."));
				}
				// TRANSLATORS: Log message. In VoiceChat.
				Logger.Log(T._("... Done!"));
			} catch (Exception e) {
				Logger.DebugLog(e.ToString());
				// TRANSLATORS: Error message. In VoiceChat.
				throw new Exception(T._("Could not disconnect from voice channel."), e);
			} finally {
				st_lock_sem.Release(); // RELEASE STREAMS
			}
		}
	}
}
