using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace CSGOStatsED.src
{
    class NPGSQLTest
    {
        public void test()
        {
            using (var conn = new NpgsqlConnection("Username=postgres;Password=arcoavid;Database=CSGODemos"))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;

                    // Insert some data
                    cmd.CommandText = "INSERT INTO data (some_field) VALUES ('Hello world')";
                    cmd.ExecuteNonQuery();

                    // Retrieve all rows
                    cmd.CommandText = "SELECT some_field FROM data";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine(reader.GetString(0));
                        }
                    }
                }
            }
        }
    }
}
