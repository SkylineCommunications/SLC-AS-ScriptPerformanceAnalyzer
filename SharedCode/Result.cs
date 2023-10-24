namespace Skyline.DataMiner.Utils.ScriptPerformanceLogger
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	[Serializable]
	public class Result
	{
		public List<MethodInvocation> MethodInvocations { get; } = new List<MethodInvocation>();

		public Dictionary<string, string> Properties { get; private set; } = new Dictionary<string, string>();

		public IEnumerable<MethodInvocation> GetInvocationsRecursive()
		{
			return MethodInvocations.SelectMany(GetInvocationsRecursive);
		}

		private IEnumerable<MethodInvocation> GetInvocationsRecursive(MethodInvocation invocation)
		{
			yield return invocation;

			foreach (MethodInvocation childInvocation in invocation.ChildInvocations.SelectMany(GetInvocationsRecursive))
			{
				yield return childInvocation;
			}
		}
	}
}
