using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Utils.ScriptPerformanceLogger;

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
		public List<MethodInvocation> ChildInvocations { get; } = new List<MethodInvocation>();
	}
}

//---------------------------------
// Result.cs
//---------------------------------
namespace Skyline.DataMiner.Utils.ScriptPerformanceLogger
{
	using System;
	using System.Collections.Generic;

	[Serializable]
	public class Result
	{
		public List<MethodInvocation> MethodInvocations { get; } = new List<MethodInvocation>();

		public Dictionary<string, string> Properties { get; private set; } = new Dictionary<string, string>();
	}
}

//---------------------------------
// SLC-AS-ScriptPerformanceAnalyzer_1.cs
//---------------------------------

[GQIMetaData(Name = "Multi Script Performance Analyzer")]
public class MyDataSource : IGQIDataSource, IGQIInputArguments, IGQIOnInit
{
	private GQIDateTimeArgument _startArg = new GQIDateTimeArgument("Start") { IsRequired = true };
	private GQIDateTimeArgument _stopArg = new GQIDateTimeArgument("Stop") { IsRequired = true };

	private string folderPath = @"C:\Skyline_Data\ScriptPerformanceLogger";
	private DateTime _start;
	private DateTime _stop;

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
		return new GQIArgument[] { _startArg, _stopArg };
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
		catch (Exception)
		{
			// fail gracefully (if no valid path was provided)
		}

		return new GQIPage(rows.ToArray())
		{
			HasNextPage = false,
		};
	}

	public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
	{
		_start = args.GetArgumentValue(_startArg);
		_stop = args.GetArgumentValue(_stopArg);
		return new OnArgumentsProcessedOutputArgs();
	}

	public OnInitOutputArgs OnInit(OnInitInputArgs args)
	{
		return new OnInitOutputArgs();
	}

	private List<Result> GetResults(DateTime start, DateTime stop)
	{
		List<Result> results = new List<Result>();

		// Get all files in the folder
		string[] files = Directory.GetFiles(folderPath);

		// Iterate through each file and check its creation time
		foreach (string file in files)
		{
			FileInfo fileInfo = new FileInfo(file);
			DateTime creationTime = fileInfo.CreationTime;

			// Compare the file's creation time with the start time
			if (creationTime > start && creationTime < stop)
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
		List<GQICell> cells = new List<GQICell>();
		cells.Add(new GQICell() { Value = methodInvocation.ClassName });
		cells.Add(new GQICell() { Value = methodInvocation.MethodName });
		cells.Add(new GQICell() { Value = methodInvocation.TimeStamp });
		cells.Add(new GQICell() { Value = methodInvocation.TimeStamp + methodInvocation.ExecutionTime });
		cells.Add(new GQICell() { Value = methodInvocation.ExecutionTime.TotalSeconds, DisplayValue = methodInvocation.ExecutionTime.TotalSeconds + " s" });
		cells.Add(new GQICell() { Value = level });

		rows.Add(new GQIRow(cells.ToArray()));

		foreach (var childInvocation in methodInvocation.ChildInvocations)
		{
			ProcessMethodInvocation(rows, childInvocation, level + 1);
		}
	}
}