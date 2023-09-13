namespace Scripts
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Utils.ScriptPerformanceLogger;

	//---------------------------------
	// SLC-AS-ScriptPerformanceAnalyzer_1.cs
	//---------------------------------

	[GQIMetaData(Name = "Multi Script Performance Analyzer")]
	public class MyDataSource : IGQIDataSource, IGQIInputArguments, IGQIOnInit
	{ 
		private GQIDateTimeArgument _startArg = new GQIDateTimeArgument("Start") { IsRequired = true };
		private GQIDateTimeArgument _stopArg = new GQIDateTimeArgument("Stop") { IsRequired = true };
		private GQIStringArgument _methodRegexFilterArgs = new GQIStringArgument("Method Filter") { IsRequired = false };

		private string folderPath = @"C:\Skyline_Data\ScriptPerformanceLogger";
		private DateTime _start;
		private DateTime _stop;
		private string[] _methodRegexFilter;

		public GQIColumn[] GetColumns()
		{
			List<GQIColumn> columns = new List<GQIColumn>
			{
				new GQIStringColumn("Class Name"),
				new GQIStringColumn("Method Name"),
				new GQIDateTimeColumn("Start"),
				new GQIDateTimeColumn("End"),
				new GQIDoubleColumn("Execution Time"),
				new GQIIntColumn("Sub Method Level"),
			};

			return columns.ToArray();
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _startArg, _stopArg, _methodRegexFilterArgs };
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			var rows = new List<GQIRow>();

			try
			{
				List<Result> results = GetResults(_start, _stop);
				foreach (Result result in results)
				{
					foreach (var methodInvocation in result.MethodInvocations)
					{
						ProcessMethodInvocation(rows, methodInvocation, 0);
					}
				}
			}
			catch (Exception e)
			{
				throw e;
			}

			return new GQIPage(rows.ToArray())
			{
				HasNextPage = false,
			};
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			// Datetimes coming from the app are passed in UTC format
			_start = args.GetArgumentValue(_startArg);
			_stop = args.GetArgumentValue(_stopArg);

			var methods = args.GetArgumentValue(_methodRegexFilterArgs);
			_methodRegexFilter = GetValuesFromInputParameter(methods);

			return new OnArgumentsProcessedOutputArgs();
		}

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			return new OnInitOutputArgs();
		}

		private static string[] GetValuesFromInputParameter(string rawValue)
		{
			try
			{
				return JsonConvert.DeserializeObject<string[]>(rawValue);
			}
			catch (Exception)
			{
				return new string[0];
			}
		}

		private List<Result> GetResults(DateTime start, DateTime stop)
		{
			List<Result> results = new List<Result>();

			// Get all files in the folder
			string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly).Where(fileName => fileName.Contains(".json")).ToArray();

			// Iterate through each file and check its creation time
			foreach (string file in files)
			{
				FileInfo fileInfo = new FileInfo(file);
				DateTime creationTime = fileInfo.CreationTimeUtc;

				// Compare the file's creation time with the start time
				if (creationTime >= start && creationTime < stop)
				{
					// File was created within the last 24 hours
					string filecontent = File.ReadAllText(file);

					Result result = JsonConvert.DeserializeObject<Result>(filecontent);
					results.Add(result);
				}
			}

			return results;
		}

		private void ProcessMethodInvocation(List<GQIRow> rows, MethodInvocation methodInvocation, int level)
		{
			var cells = new List<GQICell>
			{
				new GQICell { Value = methodInvocation.ClassName },
				new GQICell { Value = methodInvocation.MethodName },
				new GQICell { Value = DateTime.SpecifyKind(methodInvocation.TimeStamp, DateTimeKind.Utc) },
				new GQICell { Value = DateTime.SpecifyKind(methodInvocation.TimeStamp + methodInvocation.ExecutionTime, DateTimeKind.Utc) },
				new GQICell { Value = methodInvocation.ExecutionTime.TotalMilliseconds, DisplayValue = methodInvocation.ExecutionTime.TotalMilliseconds + " ms" },
				new GQICell { Value = level },
			};

			if (_methodRegexFilter.Any() && _methodRegexFilter.Contains(methodInvocation.MethodName))
			{
				rows.Add(new GQIRow(cells.ToArray()));
			}

			foreach (var childInvocation in methodInvocation.ChildInvocations)
			{
				ProcessMethodInvocation(rows, childInvocation, level + 1);
			}
		}
	}
}

//---------------------------------
// MethodInvocation.cs
//---------------------------------
namespace Skyline.DataMiner.Utils.ScriptPerformanceLogger
{
	using System;
	using System.Collections.Generic;

	using Newtonsoft.Json;

	[Serializable]
	public class MethodInvocation
	{
		[JsonProperty(Order = 0)]
		public string ClassName { get; set; }

		[JsonProperty(Order = 1)]
		public string MethodName { get; set; }

		[JsonProperty(Order = 2)]
		public DateTime TimeStamp { get; set; }

		[JsonProperty(Order = 3)]
		public TimeSpan ExecutionTime { get; set; }

		[JsonProperty(Order = 4)]
		public List<MethodInvocation> ChildInvocations { get; set; } = new List<MethodInvocation>();
	}

	[Serializable]
	public class Result
	{
		public List<MethodInvocation> MethodInvocations { get; } = new List<MethodInvocation>();

		//public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
	}
}