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
