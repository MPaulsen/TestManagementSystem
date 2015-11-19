using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace Test_Management.Models
{
    public class Course
    {
        int id = 0;
        string prefix = string.Empty;
        string number = string.Empty;
        string section = string.Empty;
        string title = string.Empty;
        string department = string.Empty;
        bool availableInRegionalCampus = false;
        bool selected = false;

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

        public string Prefix
        {
            get
            {
                return prefix;
            }
            set
            {
                prefix = value;
            }
        }

        public string Number
        {
            get
            {
                return number;
            }
            set
            {
                number = value;
            }
        }

        public string Section
        {
            get
            {
                return section;
            }
            set
            {
                section = value;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
            }
        }

        public string Department
        {
            get
            {
                return department;
            }
            set
            {
                department = value;
            }
        }

        public bool AvailableInRegionalCampus
        {
            get
            {
                return availableInRegionalCampus;
            }
            set
            {
                availableInRegionalCampus = value;
            }
        }

        public string Name
        {
            get
            {
                return string.Format("{0} {1} ({2})", prefix, number, title);
            }
        }

        public bool Selected
        {
            get
            {
                return selected;
            }
            set
            {
                selected = value;
            }
        }

        public Professor Professor { get; set; }

        public void Update()
        {
            SqlConnection cn = new SqlConnection(Database.ConnectionString);
            cn.Open();

            SqlCommand cm = cn.CreateCommand();
            cm.CommandType = CommandType.StoredProcedure;
            cm.CommandText = "UpdateCourse";
            cm.Parameters.AddWithValue("@Id", id);
            cm.Parameters.AddWithValue("@AvailableInRegionalCampus", availableInRegionalCampus);
            cm.ExecuteNonQuery();

            cn.Close();
        }

        public static Course Get(int id)
        {
            SqlConnection cn = new SqlConnection(Database.ConnectionString);
            cn.Open();

            SqlCommand cm = cn.CreateCommand();
            cm.CommandType = CommandType.StoredProcedure;
            cm.CommandText = "GetCourse";
            cm.Parameters.AddWithValue("@Id", id);
            SqlDataReader dr = cm.ExecuteReader();

            dr.Read();

            Course course = new Course();
            course.Id = Convert.ToInt32(dr["Course_ID"]);
            course.Prefix = dr["Course_Prefix"].ToString();
            course.Number = dr["Course_Number"].ToString();
            course.Section = dr["Course_Section"].ToString();
            course.Title = dr["Title"].ToString();
            course.Department = dr["Course_Department"].ToString();
            course.AvailableInRegionalCampus = Convert.ToBoolean(dr["availableInRegionalCampus"]);

            cn.Close();

            return course;
        }

        public static List<Course> List(bool? availableInRegionalCampus = null)
        {
            List<Course> courseList = new List<Course>();

            SqlConnection cn = new SqlConnection(Database.ConnectionString);
            cn.Open();

            SqlCommand cm = cn.CreateCommand();
            cm.CommandType = CommandType.StoredProcedure;
            cm.CommandText = "GetCourseList";
            cm.Parameters.AddWithValue("@AvailableInRegionalCampus", availableInRegionalCampus);
            SqlDataReader dr = cm.ExecuteReader();

            while (dr.Read())
            {
                Course course = new Course();
                course.Id = Convert.ToInt32(dr["Course_ID"]);
                course.Prefix = dr["Course_Prefix"].ToString();
                course.Number = dr["Course_Number"].ToString();
                course.Section = dr["Course_Section"].ToString();
                course.Title = dr["Title"].ToString();
                course.Department = dr["Course_Department"].ToString();
                course.AvailableInRegionalCampus = Convert.ToBoolean(dr["availableInRegionalCampus"]);

                courseList.Add(course);
            }

            cn.Close();

            return courseList;
        }
    }
}

