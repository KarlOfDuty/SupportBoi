using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace SupportBoi
{
	public static class Database
	{
		private static string connectionString = "";

		private static Random random = new Random();

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
					"CREATE TABLE IF NOT EXISTS staff(" +
					"user_id BIGINT UNSIGNED NOT NULL UNIQUE PRIMARY KEY," +
					"name VARCHAR(256) NOT NULL UNIQUE," +
					"active BOOLEAN NOT NULL DEFAULT true," +
					"INDEX(user_id, name))",
					c);
				c.Open();
				createTickets.ExecuteNonQuery();
				createBlacklisted.ExecuteNonQuery();
				createTicketHistory.ExecuteNonQuery();
				createStaffList.ExecuteNonQuery();
			}
		}
		public static bool IsOpenTicket(ulong channelID)
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
		public static bool TryGetOpenTicket(ulong channelID, out Ticket ticket)
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
					ticket = null;
					return false;
				}

				ticket = new Ticket(results);
				results.Close();
				return true;
			}
		}
		public static bool TryGetOpenTicketByID(uint id, out Ticket ticket)
		{
			using (MySqlConnection c = GetConnection())
			{
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets WHERE id=@id", c);
				selection.Parameters.AddWithValue("@id", id);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				// Check if open ticket exists in the database
				if (results.Read())
				{
					ticket = new Ticket(results);
					return true;
				}

				ticket = null;
				return false;
			}
		}
		public static bool TryGetClosedTicket(uint id, out Ticket ticket)
		{
			using (MySqlConnection c = GetConnection())
			{
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM ticket_history WHERE id=@id", c);
				selection.Parameters.AddWithValue("@id", id);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				// Check if closed ticket exists in the database
				if (results.Read())
				{
					ticket = new Ticket(results);
					return true;
				}

				ticket = null;
				return false;
			}
		}
		public static bool TryGetOpenTickets(ulong userID, out List<Ticket> tickets)
		{
			tickets = null;
			using (MySqlConnection c = GetConnection())
			{
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets WHERE creator_id=@creator_id", c);
				selection.Parameters.AddWithValue("@creator_id", userID);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				if (!results.Read())
				{
					return false;
				}

				tickets = new List<Ticket> { new Ticket(results) };
				while (results.Read())
				{
					tickets.Add(new Ticket(results));
				}
				results.Close();
				return true;
			}
		}
		public static bool TryGetOldestTickets(ulong userID, out List<Ticket> tickets)
		{
			tickets = null;
			using (MySqlConnection c = GetConnection())
			{
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets ORDER BY created_time ASC LIMIT 20", c);
				selection.Parameters.AddWithValue("@creator_id", userID);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				if (!results.Read())
				{
					return false;
				}

				tickets = new List<Ticket> { new Ticket(results) };
				while (results.Read())
				{
					tickets.Add(new Ticket(results));
				}
				results.Close();
				return true;
			}
		}
		public static bool TryGetClosedTickets(ulong userID, out List<Ticket> tickets)
		{
			tickets = null;
			using (MySqlConnection c = GetConnection())
			{
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM ticket_history WHERE creator_id=@creator_id", c);
				selection.Parameters.AddWithValue("@creator_id", userID);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				if (!results.Read())
				{
					return false;
				}

				tickets = new List<Ticket> { new Ticket(results) };
				while (results.Read())
				{
					tickets.Add(new Ticket(results));
				}
				results.Close();
				return true;
			}
		}
		public static bool TryGetAssignedTickets(ulong staffID, out List<Ticket> tickets)
		{
			tickets = null;
			using (MySqlConnection c = GetConnection())
			{
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets WHERE assigned_staff_id=@assigned_staff_id", c);
				selection.Parameters.AddWithValue("@assigned_staff_id", staffID);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				if (!results.Read())
				{
					return false;
				}

				tickets = new List<Ticket> { new Ticket(results) };
				while (results.Read())
				{
					tickets.Add(new Ticket(results));
				}
				results.Close();
				return true;
			}
		}
		public static long NewTicket(ulong memberID, ulong staffID, ulong ticketID)
		{
			using (MySqlConnection c = GetConnection())
			{
				c.Open();
				MySqlCommand cmd = new MySqlCommand(
					@"INSERT INTO tickets (created_time, creator_id, assigned_staff_id, summary, channel_id) VALUES (now(), @creator_id, @assigned_staff_id, @summary, @channel_id);",
					c);
				cmd.Parameters.AddWithValue("@creator_id", memberID);
				cmd.Parameters.AddWithValue("@assigned_staff_id", staffID);
				cmd.Parameters.AddWithValue("@summary", "");
				cmd.Parameters.AddWithValue("@channel_id", ticketID);
				cmd.ExecuteNonQuery();
				return cmd.LastInsertedId;
			}
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
		public static bool AssignStaff(Ticket ticket, ulong staffID)
		{
			using (MySqlConnection c = GetConnection())
			{
				try
				{
					c.Open();
					MySqlCommand update = new MySqlCommand(@"UPDATE tickets SET assigned_staff_id = @assigned_staff_id, created_time = @created_time WHERE id = @id", c);
					update.Parameters.AddWithValue("@assigned_staff_id", staffID);
					update.Parameters.AddWithValue("@created_time", ticket.createdTime);
					update.Parameters.AddWithValue("@id", ticket.id);
					update.Prepare();
					return update.ExecuteNonQuery() > 0;
				}
				catch (MySqlException)
				{
					return false;
				}

			}
		}
		public static bool UnassignStaff(Ticket ticket)
		{
			return AssignStaff(ticket, 0);
		}
		public static StaffMember GetRandomActiveStaff(ulong currentStaffID)
		{
			using (MySqlConnection c = GetConnection())
			{
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM staff WHERE active = true AND user_id != @user_id", c);
				selection.Parameters.AddWithValue("@user_id", currentStaffID);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				// Check if staff exists in the database
				if (!results.Read())
				{
					return null;
				}

				List<StaffMember> staffMembers = new List<StaffMember> { new StaffMember(results) };
				while (results.Read())
				{
					staffMembers.Add(new StaffMember(results));
				}
				results.Close();

				return staffMembers[random.Next(staffMembers.Count)];
			}
		}
		public static bool IsStaff(ulong staffID)
		{
			using (MySqlConnection c = GetConnection())
			{
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM staff WHERE user_id=@user_id", c);
				selection.Parameters.AddWithValue("@user_id", staffID);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				// Check if ticket exists in the database
				if (!results.Read())
				{
					return false;
				}
				results.Close();
				return true;
			}
		}
		public static bool TryGetStaff(ulong staffID, out StaffMember staffMember)
		{
			using (MySqlConnection c = GetConnection())
			{
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM staff WHERE user_id=@user_id", c);
				selection.Parameters.AddWithValue("@user_id", staffID);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				// Check if ticket exists in the database
				if (!results.Read())
				{
					staffMember = null;
					return false;
				}
				staffMember = new StaffMember(results);
				results.Close();
				return true;
			}
		}
		public class Ticket
		{
			public uint id;
			public DateTime createdTime;
			public ulong creatorID;
			public ulong assignedStaffID;
			public string summary;
			public ulong channelID;

			public Ticket(MySqlDataReader reader)
			{
				this.id = reader.GetUInt32("id");
				this.createdTime = reader.GetDateTime("created_time");
				this.creatorID = reader.GetUInt64("creator_id");
				this.assignedStaffID = reader.GetUInt64("assigned_staff_id");
				this.summary = reader.GetString("summary");
				this.channelID = reader.GetUInt64("channel_id");
			}
		}
		public class StaffMember
		{
			public ulong userID;
			public string name;
			public bool active;

			public StaffMember(MySqlDataReader reader)
			{
				this.userID = reader.GetUInt64("user_id");
				this.name = reader.GetString("name");
				this.active = reader.GetBoolean("active");
			}
		}
	}
}
