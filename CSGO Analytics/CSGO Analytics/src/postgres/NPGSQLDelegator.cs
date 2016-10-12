using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using System.Net.Sockets;

namespace CSGO_Analytics.src.postgres
{
    class NPGSQLDelegator
    {

        private static string CONN_STRING = "Host=localhost" + ";Port=5432" + ";Username=postgres" + ";Password=arcoavid" + ";Database=CSGODemos";

        /// <summary>
        /// Timeout for commands sent to the database
        /// </summary>
        private static int TIMEOUT = 40;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pConnection"></param>
        /// <param name="pCommand"></param>
        /// <returns></returns>
        private static NpgsqlCommand createCommand(NpgsqlConnection pConnection, string pCommand)
        {
            NpgsqlCommand cmd = new NpgsqlCommand(pCommand, pConnection);
            cmd.CommandText = pCommand;
            cmd.CommandTimeout = TIMEOUT;
            cmd.CommandType = System.Data.CommandType.Text;
            return cmd;
        }

        /// <summary>
        /// Returns a stream containing the result of a sql command
        /// </summary>
        /// <param name="commandtext"></param>
        /// <returns></returns>
        public static Stream fetchCommandStream(string commandtext)
        {
            using (var conn = new NpgsqlConnection(CONN_STRING))
            {
                conn.Open();

                using (var cmd = createCommand(conn, commandtext))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        MemoryStream stream = new MemoryStream();
                        StreamWriter writer = new StreamWriter(stream);
                        while (reader.Read())
                        {
                            /*
                            //Not supported yet -.-
                            using (var results = reader.GetStream(0))
                            {
                                results.CopyTo(stream);
                            }*/
                            //Flush every string recieved from the reader into the stream
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                Console.WriteLine("Write and Flush to stream at index: " + i);
                                writer.Write(reader[i]);
                                writer.Flush();
                            }
                        }

                        stream.Position = 0;

                        reader.Close();
                        conn.Close();
                        return stream;
                    }
                }
            }
        }

        private static string COMMIT = "INSERT INTO demodata(jsondata) VALUES(:jsondata) ";

        /// <summary>
        /// Uploads the json at path to the DB
        /// </summary>
        /// <param name="path"></param>
        public static void commitJSONFile(string path)
        {
            using (var r = new StreamReader(path)) //read json
            {
                string json = r.ReadToEnd();
                using (var conn = new NpgsqlConnection(CONN_STRING)) //establish connection to db
                {

                    conn.Open();

                    using (var cmd = createCommand(conn, COMMIT)) //commit the json to db
                    {
                        var parameter = new NpgsqlParameter();
                        parameter.ParameterName = "jsondata";
                        parameter.Value = json;
                        parameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
                        cmd.Parameters.Add(parameter);
                        cmd.ExecuteNonQuery();
                    }

                    conn.Close();
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

    }
}
