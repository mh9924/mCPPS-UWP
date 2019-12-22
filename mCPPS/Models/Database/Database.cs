using Microsoft.Data.Sqlite;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace mCPPS.Models
{
    public static class Database
    {
        private static string filename;

        public static async void InitializeDatabase(string filename){
            Database.filename = filename;
            try
            {
                using (SqliteConnection db = new SqliteConnection("Filename=" + filename))
                {
                    await db.OpenAsync();

                    string tableCommand = "CREATE TABLE IF NOT " +
                        "EXISTS penguins (ID INTEGER PRIMARY KEY AUTOINCREMENT, " +
                        "Username VARCHAR(24) NULL, " +
                        "Password BINARY(60) NULL, " +
                        "Randkey VARCHAR(48) NULL)";

                    SqliteCommand createTable = new SqliteCommand(tableCommand, db);

                    await createTable.ExecuteNonQueryAsync();
                }

            } catch (SqliteException ex)
            {
                await MServer.LogMessage("WARN", "Database", "Database encountered the following error while establishing a connection: " + ex);
                Debug.WriteLine(ex);
            }
        }

        public static async Task<bool> UsernameExists(string username)
        {
            using (SqliteConnection db = new SqliteConnection("Filename=" + filename))
            {
                await db.OpenAsync();

                string tableCommand = "SELECT COUNT(*) FROM penguins WHERE Username = @Username";

                SqliteCommand usernameExists = new SqliteCommand(tableCommand, db);
                usernameExists.Parameters.AddWithValue("@Username", username);

                long result = (long)await usernameExists.ExecuteScalarAsync();
                return result == 1;
            }
        }

        public static async Task<object> GetColumnFromUsername(string username, string column)
        {
            using (SqliteConnection db = new SqliteConnection("Filename=" + filename))
            {
                await db.OpenAsync();

                string tableCommand = "SELECT " + column + " FROM penguins WHERE username = @Username";

                SqliteCommand getColumnFromUsername = new SqliteCommand(tableCommand, db);
                getColumnFromUsername.Parameters.AddWithValue("@Username", username);

                SqliteDataReader result = await getColumnFromUsername.ExecuteReaderAsync();
                return result.GetValue(0);
            }
        }


    }
}
