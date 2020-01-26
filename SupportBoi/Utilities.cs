namespace SupportBoi
{
	public static class Utilities
	{
		public static string[] ParseIDs(string args)
		{
			return args.Trim().Replace("<@!", "").Replace("<@", "").Replace(">", "").Split();
		}
	}
}
