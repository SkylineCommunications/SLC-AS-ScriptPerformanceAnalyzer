using System;
using System.Collections.Generic;

using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Utils.ScriptPerformanceLogger;

[GQIMetaData(Name = "Script Performance Analyzer")]
public class MyDataSource : IGQIDataSource, IGQIInputArguments, IGQIOnInit
{
	private GQIStringArgument _filePath = new GQIStringArgument("File Path") { IsRequired = true };

	private string filePath;

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
		return new GQIArgument[] { _filePath };
	}

	public GQIPage GetNextPage(GetNextPageInputArgs args)
	{
		var rows = new List<GQIRow>();

		try
		{
			var result = ResultFileLoader.LoadFile(filePath);

			foreach (var methodInvocation in result.MethodInvocations)
			{
				ProcessMethodInvocation(rows, methodInvocation, 0);
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
		filePath = args.GetArgumentValue(_filePath);
		return new OnArgumentsProcessedOutputArgs();
	}

	public OnInitOutputArgs OnInit(OnInitInputArgs args)
	{
		return new OnInitOutputArgs();
	}

	private void ProcessMethodInvocation(List<GQIRow> rows, MethodInvocation methodInvocation, int level)
	{
		var cells = new List<GQICell>
		{
			new GQICell { Value = methodInvocation.ClassName },
			new GQICell { Value = methodInvocation.MethodName },
			new GQICell { Value = methodInvocation.TimeStamp },
			new GQICell { Value = methodInvocation.TimeStamp + methodInvocation.ExecutionTime },
			new GQICell { Value = methodInvocation.ExecutionTime.TotalSeconds, DisplayValue = methodInvocation.ExecutionTime.TotalSeconds + " s" },
			new GQICell { Value = level },
		};

		rows.Add(new GQIRow(cells.ToArray()));

		foreach (var childInvocation in methodInvocation.ChildInvocations)
		{
			ProcessMethodInvocation(rows, childInvocation, level + 1);
		}
	}
}