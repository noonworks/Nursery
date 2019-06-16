using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Nursery.Utility {
	public class BinaryPool<TFile, TData> where TFile : BinaryFileBase<TData>, new() {
		private List<KeyValuePair<string, TFile>> pool = new List<KeyValuePair<string, TFile>>();

		public long MemoryMax { get; set; }

		public void Clear() {
			this.pool = new List<KeyValuePair<string, TFile>>();
		}

		public void AddFiles(string[] filepaths) {
			List<string> notfound = new List<string>();
			foreach (var filepath in filepaths) {
				var contains = pool.Select(kv => kv.Key).Contains(filepath);
				if (contains) { continue; }
				var usage = pool.Select(kv => kv.Value.Size).Sum();
				var f = new TFile();
				f.SetFilePath(filepath);
				if (usage + f.Size < MemoryMax) { f.Load(); }
				this.pool.Add(new KeyValuePair<string, TFile>(filepath, f));
			}
			if (notfound.Count > 0) {
				// TRANSLATORS: Error message. In BinaryPool. {0} is name(s) of file.
				throw new Exception(T._("Binary file(s) are not found: {0}", String.Join(", ", notfound)));
			}
		}

		public TData GetData(string filename) {
			var idx = Array.IndexOf(pool.Select(kv => kv.Key).ToArray(), filename);
			if (idx < 0) { return default(TData); }
			// move to top
			var filekv = pool[idx];
			pool.RemoveAt(idx);
			pool.Insert(0, filekv);
			// load
			if (!filekv.Value.IsLoad) {
				long sum = 0;
				foreach (var kv in pool) {
					sum += kv.Value.Size;
					if (sum >= this.MemoryMax && kv.Value.IsLoad) { kv.Value.UnLoad(); }
				}
			}
			return filekv.Value.Load();
		}
	}

	public abstract class BinaryFileBase<TData> {
		public string FileName { get; private set; }
		public long Size { get; protected set; }
		public bool IsLoad { get; protected set; } = false;
		public TData Data { get; protected set; }

		public BinaryFileBase() {}

		public void SetFilePath(string filepath) {
			if (!File.Exists(filepath)) {
				filepath = Path.Combine(Directory.GetCurrentDirectory(), filepath);
			}
			if (!File.Exists(filepath)) {
				// TRANSLATORS: Error message. In BinaryPool. {0} is file path.
				throw new Exception(T._("Binary file [{0}] is not found.", filepath));
			}
			this.FileName = filepath;
			var fi = new FileInfo(this.FileName);
			this.Size = fi.Length;
		}
		
		abstract public TData Load();
		abstract public void UnLoad();
	}

	public class BinaryFile : BinaryFileBase<byte[]> {
		public BinaryFile() : base() {
			this.Data = new byte[0];
		}

		public override byte[] Load() {
			if (this.IsLoad) { return this.Data; }
			using (FileStream fs = new FileStream(this.FileName, FileMode.Open, FileAccess.Read)) {
				if (this.Size != fs.Length) { this.Size = fs.Length; }
				this.Data = new byte[fs.Length];
				fs.Read(this.Data, 0, this.Data.Length);
			}
			this.IsLoad = true;
			return this.Data;
		}

		public override void UnLoad() {
			this.Data = new byte[0];
			this.IsLoad = false;
		}
	}

	public class BinaryStream : BinaryFileBase<MemoryStream> {
		public BinaryStream() : base() {
			this.Data = new MemoryStream();
		}

		public override MemoryStream Load() {
			if (this.IsLoad) { return this.Data; }
			this.Data = new MemoryStream();
			using (FileStream fs = new FileStream(this.FileName, FileMode.Open, FileAccess.Read)) {
				if (this.Size != fs.Length) { this.Size = fs.Length; }
				fs.CopyTo(this.Data);
			}
			this.IsLoad = true;
			return this.Data;
		}

		public override void UnLoad() {
			this.Data = new MemoryStream();
			this.IsLoad = false;
		}
	}

	public class UnsafeBinaryFile : BinaryFileBase<SafeBinaryHandle> {
		public UnsafeBinaryFile() : base() {
			this.Data = default(SafeBinaryHandle);
		}

		public override SafeBinaryHandle Load() {
			if (this.IsLoad) { return this.Data; }
			byte[] bytes;
			using (FileStream fs = new FileStream(this.FileName, FileMode.Open, FileAccess.Read)) {
				if (this.Size != fs.Length) { this.Size = fs.Length; }
				bytes = new byte[fs.Length];
				fs.Read(bytes, 0, bytes.Length);
			}
			this.Data = SafeBinaryHandle.Create(bytes);
			this.IsLoad = true;
			return this.Data;
		}

		public override void UnLoad() {
			this.Data = default(SafeBinaryHandle);
			this.IsLoad = false;
		}
	}

	public class SafeBinaryHandle : SafeHandleZeroOrMinusOneIsInvalid {
		private bool released = false;

		public IntPtr RawIntPtr { get => handle; }
		public long Size { get; private set; }

		private SafeBinaryHandle() : base(true) { }

		public static SafeBinaryHandle Create(byte[] bytes) {
			var instance = new SafeBinaryHandle {
				Size = bytes.Length,
				// alloc unsafe memory
				handle = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(byte)) * bytes.Length)
			};
			// copy bytes to unsafe memory
			Marshal.Copy(bytes, 0, instance.handle, bytes.Length);
			return instance;
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		override protected bool ReleaseHandle() {
			if (!released) { Marshal.FreeCoTaskMem(handle); }
			released = true;
			return true;
		}
	}
}
