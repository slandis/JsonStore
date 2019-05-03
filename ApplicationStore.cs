using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ApplicationStore<T> : IDisposable {
	private FileStream _stream;
	private List<T> _list;
	
	public ApplicationStore(string fileName) {
		if (!Directory.Exists(Path.GetDirectoryName(fileName)))
			throw new DirectoryNotFoundException();
		
		_stream = WaitForExclusiveAccess(fileName, 2);
		
		if (!_stream.CanRead || !_stream.CanWrite || !_stream.CanSeek || _stream == null)
			throw new IOException();
		
		_list = JsonConvert.DeserializeObject<List<T>>(new StreamReader(_stream).ReadToEnd()) as List<T>;
	}
	
	public void GetCollection(out List<T> list) {
		list = _list;
	}
	
	private static FileStream WaitForExclusiveAccess(string fileName, int timeout) {
		const int retryPause = 100;

			var startTime = DateTime.Now.Ticks;
			do {
				try {
					FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
					
					return fs;
				} catch (IOException) {
					Thread.Sleep(retryPause);
				}
			} while (new TimeSpan(DateTime.Now.Ticks - startTime).TotalSeconds < timeout);

			return null;
	}
	
	#region IDisposable Implementation
	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}
	
	protected virtual void Dispose(bool disposing) {
		if (!disposing) {
			return;
		}
		
		if (_stream != null) {	
			string data = JsonConvert.SerializeObject(_list);
			_stream.SetLength(0);
			_stream.Position = 0;
			_stream.Flush();
			_stream.Write(Encoding.ASCII.GetBytes(data), 0, data.Length);
			_stream.Flush();
			_stream.Close();
			_stream.Dispose();
			_stream = null;
		}
	}
	#endregion
}