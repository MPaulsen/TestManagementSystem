using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Data;

namespace Test_Management.Models
{
    public class Database
    {
        public static string ConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["Connection:COBA"].ConnectionString;
            }
        }

        /// <summary>
        /// Performs a scalar select and returns the value as a string
        /// </summary>
        /// <param name="qS"></param>
        /// <returns></returns>
        public static String ScalarString(String qS)
        {
            object returnValue = "";
            SqlConnection con = new SqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(qS, con);

            using (con)
            {
                if (con.State == ConnectionState.Open)
                {
                    returnValue = cmd.ExecuteScalar();
                    con.Close();
                }
                else
                {
                    con.Open();
                    returnValue = cmd.ExecuteScalar();
                    con.Close();
                }
            }

            if (returnValue == null)
            {
                return "";
            }
            else
                return returnValue.ToString();
        }

        /// <summary>
        /// Performs a non-query style query
        /// </summary>
        /// <param name="qS"></param>
        public static void nonQuery(String qS)
        {
            SqlConnection con = new SqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(qS, con);

            using (con)
            {
                try
                {
                    if (con.State == ConnectionState.Open)
                        cmd.ExecuteNonQuery();
                    else
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    ;
                }
            }
            con.Close();
        }
    }
}