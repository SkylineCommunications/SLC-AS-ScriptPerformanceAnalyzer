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
using System.IO.Compression;
using System.Linq;
using System.Security;

using Skyline.DataMiner.Automation;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
	private const string ArchivePath = @"C:\Skyline_Data\ScriptPerformanceLogger\Archive.zip";

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">Link with SLAutomation process.</param>
	public void Run(Engine engine)
	{
		try
		{
			TryRun(engine);
		}
		catch (ScriptAbortException)
		{
			throw;
		}
		catch (ScriptForceAbortException)
		{
			throw;
		}
		catch (ScriptTimeoutException)
		{
			throw;
		}
		catch (Exception e)
		{
			engine.ExitFail(e.Message);
		}
	}

	private static void TryRun(IEngine engine)
	{
		var days = Convert.ToUInt32(engine.GetScriptParam(2).Value);
		int maxSize = Convert.ToInt32(engine.GetScriptParam(3).Value) * 1_000_000;

		DateTime threshold = DateTime.UtcNow.AddDays(-days);

		DirectoryInfo directoryInfo = Directory.CreateDirectory(Path.GetDirectoryName(ArchivePath));

		if (ShouldRollOver(maxSize))
		{
			RollOver();
		}

		var exceptions = new List<string>();
		using (ZipArchive archive = ZipFile.Open(ArchivePath, ZipArchiveMode.Update))
		{
			IEnumerable<FileInfo> filesToArchive = directoryInfo.EnumerateFiles("*.json")
				.Where(info => info.CreationTimeUtc.Date < threshold);

			foreach (FileInfo fileInfo in filesToArchive)
			{
				try
				{
					archive.CreateEntryFromFile(fileInfo.FullName, fileInfo.Name, CompressionLevel.Optimal);
					fileInfo.Delete();
				}
				catch (IOException e)
				{
					exceptions.Add($"{fileInfo.Name}: {e.Message}");
				}
				catch (UnauthorizedAccessException e)
				{
					exceptions.Add($"{fileInfo.Name}: {e.Message}");
				}
				catch (SecurityException e)
				{
					exceptions.Add($"{fileInfo.Name}: {e.Message}");
				}
			}
		}

		if (exceptions.Any())
		{
			engine.ExitFail($"Could not archive the following files:\n{String.Join("\n", exceptions)}");
		}
	}

	private static void RollOver()
	{
		string destFileName = Path.ChangeExtension(ArchivePath, "bak.zip");
		File.Delete(destFileName);
		File.Move(ArchivePath, destFileName);
	}

	private static bool ShouldRollOver(int maxSize)
	{
		if (maxSize <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(maxSize));
		}

		if (!File.Exists(ArchivePath))
		{
			return false;
		}

		return new FileInfo(ArchivePath).Length > maxSize;
	}
}