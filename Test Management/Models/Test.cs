using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace Test_Management.Models
{
    public class Test
    {
        int id = 0;
        string name = string.Empty;
        DateTime startDate = DateTime.Now;
        DateTime endDate = DateTime.Now;
        string notes;
        int length;

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

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public DateTime StartDate
        {
            get
            {
                return startDate;
            }
            set
            {
                startDate = value;
            }
        }

        public DateTime EndDate
        {
            get
            {
                return endDate;
            }
            set
            {
                endDate = value;
            }
        }

        public string Notes
        {
            get
            {
                return notes;
            }
            set
            {
                notes = value;
            }
        }

        public int Length
        {
            get
            {
                return length;
            }
            set
            {
                length = value;
            }
        }
        public Course Course { get; set; }

        public static Test Get(int id)
        {
            SqlConnection cn = new SqlConnection(Database.ConnectionString);
            cn.Open();

            SqlCommand cm = cn.CreateCommand();
            cm.CommandType = CommandType.StoredProcedure;
            cm.CommandText = "GetTest";
            cm.Parameters.AddWithValue("@Id", id);
            SqlDataReader dr = cm.ExecuteReader();

            dr.Read();

            Test test = new Test();
            test.Id = Convert.ToInt32(dr["Test_ID"]);
            test.Name = dr["Test_Name"].ToString();
            test.StartDate = Convert.ToDateTime(dr["Open_Date_Time"]);
            test.EndDate = Convert.ToDateTime(dr["Close_Date_Time"]);

            Course course = new Course();
            course.Id = Convert.ToInt32(dr["Course_ID"]);
            course.Prefix = dr["Course_Prefix"].ToString();
            course.Number = dr["Course_Number"].ToString();
            course.Title = dr["Title"].ToString();

            Professor professor = new Professor();
            professor.FirstName = dr["First_Name"].ToString();
            professor.LastName = dr["Last_Name"].ToString();
            course.Professor = professor;

            test.Course = course;

            cn.Close();

            return test;
        }

        public static List<Test> List(string nid)
        {
            List<Test> list = new List<Test>();

            SqlConnection cn = new SqlConnection(Database.ConnectionString);
            cn.Open();

            SqlCommand cm = cn.CreateCommand();
            cm.CommandType = CommandType.StoredProcedure;
            cm.CommandText = "GetAvailableTestList";
            cm.Parameters.AddWithValue("@NID", nid);
            SqlDataReader dr = cm.ExecuteReader();

            while (dr.Read())
            {
                Test test = new Test();
                test.Id = Convert.ToInt32(dr["Test_ID"]);
                test.Name = dr["Test_Name"].ToString();

                Course course = new Course();
                course.Prefix = dr["Course_Prefix"].ToString();
                course.Number = dr["Course_Number"].ToString();
                course.Title = dr["Title"].ToString();

                Professor professor = new Professor();
                professor.FirstName = dr["First_Name"].ToString();
                professor.LastName = dr["Last_Name"].ToString();
                course.Professor = professor;

                test.Course = course;

                list.Add(test);
            }

            cn.Close();

            return list;
        }

        public static List<Test> List(int? courseId = null, int? term = null, bool? active = null, bool? availableInRegionalCampus = null, string nid = null, DateTime? endDate = null)
        {
            List<Test> list = new List<Test>();

            SqlConnection cn = new SqlConnection(Database.ConnectionString);
            cn.Open();

            SqlCommand cm = cn.CreateCommand();
            cm.CommandType = CommandType.StoredProcedure;
            cm.CommandText = "GetTestList";
            cm.Parameters.AddWithValue("@CourseId", courseId);
            cm.Parameters.AddWithValue("@Term", term);
            cm.Parameters.AddWithValue("@Active", active);
            cm.Parameters.AddWithValue("@AvailableInRegionalCampus", availableInRegionalCampus);
            cm.Parameters.AddWithValue("@NID", nid);
            cm.Parameters.AddWithValue("@EndDate", endDate);
            SqlDataReader dr = cm.ExecuteReader();

            while (dr.Read())
            {
                Test test = new Test();
                test.Id = Convert.ToInt32(dr["Test_ID"]);
                test.Name = dr["Test_Name"].ToString();
                test.StartDate = Convert.ToDateTime(dr["Open_Date_Time"]);
                test.EndDate = Convert.ToDateTime(dr["Close_Date_Time"]);

                Course course = new Course();
                course.Id = Convert.ToInt32(dr["Course_ID"]);
                course.Prefix = dr["Course_Prefix"].ToString();
                course.Number = dr["Course_Number"].ToString();
                course.Title = dr["Title"].ToString();

                Professor professor = new Professor();
                professor.FirstName = dr["First_Name"].ToString();
                professor.LastName = dr["Last_Name"].ToString();
                course.Professor = professor;

                test.Course = course;

                list.Add(test);
            }

            cn.Close();

            return list;
        }

        public static List<Test> ListSimple(int? courseId = null, int? term = null, bool? active = null)
        {
            List<Test> list = new List<Test>();

            SqlConnection cn = new SqlConnection(Database.ConnectionString);
            cn.Open();

            SqlCommand cm = cn.CreateCommand();
            cm.CommandType = CommandType.StoredProcedure;
            cm.CommandText = "GetTestListSimple";
            cm.Parameters.AddWithValue("@CourseId", courseId);
            cm.Parameters.AddWithValue("@Term", term);
            cm.Parameters.AddWithValue("@Active", active);
            SqlDataReader dr = cm.ExecuteReader();

            while (dr.Read())
            {
                Test test = new Test();
                test.Id = Convert.ToInt32(dr["Test_ID"]);
                test.Name = dr["Test_Name"].ToString();

                list.Add(test);
            }

            cn.Close();

            return list;
        }
    }
}

