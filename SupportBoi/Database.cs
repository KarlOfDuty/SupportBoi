using System;
using System.Transactions;
using MySql.Data.MySqlClient;

namespace SupportBoi
{
	public static class Database
	{
		private static String connectionString = "";

		public static void SetConnectionString(string host, int port, string database, string username, string password)
		{
			connectionString = "server=" + host + 
			                   ";database=" + database + 
			                   ";port=" + port + 
			                   ";userid=" + username + 
			                   ";password=" + password;
		}

		public static MySqlConnection GetConnection()
		{
			return new MySqlConnection(connectionString);
		}

		public static long GetNumberOfTickets()
		{
			try
			{
				using (MySqlConnection c = GetConnection())
				{
					MySqlCommand countTickets = new MySqlCommand("SELECT COUNT(*) FROM tickets", c);
					c.Open();
					return (long)countTickets.ExecuteScalar();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Error occured when attempting to count number of open tickets: " + e);
			}

			return -1;
		}

		public static long GetNumberOfClosedTickets()
		{
			try
			{
				using (MySqlConnection c = GetConnection())
				{
					MySqlCommand countTickets = new MySqlCommand("SELECT COUNT(*) FROM ticket_history", c);
					c.Open();
					return (long)countTickets.ExecuteScalar();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Error occured when attempting to count number of open tickets: " + e);
			}

			return -1;
		}

		public static void SetupTables()
		{
			using (MySqlConnection c = GetConnection())
			{
				MySqlCommand createTickets = new MySqlCommand(
					"CREATE TABLE IF NOT EXISTS tickets(" +
					"id INT UNSIGNED NOT NULL UNIQUE PRIMARY KEY AUTO_INCREMENT," +
					"created_time TIMESTAMP NOT NULL," +
					"creator_id BIGINT UNSIGNED NOT NULL," +
					"assigned_staff_id BIGINT UNSIGNED NOT NULL DEFAULT 0," +
					"summary VARCHAR(5000) NOT NULL," +
					"channel_id BIGINT UNSIGNED NOT NULL UNIQUE," +
					"INDEX(created_time, assigned_staff_id, channel_id))",
					c);
				MySqlCommand createTicketHistory = new MySqlCommand(
					"CREATE TABLE IF NOT EXISTS ticket_history(" +
					"id INT UNSIGNED NOT NULL UNIQUE PRIMARY KEY," +
					"created_time TIMESTAMP NOT NULL," +
					"closed_time TIMESTAMP NOT NULL," +
					"creator_id BIGINT UNSIGNED NOT NULL," +
					"assigned_staff_id BIGINT UNSIGNED NOT NULL DEFAULT 0," +
					"summary VARCHAR(5000) NOT NULL," +
					"channel_id BIGINT UNSIGNED NOT NULL UNIQUE," +
					"INDEX(created_time, closed_time, channel_id))",
					c);
				MySqlCommand createBlacklisted = new MySqlCommand(
					"CREATE TABLE IF NOT EXISTS blacklisted_users(" +
					"user_id BIGINT UNSIGNED NOT NULL UNIQUE PRIMARY KEY," +
					"time TIMESTAMP NOT NULL," +
					"moderator_id BIGINT UNSIGNED NOT NULL," +
					"INDEX(user_id, time))",
					c);
				MySqlCommand createStaffList = new MySqlCommand(
					"CREATE TABLE IF NOT EXISTS staff_list(" +
					"user_id BIGINT UNSIGNED NOT NULL UNIQUE PRIMARY KEY," +
					"username VARCHAR(256) NOT NULL UNIQUE," +
					"active BOOLEAN NOT NULL DEFAULT true," +
					"INDEX(user_id, username))",
					c);
				c.Open();
				createTickets.ExecuteNonQuery();
				createBlacklisted.ExecuteNonQuery();
				createTicketHistory.ExecuteNonQuery();
				createStaffList.ExecuteNonQuery();
			}
		}
		public static bool IsTicket(ulong channelID)
		{
			using (MySqlConnection c = GetConnection())
			{
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets WHERE channel_id=@channel_id", c);
				selection.Parameters.AddWithValue("@channel_id", channelID);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				// Check if ticket exists in the database
				if (!results.Read())
				{
					return false;
				}
				results.Close();
			}
			return true;
		}
		public static bool IsBlacklisted(ulong userID)
		{
			using (MySqlConnection c = GetConnection())
			{
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM blacklisted_users WHERE user_id=@user_id", c);
				selection.Parameters.AddWithValue("@user_id", userID);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				// Check if user is blacklisted
				if (results.Read())
				{
					return true;
				}
				results.Close();
			}

			return false;
		}
		public static bool Blacklist(ulong blacklistedID, ulong staffID)
		{
			using (MySqlConnection c = GetConnection())
			{
				try
				{
					c.Open();
					MySqlCommand cmd = new MySqlCommand(@"INSERT INTO blacklisted_users (user_id,time,moderator_id) VALUES (@user_id, now(), @moderator_id);", c);
					cmd.Parameters.AddWithValue("@user_id", blacklistedID);
					cmd.Parameters.AddWithValue("@moderator_id", staffID);
					cmd.Prepare();
					return cmd.ExecuteNonQuery() > 0;
				}
				catch (MySqlException)
				{
					return false;
				}
			}
		}
		public static bool Unblacklist(ulong blacklistedID)
		{
			using (MySqlConnection c = GetConnection())
			{
				try
				{
					c.Open();
					MySqlCommand cmd = new MySqlCommand(@"DELETE FROM blacklisted_users WHERE user_id=@user_id", c);
					cmd.Parameters.AddWithValue("@user_id", blacklistedID);
					cmd.Prepare();
					return cmd.ExecuteNonQuery() > 0;
				}
				catch (MySqlException)
				{
					return false;
				}

			}
		}
	}
}
