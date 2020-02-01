using System;
using Tyrrrz.Extensions;

namespace SupportBoi
{
	public static class Utilities
	{
		public static string[] ParseIDs(string args)
		{
			if (string.IsNullOrEmpty(args))
			{
				return new string[0];
			}
			return  args.Trim().Replace("<@!", "").Replace("<@", "").Replace(">", "").Split();
		}
	}
}
