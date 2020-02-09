using System.Collections.Generic;

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

		public static LinkedList<string> ParseListIntoMessages(List<string> listItems)
		{ 
			LinkedList<string> messages = new LinkedList<string>();

			foreach (string listItem in listItems)
			{
				if (messages.Last?.Value?.Length + listItem?.Length < 2048)
				{
					messages.Last.Value += listItem;
				}
				else
				{
					messages.AddLast(listItem);
				}
			}

			return messages;
		}
	}
}
