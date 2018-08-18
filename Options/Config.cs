using Newtonsoft.Json;

namespace Nursery.Options {
	public class PathHolderConfig {
		[JsonIgnore]
		public string ConfigFilePath { get; set; } = "";
		[JsonIgnore]
		public string ConfigFileDir { get; set; } = "";
		[JsonIgnore]
		public string ConfigFileName { get; set; } = "";

		public void SetPath(string path) {
			this.ConfigFilePath = path;
			this.ConfigFileDir = System.IO.Path.GetDirectoryName(path);
			this.ConfigFileName = System.IO.Path.GetFileName(path);
		}
	}
}
