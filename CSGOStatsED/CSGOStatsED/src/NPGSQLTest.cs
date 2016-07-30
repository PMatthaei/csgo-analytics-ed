using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace CSGOStatsED.src
{
    class NPGSQLTest
    {
        public static void test()
        {
            using (var conn = new NpgsqlConnection("Host=localhost;Port=5432;Username=postgres;Password=arcoavid;Database=CSGODemos"))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;

                    // Insert some data
                    //cmd.CommandText = "INSERT INTO data (some_field) VALUES ('Hello world')";
                    //cmd.ExecuteNonQuery();
                    // Retrieve all rows
                    cmd.CommandText = "SELECT data->>'meta' FROM stats_data";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine(reader.GetString(0));
                        }
                    }
                }
                conn.Close();
            }


        }

        public string loadJsonFromPath(string path)
        {
            using (StreamReader r = new StreamReader(path))
            {
                return r.ReadToEnd();
            }
        }


        public string readJsonFromDB(string commandtext)
        {
            using (var cmd = new NpgsqlCommand())
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine(reader.GetString(0));
                    }
                }
            }

            return "";
        }


    }
}
