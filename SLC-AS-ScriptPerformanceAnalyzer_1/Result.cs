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
