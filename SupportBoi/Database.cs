using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace SupportBoi
{
	public static class Database
	{
		private static string connectionString = "";

		private static readonly Random random = new Random();

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

		/// <summary>
		///		Everything related to tickets.
		/// </summary>
		public static class TicketLinked
		{
			/// <summary>
			///		Gets the total number of tickets.
			/// </summary>
			public static long GetTotalOpenedTickets()
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
			/// <summary>
			///		Gets the total number of closed tickets.
			/// </summary>
			public static long GetTotalClosedTickets()
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
			/// <summary>
			///		Generates a new ticket and returns its Id.
			/// </summary>
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
			/// <summary>
			///		Removes the ticket from the active tickets.
			/// </summary>
			public static bool DeleteOpenTicket(Ticket ticket)
			{
				using (MySqlConnection connection = GetConnection())
				{
					try
					{
						MySqlCommand deletion = new MySqlCommand(@"DELETE FROM tickets WHERE channel_id=@channel_id", connection);
						deletion.Parameters.AddWithValue("@channel_id", ticket.channelID);
						deletion.Prepare();
						return deletion.ExecuteNonQuery() > 0;
					}
					catch (MySqlException)
					{
						return false;
					}
				}
			}
			/// <summary>
			///		Closes the ticket.
			/// </summary>
			public static bool CloseTicket(Ticket ticket)
			{
				using (MySqlConnection connection = GetConnection())
				{
					try
					{
						// Create an entry in the ticket history database
						MySqlCommand archiveTicket = new MySqlCommand(@"INSERT INTO ticket_history (id, created_time, closed_time, creator_id, assigned_staff_id, summary, channel_id) VALUES (@id, @created_time, now(), @creator_id, @assigned_staff_id, @summary, @channel_id);", connection);
						archiveTicket.Parameters.AddWithValue("@id", ticket.id);
						archiveTicket.Parameters.AddWithValue("@created_time", ticket.createdTime);
						archiveTicket.Parameters.AddWithValue("@creator_id", ticket.creatorID);
						archiveTicket.Parameters.AddWithValue("@assigned_staff_id", ticket.assignedStaffID);
						archiveTicket.Parameters.AddWithValue("@summary", ticket.summary);
						// The ticket is obtained thanks to a channel with a ticket, so the channelID is identical to the ticket.channelID
						archiveTicket.Parameters.AddWithValue("@channel_id", ticket.channelID);

						connection.Open();
						return archiveTicket.ExecuteNonQuery() > 0;
					}
					catch (MySqlException)
					{
						return false;
					}
				}

			}
			/// <summary>
			///		Gets the status of the ticket.
			/// </summary>
			public static bool IsOpenTicket(ulong channelId)
			{
				using (MySqlConnection c = GetConnection())
				{
					c.Open();
					MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets WHERE channel_id=@channel_id", c);
					selection.Parameters.AddWithValue("@channel_id", channelId);
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

			/// <summary>
			///		Updates the ticket summary by channelId.
			/// </summary>
			public static bool UpdateTicketSummary(ulong channelId, string summary)
			{
				using (MySqlConnection connection = GetConnection())
				{
					try
					{
						connection.Open();
						MySqlCommand update = new MySqlCommand(@"UPDATE tickets SET summary = @summary WHERE channel_id = @channel_id", connection);
						update.Parameters.AddWithValue("@summary", summary);
						update.Parameters.AddWithValue("@channel_id", channelId);
						update.Prepare();
						return update.ExecuteNonQuery() > 0;
					}
					catch (MySqlException)
					{
						return false;
					}
				}
			}

			#region Trying functions 

			/// <summary>
			///		Tries to get an open ticket.
			/// </summary>
			public static bool TryGetOpenTicket(ulong channelId, out Ticket ticket)
			{
				using (MySqlConnection c = GetConnection())
				{
					c.Open();
					MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets WHERE channel_id=@channel_id", c);
					selection.Parameters.AddWithValue("@channel_id", channelId);
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
			/// <summary>
			///		Tries to get an open ticket by ticketId.
			/// </summary>
			public static bool TryGetOpenTicketById(uint id, out Ticket ticket)
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
			/// <summary>
			///		Tries to get an closed ticket.
			/// </summary>
			public static bool TryGetClosedTicketById(uint id, out Ticket ticket)
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
			/// <summary>
			///		Tries to get the user's open tickets.
			/// </summary>
			public static bool TryGetOpenTicketsByUser(ulong userID, out List<Ticket> tickets)
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
			/// <summary>
			///		Tries to get the user's oldest tickets.
			/// </summary>
			public static bool TryGetOldestTickets(ulong userId, out List<Ticket> tickets, int limit)
			{
				tickets = null;
				using (MySqlConnection c = GetConnection())
				{
					c.Open();
					MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets ORDER BY created_time ASC LIMIT @limit", c);
					selection.Parameters.AddWithValue("@creator_id", userId);
					selection.Parameters.AddWithValue("@limit", limit);
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
			/// <summary>
			///		Tries to get the user's closed tickets.
			/// </summary>
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
			/// <summary>
			///		Tries to get associated tickets with a staff.
			/// </summary>
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

			#endregion
		}

		/// <summary>
		///		Everything related to users.
		/// </summary>
		public static class UserLinked
		{
			/// <summary>
			///		Returns whether the user is blocked from using the ticket system.
			/// </summary>
			public static bool IsBlocked(ulong userId)
			{
				using (MySqlConnection c = GetConnection())
				{
					c.Open();
					MySqlCommand selection = new MySqlCommand(@"SELECT * FROM blacklisted_users WHERE user_id=@user_id", c);
					selection.Parameters.AddWithValue("@user_id", userId);
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
			/// <summary>
			///		Blocks the user from using the system ticket.
			/// </summary>
			public static bool Block(ulong userId, ulong staffID)
			{
				using (MySqlConnection c = GetConnection())
				{
					try
					{
						c.Open();
						MySqlCommand cmd = new MySqlCommand(@"INSERT INTO blacklisted_users (user_id,time,moderator_id) VALUES (@user_id, now(), @moderator_id);", c);
						cmd.Parameters.AddWithValue("@user_id", userId);
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
			/// <summary>
			///		Unblock the user to use the system ticket.
			/// </summary>
			public static bool UnBlock(ulong userId)
			{
				using (MySqlConnection c = GetConnection())
				{
					try
					{
						c.Open();
						MySqlCommand cmd = new MySqlCommand(@"DELETE FROM blacklisted_users WHERE user_id=@user_id", c);
						cmd.Parameters.AddWithValue("@user_id", userId);
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

		public static class StaffLinked
		{
			/// <summary>
			///		Assigns staff to the ticket.
			/// </summary>
			public static bool AssignStaff(Ticket ticket, ulong userId)
			{
				using (MySqlConnection c = GetConnection())
				{
					try
					{
						c.Open();
						MySqlCommand update = new MySqlCommand(@"UPDATE tickets SET assigned_staff_id = @assigned_staff_id WHERE id = @id", c);
						update.Parameters.AddWithValue("@assigned_staff_id", userId);
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
			/// <summary>
			///		Unassigns staff to the ticket.
			/// </summary>
			public static bool UnassignStaff(Ticket ticket)
			{
				return AssignStaff(ticket, 0);
			}
			/// <summary>
			///		Returns whether the user is a staff.
			/// </summary>
			public static bool IsStaff(ulong userId)
			{
				using (MySqlConnection c = GetConnection())
				{
					c.Open();
					MySqlCommand selection = new MySqlCommand(@"SELECT * FROM staff WHERE user_id=@user_id", c);
					selection.Parameters.AddWithValue("@user_id", userId);
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
			/// <summary>
			///		Trying to get a staff by snowflake.
			/// </summary>
			public static bool TryGetStaff(ulong userId, out StaffMember staffMember)
			{
				using (MySqlConnection c = GetConnection())
				{
					c.Open();
					MySqlCommand selection = new MySqlCommand(@"SELECT * FROM staff WHERE user_id=@user_id", c);
					selection.Parameters.AddWithValue("@user_id", userId);
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
			/// <summary>
			///		Gets a random active staff.
			/// </summary>
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
			/// <summary>
			///		Updates the staff entity, creates if not, and updates the nickname if it exists.
			/// </summary>
			public static bool UpdateStaff(ulong userId, string userName) // I don't think it's right to keep a staff nickname
			{
				using (MySqlConnection connection = GetConnection())
				{
					try
					{
						MySqlCommand command = IsStaff(userId) ? new MySqlCommand(@"UPDATE staff SET name = @name WHERE user_id = @user_id", connection) :
							new MySqlCommand(@"INSERT INTO staff (user_id, name) VALUES (@user_id, @name);", connection);

						connection.Open();
						command.Parameters.AddWithValue("@user_id", userId);
						command.Parameters.AddWithValue("@name", userName);
						return command.ExecuteNonQuery() > 0;
					}
					catch (MySqlException)
					{
						return false;
					}
				}
			}
			/// <summary>
			///		Updates the staff activity.
			/// </summary>
			public static bool UpdateStaffActive(ulong staffId, bool active)
			{
				using (MySqlConnection connection = GetConnection())
				{
					try
					{
						connection.Open();
						MySqlCommand update = new MySqlCommand(@"UPDATE staff SET active = @active WHERE user_id = @user_id", connection);
						update.Parameters.AddWithValue("@user_id", staffId);
						update.Parameters.AddWithValue("@active", active);
						update.Prepare();
						return update.ExecuteNonQuery() > 0;
					}
					catch (MySqlException)
					{
						return false;
					}
				}
			}
			/// <summary>
			///		Removes the user from the staff.
			/// </summary>
			public static bool RemoveStaff(ulong userId)
			{
				using (MySqlConnection connection = GetConnection())
				{
					try
					{
						connection.Open();
						MySqlCommand deletion = new MySqlCommand(@"DELETE FROM staff WHERE user_id=@user_id", connection);
						deletion.Parameters.AddWithValue("@user_id", userId);
						deletion.Prepare();
						return deletion.ExecuteNonQuery() > 0;
					}
					catch (MySqlException)
					{
						return false;
					}
				}
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
