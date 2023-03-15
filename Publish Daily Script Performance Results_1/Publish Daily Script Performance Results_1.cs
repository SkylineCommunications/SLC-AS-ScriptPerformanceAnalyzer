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
	private const string Path = @"C:\Skyline_Data\ScriptPerformanceLogger";

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">Link with SLAutomation process.</param>
	public void Run(Engine engine)
	{
		DirectoryInfo directoryInfo = Directory.CreateDirectory(Path);
		DateTime threshold = DateTime.UtcNow.AddDays(-1);

		QaPortalApiHelper qaPortalHelper = GetQaPortalApiHelper(engine);

		IEnumerable<FileInfo> dailyResults = directoryInfo.EnumerateFiles("*.json")
			.Where(info => info.CreationTimeUtc > threshold);

		var testReportsByTitle = new Dictionary<string, TestReport>();

		TestSystemInfo testSystemInfo = GetTestSystemInfo(engine);
		foreach (FileInfo fileInfo in dailyResults)
		{
			string title = fileInfo.Name.Split('_')[1];
			if (!testReportsByTitle.TryGetValue(title, out TestReport report))
			{
				report = new TestReport(GetTestInfo(engine, title), testSystemInfo);
				testReportsByTitle.Add(title, report);
			}

			try
			{
				var result = new JsonSerializer().Deserialize<Result>(new JsonTextReader(fileInfo.OpenText()));
				foreach (MethodInvocation invocation in result.GetInvocationsRecursive())
				{
					var fullName = $"{invocation.ClassName}.{invocation.MethodName}";

					report.TryAddTestCase(new TestCaseReport(fullName, QAPortalAPI.Enums.Result.Unknown, String.Empty));

					var performanceReport = new PerformanceTestCaseReport(
						fullName,
						QAPortalAPI.Enums.Result.Unknown,
						String.Empty,
						ResultUnit.Millisecond,
						invocation.ExecutionTime.TotalMilliseconds)
					{
						Date = invocation.TimeStamp,
					};

					report.PerformanceTestCases.Add(performanceReport);
				}
			}
			catch (SystemException)
			{
			}
			catch (JsonException)
			{
			}
		}

		foreach (TestReport testReport in testReportsByTitle.Values)
		{
			qaPortalHelper.PostResult(testReport);
		}
	}

	private static QaPortalApiHelper GetQaPortalApiHelper(IEngine engine)
	{
		string portalLink = engine.GetScriptParam(2).Value;
		string clientId = engine.GetScriptParam(3).Value;
		string apiKey = engine.GetScriptParam(4).Value;

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