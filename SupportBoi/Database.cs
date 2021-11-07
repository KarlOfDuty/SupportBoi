﻿using System;
using System.Linq;
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
					"id INT UNSIGNED NOT NULL PRIMARY KEY AUTO_INCREMENT," +
					"created_time DATETIME NOT NULL," +
					"creator_id BIGINT UNSIGNED NOT NULL," +
					"assigned_staff_id BIGINT UNSIGNED NOT NULL DEFAULT 0," +
					"summary VARCHAR(5000) NOT NULL," +
					"channel_id BIGINT UNSIGNED NOT NULL UNIQUE," +
					"INDEX(created_time, assigned_staff_id, channel_id))",
					c);
				MySqlCommand createTicketHistory = new MySqlCommand(
					"CREATE TABLE IF NOT EXISTS ticket_history(" +
					"id INT UNSIGNED NOT NULL PRIMARY KEY," +
					"created_time DATETIME NOT NULL," +
					"closed_time DATETIME NOT NULL," +
					"creator_id BIGINT UNSIGNED NOT NULL," +
					"assigned_staff_id BIGINT UNSIGNED NOT NULL DEFAULT 0," +
					"summary VARCHAR(5000) NOT NULL," +
					"channel_id BIGINT UNSIGNED NOT NULL UNIQUE," +
					"INDEX(created_time, closed_time, channel_id))",
					c);
				MySqlCommand createBlacklisted = new MySqlCommand(
					"CREATE TABLE IF NOT EXISTS blacklisted_users(" +
					"user_id BIGINT UNSIGNED NOT NULL PRIMARY KEY," +
					"time DATETIME NOT NULL," +
					"moderator_id BIGINT UNSIGNED NOT NULL," +
					"INDEX(user_id, time))",
					c);
				MySqlCommand createStaffList = new MySqlCommand(
					"CREATE TABLE IF NOT EXISTS staff(" +
					"user_id BIGINT UNSIGNED NOT NULL PRIMARY KEY," +
					"name VARCHAR(256) NOT NULL," +
					"active BOOLEAN NOT NULL DEFAULT true)",
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
		public static bool TryGetOldestTickets(ulong userID, out List<Ticket> tickets, int listLimit)
		{
			tickets = null;
			using (MySqlConnection c = GetConnection())
			{
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets ORDER BY created_time ASC LIMIT @limit", c);
				selection.Parameters.AddWithValue("@creator_id", userID);
				selection.Parameters.AddWithValue("@limit", listLimit);
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
					@"INSERT INTO tickets (created_time, creator_id, assigned_staff_id, summary, channel_id) VALUES (UTC_TIMESTAMP(), @creator_id, @assigned_staff_id, @summary, @channel_id);",
					c);
				cmd.Parameters.AddWithValue("@creator_id", memberID);
				cmd.Parameters.AddWithValue("@assigned_staff_id", staffID);
				cmd.Parameters.AddWithValue("@summary", "");
				cmd.Parameters.AddWithValue("@channel_id", ticketID);
				cmd.ExecuteNonQuery();
				return cmd.LastInsertedId;
			}
		}

		public static void ArchiveTicket(Ticket ticket)
		{
			// Check if ticket already exists in the archive
			if (TryGetClosedTicket(ticket.id, out Ticket _))
			{
				using (MySqlConnection c = GetConnection())
				{
					MySqlCommand deleteTicket = new MySqlCommand(@"DELETE FROM ticket_history WHERE id=@id OR channel_id=@channel_id", c);
					deleteTicket.Parameters.AddWithValue("@id", ticket.id);
					deleteTicket.Parameters.AddWithValue("@channel_id", ticket.channelID);

					c.Open();
					deleteTicket.Prepare();
					deleteTicket.ExecuteNonQuery();
				}
			}

			using (MySqlConnection c = GetConnection())
			{
				// Create an entry in the ticket history database
				MySqlCommand archiveTicket = new MySqlCommand(@"INSERT INTO ticket_history (id, created_time, closed_time, creator_id, assigned_staff_id, summary, channel_id) VALUES (@id, @created_time, UTC_TIMESTAMP(), @creator_id, @assigned_staff_id, @summary, @channel_id);", c);
				archiveTicket.Parameters.AddWithValue("@id", ticket.id);
				archiveTicket.Parameters.AddWithValue("@created_time", ticket.createdTime);
				archiveTicket.Parameters.AddWithValue("@creator_id", ticket.creatorID);
				archiveTicket.Parameters.AddWithValue("@assigned_staff_id", ticket.assignedStaffID);
				archiveTicket.Parameters.AddWithValue("@summary", ticket.summary);
				archiveTicket.Parameters.AddWithValue("@channel_id", ticket.channelID);

				c.Open();
				archiveTicket.Prepare();
				archiveTicket.ExecuteNonQuery();
			}
		}

		public static void DeleteOpenTicket(uint ticketID)
		{
			using (MySqlConnection c = GetConnection())
			{
				MySqlCommand deletion = new MySqlCommand(@"DELETE FROM tickets WHERE id=@id", c);
				deletion.Parameters.AddWithValue("@id", ticketID);

				c.Open();
				deletion.Prepare();
				deletion.ExecuteNonQuery();
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
					MySqlCommand cmd = new MySqlCommand(@"INSERT INTO blacklisted_users (user_id,time,moderator_id) VALUES (@user_id, UTC_TIMESTAMP(), @moderator_id);", c);
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
					MySqlCommand update = new MySqlCommand(@"UPDATE tickets SET assigned_staff_id = @assigned_staff_id WHERE id = @id", c);
					update.Parameters.AddWithValue("@assigned_staff_id", staffID);
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
			List<StaffMember> staffMembers = GetActiveStaff(currentStaffID);
			if (!staffMembers.Any())
			{
				return null;
			}

			return staffMembers[random.Next(staffMembers.Count)];
		}

		public static List<StaffMember> GetActiveStaff(ulong currentStaffID = 0)
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
					return new List<StaffMember>();
				}

				List<StaffMember> staffMembers = new List<StaffMember> { new StaffMember(results) };
				while (results.Read())
				{
					staffMembers.Add(new StaffMember(results));
				}
				results.Close();

				return staffMembers;
			}
		}

		public static List<StaffMember> GetAllStaff(ulong currentStaffID = 0)
		{
			using (MySqlConnection c = GetConnection())
			{
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM staff WHERE user_id != @user_id", c);
				selection.Parameters.AddWithValue("@user_id", currentStaffID);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				// Check if staff exists in the database
				if (!results.Read())
				{
					return new List<StaffMember>();
				}

				List<StaffMember> staffMembers = new List<StaffMember> { new StaffMember(results) };
				while (results.Read())
				{
					staffMembers.Add(new StaffMember(results));
				}
				results.Close();

				return staffMembers;
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

			public string FormattedCreatedTime()
			{
				return this.createdTime.ToString(Config.timestampFormat);
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
