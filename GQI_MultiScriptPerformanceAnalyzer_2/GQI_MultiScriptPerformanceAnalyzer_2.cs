namespace Scripts
{
	using System;
	using System.Collections.Generic;
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

		private DateTime _start;
		private DateTime _stop;
		private string[] _methodRegexFilter;

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
				new GQIStringColumn("Class Name"),
				new GQIStringColumn("Method Name"),
				new GQIDateTimeColumn("Start"),
				new GQIDateTimeColumn("End"),
				new GQIDoubleColumn("Execution Time"),
				new GQIIntColumn("Sub Method Level"),
			};
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _startArg, _stopArg, _methodRegexFilterArgs };
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			var rows = new List<GQIRow>();

			var methodInvocations = ResultFileLoader.LoadFiles(_start, _stop)
				.SelectMany(r => r.MethodInvocations);

			foreach (var methodInvocation in methodInvocations)
			{
				ProcessMethodInvocation(rows, methodInvocation, 0);
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

		private void ProcessMethodInvocation(List<GQIRow> rows, MethodInvocation methodInvocation, int level)
		{
			if (_methodRegexFilter.Any() && _methodRegexFilter.Contains(methodInvocation.MethodName))
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

				rows.Add(new GQIRow(cells.ToArray()));
			}

			foreach (var childInvocation in methodInvocation.ChildInvocations)
			{
				ProcessMethodInvocation(rows, childInvocation, level + 1);
			}
		}
	}
}
