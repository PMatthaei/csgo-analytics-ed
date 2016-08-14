using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace CSGO_Analytics.src.postgres
{
    class NPGSQLDelegator
    {

        private static string CONN_STRING = "Host=" + host + ";Port=" + port + ";Username=" + user + ";Password=" + pw + ";Database=" + db + "";
        private static string host = "localhost";
        private static string port = "5432";
        private static string user = "postgres";
        private static string pw = "arcoavid";
        private static string db = "CSGODemos";

        /// <summary>
        /// returns a stream containing the result of a sql command
        /// </summary>
        /// <param name="commandtext"></param>
        /// <returns></returns>
        public static Stream fetchSQLCommandStream(string commandtext)
        {
            using (var conn = new NpgsqlConnection(CONN_STRING))
            {
                conn.Open();

                using (var cmd = createSQLCommand(conn, commandtext))
                {

                    try //false command?
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            Stream s = reader.GetStream(0);
                            conn.Close();
                            return s;
                        }
                    }
                    catch (NpgsqlException e)
                    {
                        return null;
                    }


                }
            }
        }

        private static NpgsqlCommand createSQLCommand(NpgsqlConnection pConnection, string pCommand)
        {
            NpgsqlCommand cmd = pConnection.CreateCommand();
            cmd.CommandText = pCommand;
            cmd.CommandTimeout = 30;
            cmd.CommandType = System.Data.CommandType.Text;
            return cmd;
        }


        private const string COMMIT = "INSERT INTO stats_data(data) VALUES(:jsondata) ";

        /// <summary>
        /// Uploads the json at path to the DB
        /// </summary>
        /// <param name="path"></param>
        public static void commitJSONFile(string path)
        {
            using (StreamReader r = new StreamReader(path)) //read json
            {
                string json = r.ReadToEnd();
                using (var conn = new NpgsqlConnection(CONN_STRING)) //establish connection to db
                {
                    conn.Open();

                    using (var cmd = createSQLCommand(conn, COMMIT)) //commit the json to db
                    {
                        cmd.Parameters.Add(new NpgsqlParameter("jsondata", json));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }


        public void setConnectionString(string s)
        {
            CONN_STRING = s;
        }

        public string getConnectionString()
        {
            return CONN_STRING;
        }

        public void setHost(string s)
        {
            host = s;
        }

        public string getHost()
        {
            return host;
        }

        public void setPW(string s)
        {
            pw = s;
        }

        public string getPW()
        {
            return pw;
        }

        public void setUser(string s)
        {
            user = s;
        }

        public string getUser()
        {
            return user;
        }


        public void setDB(string s)
        {
            db = s;
        }

        public string getDB()
        {
            return db;
        }
    }
}
