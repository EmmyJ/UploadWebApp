using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace UploadWebapp.DB
{
    public class DB : IDisposable
    {
        private SqlConnection Connection;
        private bool disposed = false;
        private string connectionString = "";

        public DB(string ConnectionStringName = "connectionDB")
        {
            connectionString = System.Configuration.ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;

            Connection = new SqlConnection(connectionString);
            Connection.Open();
        }

        public SqlDataReader ExecuteReader(string commandString, params SqlParameter[] parameters)
        {
            using (SqlCommand command = Connection.CreateCommand())
            {
                command.CommandText = commandString;
                command.Parameters.AddRange(parameters);
                return command.ExecuteReader();
            }
        }

        public object ExecuteScalar(string commandString, params SqlParameter[] parameters)
        {
            using (SqlCommand command = Connection.CreateCommand())
            {
                command.CommandText = commandString;
                command.Parameters.AddRange(parameters);
                return command.ExecuteScalar();
            }
        }

        public int Execute(string commandString, params SqlParameter[] parameters)
        {
            using (SqlCommand command = Connection.CreateCommand())
            {
                command.CommandText = commandString;
                command.Parameters.AddRange(parameters);
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Fetches 1 value as an object.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public object FetchSingleValue(string query)
        {
            object returnResult = null;

            try
            {
                using (SqlCommand command = Connection.CreateCommand())
                {
                    command.CommandText = query;
                    returnResult = command.ExecuteScalar();
                }
            }
            catch (Exception e)
            {
            }

            return returnResult;
        }

        

        /// <summary>
        /// Fetches all records returning a DataTable.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public DataTable FetchAllRecords(string query)
        {

            SqlDataAdapter adapter = new SqlDataAdapter(query, Connection);
            DataSet DS = new DataSet();
            //get query results in dataset
            adapter.Fill(DS);
            //return datatable with all records
            return DS.Tables[0];

        }


        public void Dispose()
        {
            if (!this.disposed)
            {
                Connection.Close();
                Connection.Dispose();
                GC.SuppressFinalize(this);
            }
            disposed = true;
        }

        public static string FormatDBDate(System.DateTime datum)
        {
            if (datum.Year == 1900)
            {
                return "";
            }
            else
            {
                return datum.ToString("o");
            }
        }

        

        /// <summary>
        /// QTS: removes quotes - anti-sql-injection - needs to be improved
        /// </summary>
        /// <param name="inputSQL">The input SQL.</param>
        /// <returns></returns>
        public static string qts(string inputSQL)
        {
            inputSQL = inputSQL.Replace("'", "''");
            inputSQL = inputSQL.Replace("\\", "\\\\");
            inputSQL = inputSQL.Replace("char(124)", "");
            return inputSQL;
        }

        /// <summary>
        /// MyString: checks wether string is NULL, if null, SQL NULL string is returned, otherwise the string is returned between quotes and with QTS (anti sql-injection)
        /// </summary>
        /// <param name="inputSQL">The input SQL. Can be NULL</param>
        /// <returns></returns>
        public static string MyString(object inputSQL)
        {
            if (inputSQL == null)
            {
                return "NULL";
            }
            else
            {
                return "'" + qts(inputSQL.ToString()) + "'";
            }
        }

        /// <summary>
        /// MyDateTime: checks wether date is NULL, if null, SQL NULL string is returned
        /// </summary>
        /// <param name="inputSQL">The input SQL. Can be NULL</param>
        /// <returns></returns>
        public static string MyDateTime(object inputSQL)
        {
            if (inputSQL == null)
            {
                return "NULL";
            }
            else
            {
                DateTime dt = Convert.ToDateTime(inputSQL);
                return "'" + dt.ToString("yyyy-MM-dd HH:mm:ss") + "'";
            }
        }

        /// <summary>
        /// MyInt: checks wether int is NULL or not numeric, if null, SQL NULL string is returned, otherwise the number is returned as string
        /// </summary>
        /// <param name="inputSQL">The input SQL. Can be NULL</param>
        /// <returns></returns>
        public static string MyInt(int? inputSQL)
        {
            if (inputSQL == null)
            {
                return "NULL";
            }
            else
            {
                try
                {
                    int.Parse(inputSQL.ToString());
                    return inputSQL.ToString();
                }
                catch
                {
                    return "NULL";
                }
            }
        }

        /// <summary>
        /// MyFloat: checks wether float is NULL or not numeric, if null, SQL NULL string is returned, otherwise the number is returned as string
        /// </summary>
        /// <param name="inputSQL">The input SQL. Can be NULL</param>
        /// <returns></returns>
        public static string MyFloat(float? inputSQL)
        {
            if (inputSQL == null)
            {
                return "NULL";
            }
            else
            {
                try
                {
                    float.Parse(inputSQL.ToString());
                    return "'" + inputSQL.ToString().Replace(',', '.') + "'";
                }
                catch
                {
                    return "NULL";
                }
            }
        }

        /// <summary>
        /// MyDouble: checks wether double is NULL or not numeric, if null, SQL NULL string is returned, otherwise the number is returned as string
        /// </summary>
        /// <param name="inputSQL">The input SQL. Can be NULL</param>
        /// <returns></returns>
        public static string MyDouble(double? inputSQL)
        {
            if (inputSQL == null)
            {
                return "NULL";
            }
            else
            {
                try
                {
                    double.Parse(inputSQL.ToString());
                    return "'" + inputSQL.ToString().Replace(',', '.') + "'";
                }
                catch
                {
                    return "NULL";
                }
            }
        }

        /// <summary>
        /// MyDecimal: checks wether decimal is NULL or not numeric, if null, SQL NULL string is returned, otherwise the number is returned as string
        /// </summary>
        /// <param name="inputSQL">The input SQL. Can be NULL</param>
        /// <returns></returns>
        public static string MyDecimal(decimal? inputSQL)
        {
            if (inputSQL == null)
            {
                return "NULL";
            }
            else
            {
                try
                {
                    decimal.Parse(inputSQL.ToString());
                    return "'" + inputSQL.ToString().Replace(',', '.') + "'";
                }
                catch
                {
                    return "NULL";
                }
            }
        }

        /// <summary>
        /// MyBool: checks wether bool is NULL or not numeric, if null, SQL NULL string is returned, otherwise the bool is returned as 1 or 0
        /// </summary>
        /// <param name="inputSQL">The input SQL. Can be NULL</param>
        /// <returns></returns>
        public static string MyBool(bool? inputSQL)
        {
            if (inputSQL == null)
            {
                return "NULL";
            }
            else
            {
                if (inputSQL == true)
                {
                    return "1";
                }
                else
                {
                    return "0";
                }
            }
        }
    }
}