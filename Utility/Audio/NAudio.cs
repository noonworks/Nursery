using NAudio.Wave;

namespace Nursery.Utility.Audio {
	public class NAudio {
		public static NAudio Instance { get; private set; } = null;

		public static void Initialize(string outputDeviceName, string inputDeviceName) {
			if (Instance == null) {
				Instance = new NAudio(outputDeviceName, inputDeviceName);
			}
		}

		public int WaveOutDeviceId { get; private set; } = -1;
		public int WaveInDeviceId { get; private set; } = -1;
		public string[] WaveOutDevices { get; private set; }
		public string[] WaveInDevices { get; private set; }

		private NAudio(string outputDeviceName, string inputDeviceName) {
			GetWaveOutDeviceId(outputDeviceName);
			GetWaveInDeviceId(inputDeviceName);
		}

		private void GetWaveOutDeviceId(string deviceName) {
			this.WaveOutDevices = new string[WaveOut.DeviceCount];
			for (int i = 0; i < WaveOut.DeviceCount; i++) {
				var caps = WaveOut.GetCapabilities(i);
				this.WaveOutDevices[i] = caps.ProductName;
				if (caps.ProductName.StartsWith(deviceName) || deviceName.StartsWith(caps.ProductName)) {
					this.WaveOutDeviceId = i;
				}
			}
		}

		private void GetWaveInDeviceId(string deviceName) {
			this.WaveInDevices = new string[WaveIn.DeviceCount];
			for (int i = 0; i < WaveIn.DeviceCount; i++) {
				var caps = WaveIn.GetCapabilities(i);
				this.WaveInDevices[i] = caps.ProductName;
				if (caps.ProductName.StartsWith(deviceName) || deviceName.StartsWith(caps.ProductName)) {
					this.WaveInDeviceId = i;
				}
			}
		}
	}
}
