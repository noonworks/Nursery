using System;
using System.Collections.Generic;
using System.Linq;
using Un4seen.Bass;
using Nursery.Utility;

namespace Nursery {
	class BassWrapper {
		public static bool IsInitialized { get; set; }
		private static int SoundDeviceId = -1;
		private static int RecordDeviceId = -1;

		public static void Initialize(Options.MainConfig config) {
			if (BassWrapper.IsInitialized) { return; }
			BassWrapper.IsInitialized = false;
			// Bass.Net
			// get sound device
			// TRANSLATORS: Log message. In BassWrapper.
			Logger.Log(T._("- get sound device ..."));
			SoundDeviceId = GetDeviceId(config);
			if (SoundDeviceId < 0) { return; }
			// get record device
			// TRANSLATORS: Log message. In BassWrapper.
			Logger.Log(T._("- get record device ..."));
			RecordDeviceId = GetRecordDeviceId(config);
			if (RecordDeviceId < 0) { return; }
			// init sound device
			// TRANSLATORS: Log message. In BassWrapper.
			Logger.Log(T._("- initialize sound device ..."));
			if (!Bass.BASS_Init(SoundDeviceId, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero)) {
				// TRANSLATORS: Error message. In BassWrapper. {0} is error code.
				throw new Exception(T._("Failed to init sound device: {0}", Bass.BASS_ErrorGetCode()));
			}
			// init record device
			// TRANSLATORS: Log message. In BassWrapper.
			Logger.Log(T._("- initialize record device ..."));
			if (!Bass.BASS_RecordInit(RecordDeviceId)) {
				// TRANSLATORS: Error message. In BassWrapper. {0} is error code.
				throw new Exception(T._("Failed to init recording device: {0}", Bass.BASS_ErrorGetCode()));
			}
			BassWrapper.IsInitialized = true;
		}

		private static int GetDeviceFromName(BASS_DEVICEINFO[] devices, string devicename) {
			for (int i = 0; i < devices.Length; i++) {
				if (devices[i].name == devicename) {
					return i;
				}
			}
			return -1;
		}

		private static int GetRecordDeviceId(Options.MainConfig config) {
			var devices = Bass.BASS_RecordGetDeviceInfos();
			var id = GetDeviceFromName(devices, config.DeviceRead);
			if (id >= 0) { return id; }
			IEnumerable<string> devicesList = devices.Select(d => d.name);
			// TRANSLATORS: Error message. In BassWrapper. {0} is name of recording device. {1} is list of available recording device(s).
			throw new Exception(T._("Recording device \"{0}\" is not found.\nAvailable recording devices:\n{1}", config.DeviceRead, " * " + string.Join("\n * ", devicesList)));
		}

		private static int GetDeviceId(Options.MainConfig config) {
			var devices = Bass.BASS_GetDeviceInfos();
			var id = GetDeviceFromName(devices, config.DeviceSoundEffect);
			if (id >= 0) { return id; }
			IEnumerable<string> devicesList = devices.Select(d => d.name);
			// TRANSLATORS: Error message. In BassWrapper. {0} is name of sound device. {1} is list of available sound device(s).
			throw new Exception(T._("Sound device \"{0}\" is not found.\nAvailable sound devices:\n{1}", config.DeviceSoundEffect, " * " + string.Join("\n * ", devicesList)));
		}
		
		public static void Free() {
			if (!BassWrapper.IsInitialized) { return; }
			Bass.BASS_Stop();
			if (SoundDeviceId >= 0) {
				// TRANSLATORS: Log message. In BassWrapper.
				Logger.Log(T._("- free sound device ..."));
				Bass.BASS_SetDevice(SoundDeviceId);
				if (Bass.BASS_Free()) {
					// TRANSLATORS: Log message. In BassWrapper.
					Logger.Log(T._("  - success"));
				}
			}
			if (RecordDeviceId >= 0) {
				// TRANSLATORS: Log message. In BassWrapper.
				Logger.Log(T._("- free record device ..."));
				Bass.BASS_SetDevice(RecordDeviceId);
				if (Bass.BASS_RecordFree()) {
					// TRANSLATORS: Log message. In BassWrapper.
					Logger.Log(T._("  - success"));
				}
			}
			BassWrapper.IsInitialized = false;
		}

	}
}
