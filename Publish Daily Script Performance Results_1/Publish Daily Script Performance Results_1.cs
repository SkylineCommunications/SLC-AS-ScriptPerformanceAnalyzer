/*
****************************************************************************
*  Copyright (c) 2023,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

dd/mm/2023	1.0.0.1		XXX, Skyline	Initial version
****************************************************************************
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;

using QAPortalAPI.APIHelper;
using QAPortalAPI.Enums;
using QAPortalAPI.Models.ReportingModels;

using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Utils.ScriptPerformanceLogger;

using Result = Skyline.DataMiner.Utils.ScriptPerformanceLogger.Result;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
	private const string DirectoryPath = @"C:\Skyline_Data\ScriptPerformanceLogger";

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">Link with SLAutomation process.</param>
	public void Run(Engine engine)
	{
		DirectoryInfo directoryInfo = Directory.CreateDirectory(DirectoryPath);
		DateTime threshold = DateTime.UtcNow.AddDays(-1);

		QaPortalApiHelper qaPortalHelper = GetQaPortalApiHelper(engine);

		IEnumerable<FileInfo> dailyResults = directoryInfo.EnumerateFiles("*.json")
			.Where(info => info.CreationTimeUtc > threshold);

		IEnumerable<IGrouping<string, FileInfo>> resultsGroupedByTitle = dailyResults
			.Where(info => info.Name.Contains('_'))
			.GroupBy(GetTitle);

		var testReportsByTitle = new Dictionary<string, TestReport>();
		TestSystemInfo testSystemInfo = GetTestSystemInfo(engine);

		foreach (IGrouping<string, FileInfo> grouping in resultsGroupedByTitle)
		{
			string title = grouping.Key;
			if (!testReportsByTitle.TryGetValue(title, out TestReport report))
			{
				report = new TestReport(GetTestInfo(engine, title), testSystemInfo);
				testReportsByTitle.Add(title, report);
			}

			IEnumerable<IGrouping<string, MethodInvocation>> invocationsGroupedByFullName = GetInvocations(grouping)
				.GroupBy(invocation => $"{invocation.ClassName}.{invocation.MethodName}");

			foreach (IGrouping<string, MethodInvocation> invocations in invocationsGroupedByFullName)
			{
				string fullName = invocations.Key;

				double[] executionTimes = invocations
					.Select(invocation => invocation.ExecutionTime.TotalMilliseconds)
					.ToArray();

				var avgCase = new TestCaseReport($"{fullName} (avg)", QAPortalAPI.Enums.Result.Success, String.Empty);
				report.TryAddTestCase(avgCase);
				report.PerformanceTestCases.Add(
					new PerformanceTestCaseReport(
						avgCase.TestCaseName,
						avgCase.TestCaseResult,
						avgCase.TestCaseResultInfo,
						ResultUnit.Millisecond,
						executionTimes.Average()));

				var minCase = new TestCaseReport($"{fullName} (min)", QAPortalAPI.Enums.Result.Success, String.Empty);
				report.TryAddTestCase(minCase);
				report.PerformanceTestCases.Add(
					new PerformanceTestCaseReport(
						minCase.TestCaseName,
						minCase.TestCaseResult,
						minCase.TestCaseResultInfo,
						ResultUnit.Millisecond,
						executionTimes.Min()));

				var maxCase = new TestCaseReport($"{fullName} (max)", QAPortalAPI.Enums.Result.Success, String.Empty);
				report.TryAddTestCase(maxCase);
				report.PerformanceTestCases.Add(
					new PerformanceTestCaseReport(
						maxCase.TestCaseName,
						maxCase.TestCaseResult,
						maxCase.TestCaseResultInfo,
						ResultUnit.Millisecond,
						executionTimes.Max()));
			}
		}

		foreach (TestReport testReport in testReportsByTitle.Values)
		{
			qaPortalHelper.PostResult(testReport);
		}
	}

	private static string GetTitle(FileInfo info)
	{
		return Path.GetFileNameWithoutExtension(info.Name.Substring(info.Name.IndexOf('_') + 1));
	}

	private static IEnumerable<MethodInvocation> GetInvocations(IEnumerable<FileInfo> files)
	{
		var jsonSerializer = new JsonSerializer();
		foreach (FileInfo file in files)
		{
			Result result;
			try
			{
				result = jsonSerializer.Deserialize<Result>(new JsonTextReader(file.OpenText()));
			}
			catch (SystemException)
			{
				continue;
			}
			catch (JsonException)
			{
				continue;
			}

			foreach (MethodInvocation invocation in result.GetInvocationsRecursive())
			{
				yield return invocation;
			}
		}
	}

	private static QaPortalApiHelper GetQaPortalApiHelper(IEngine engine)
	{
		string portalLink = engine.GetScriptParam(2).Value;
		string clientId = engine.GetScriptParam(3).Value;
		string apiKey = engine.GetScriptParam(4).Value;

		if (portalLink == "." && clientId == "." && apiKey == ".")
		{
			return new QaPortalApiHelper(
				engine.GenerateInformation,
				"https://qaportal.skyline.local/api/public/results/addresult",
				String.Empty,
				String.Empty);
		}

		return portalLink.Contains("@")
			? new QaPortalApiHelper(engine.GenerateInformation, portalLink, clientId, apiKey, engine.SendEmail)
			: new QaPortalApiHelper(engine.GenerateInformation, portalLink, clientId, apiKey);
	}

	private static TestSystemInfo GetTestSystemInfo(IEngine engine)
	{
		string agentName = engine.GetScriptParam(6).Value;
		return new TestSystemInfo(agentName);
	}

	private static TestInfo GetTestInfo(IEngine engine, string title)
	{
		string domain = engine.GetScriptParam(5).Value;
		string projectId = engine.GetScriptParam(7).Value;

		List<int> projectIds = projectId.Split(new[] { ';', '|', ',' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(Int32.Parse)
			.ToList();

		return new TestInfo(
			$"Script Performance {title}",
			domain,
			projectIds,
			$"These are the performance results of '{title}' captured by the ScriptPerformanceLogger tool.");
	}
}