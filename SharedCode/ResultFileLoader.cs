namespace Skyline.DataMiner.Utils.ScriptPerformanceLogger
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Newtonsoft.Json;

	public static class ResultFileLoader
	{
		public static IEnumerable<Result> LoadAllFiles()
		{
			var directory = new DirectoryInfo(Paths.ScriptPerformanceLoggerPath);
			var files = directory.EnumerateFiles("*.json", SearchOption.TopDirectoryOnly);

			return LoadFiles(files);
		}

		public static IEnumerable<Result> LoadFiles(DateTime start, DateTime stop)
		{
			var directory = new DirectoryInfo(Paths.ScriptPerformanceLoggerPath);
			var files = directory.EnumerateFiles("*.json", SearchOption.TopDirectoryOnly)
				.Where(f => f.CreationTime >= start && f.CreationTime <= stop);

			return LoadFiles(files);
		}

		public static IEnumerable<Result> LoadFiles(IEnumerable<FileInfo> files)
		{
			// load files in parallel
			return files
				.AsParallel()
				.Select(x => LoadFile(x.FullName));
		}

		public static Result LoadFile(string path)
		{
			var content = File.ReadAllText(path);
			var result = JsonConvert.DeserializeObject<Result>(content);

			return result;
		}
	}
}