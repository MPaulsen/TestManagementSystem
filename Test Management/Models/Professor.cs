using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.Data.SqlClient;

namespace Test_Management.Models
{
    public class Professor
    {
        int id = 0;
        string firstName = string.Empty;
        string lastName = string.Empty;
        string email = string.Empty;
        string office = string.Empty;
        string username = string.Empty;
        string phone = string.Empty;

        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }

        public string FirstName
        {
            get
            {
                return firstName;
            }
            set
            {
                firstName = value;
            }
        }

        public string LastName
        {
            get
            {
                return lastName;
            }
            set
            {
                lastName = value;
            }
        }

        public string Email
        {
            get
            {
                return email;
            }
            set
            {
                email = value;
            }
        }

        public string Office
        {
            get
            {
                return office;
            }
            set
            {
                office = value;
            }
        }

        public string Username
        {
            get
            {
                return username;
            }
            set
            {
                username = value;
            }
        }

        public string Phone
        {
            get
            {
                return phone;
            }
            set
            {
                phone = value;
            }
        }

        public static Professor Get(string nid)
        {
            Professor newProf = new Professor();

            SqlConnection con = new System.Data.SqlClient.SqlConnection(Database.ConnectionString);

            string query = @"SELECT Professor_ID, First_Name, Last_Name, Email, Emergency_Phone
                                FROM [dbo].[tlProfessors]
                                WHERE CBAUserName = '" + nid + "'";

            SqlCommand cmd = new System.Data.SqlClient.SqlCommand(query, con);

            if (con.State == ConnectionState.Closed)
                con.Open();
            SqlDataReader rdr = cmd.ExecuteReader();
                
            if (!rdr.HasRows)
                return null;

            rdr.Read();

            newProf.id = Convert.ToInt32(rdr[0].ToString());
            newProf.firstName = rdr[1].ToString();
            newProf.lastName = rdr[2].ToString();
            newProf.email = rdr[3].ToString();
            newProf.phone = rdr[4].ToString();

            con.Close();
                
            

            return newProf;
        }
    }
}

