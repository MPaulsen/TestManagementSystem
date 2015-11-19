using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace Test_Management.Models
{
    public class TestCourseVM
    {
        public Professor prof { get; set; }
        public Test test { get; set; }
        public List<Course> courses { get; set; }
        public List<SelectListItem> titles { get; set; }
        public string title { get; set; }
        public string testName { get; set; }
        public int testNumber { get; set; }
        public DateTime testDateOpen { get; set; }
        public string testTimeOpen { get; set; }
        public DateTime testDateClose { get; set; }
        public string testTimeClose { get; set; }
        public List<DateTime> BlackedOutDates { get; set; }
        public string[] BlackedOutDatesArray { get; set; }
        public List<DateTime> FullDates { get; set; }
        public string[] FullDatesArray { get; set; }
        public int DaysLeft { get; set; }
        public List<Material> materialList { get; set; }

        public SelectList _titles()
        {
            return new SelectList(titles, "Value", "Text");
        }

        public void GetMaterialList()
        {
            materialList = new List<Material>();
            SqlConnection con = new SqlConnection(Database.ConnectionString);
            string query = @"SELECT Material_ID, Material_Name, Material_Description FROM dbo.tlTest_Materials";
            SqlCommand cmd = new SqlCommand(query, con);
            con.Open();
            SqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                Material mat = new Material();
                mat.MaterialID = Convert.ToInt32(rdr[0].ToString());
                mat.Name = rdr[1].ToString();
                mat.Description = rdr[2].ToString();
                materialList.Add(mat);
            }

            con.Close();
        }

        public void GetBlackedOutDates()
        {
            List<DateTime> blackouts = new List<DateTime>();
            List<DateTime> fulls = new List<DateTime>();

            //NOTE: There should be different logic for summer maybe?  They need to make sure the FinalsClose property is the last day of the entire summer semester.  Double check this.
            DaysLeft = Convert.ToInt32(Database.ScalarString("SELECT DATEDIFF(DAY, GETDATE(), CONVERT(date, (SELECT Value FROM [cba_trc_01].[dbo].[tlSettings] WHERE Property = 'FinalsClose')))"));

            SqlConnection con = new SqlConnection(Database.ConnectionString);
            string query = @"SELECT [Date_Time_Start], [Date_Time_End]
                              FROM [dbo].[tlDateExceptions]
                              WHERE Date_Time_Start > GETDATE()";
            SqlCommand cmd = new SqlCommand(query, con);
            con.Open();
            SqlDataReader rdr = cmd.ExecuteReader();
            DateTime Open;
            DateTime Close;
            while (rdr.Read())
            {
                Open = Convert.ToDateTime(rdr[0].ToString()).Date;
                Close = Convert.ToDateTime(rdr[1].ToString()).Date;
                while (Open <= Close)
                {
                    blackouts.Add(Open);
                    Open = Open.AddDays(1);
                }
            }

            con.Close();

            BlackedOutDates = blackouts;

            SetBlackedOutDatesArray();
            
            //NOTE: Not accounting for Finals Week Logic yet.
            //NOTE: Still need to manipulate time boxes for weekends (if not finals week).

            int CourseEnrollment = Convert.ToInt32(Database.ScalarString(@"
            SELECT COUNT(NID)
            FROM dbo.tlStudents_Courses
            WHERE Term = (SELECT Value FROM tlSettings WHERE Property = 'CurrentTerm') AND Enrollment_Status LIKE 'Enrolled%'
            AND Course_ID IN 
            (
	            SELECT C.Course_ID
	            FROM dbo.tlCourses C, dbo.tlProfessors_Courses PC
	            WHERE PC.Term = (SELECT Value FROM tlSettings WHERE Property = 'CurrentTerm') AND C.Course_ID = PC.Course_ID 
	            AND Course_Prefix = '" + courses[0].Prefix + "' AND Course_Number = '" + courses[0].Number + "' AND PC.Professor_ID = '" + prof.Id + @"'
            )"
            ));

            query = @"
            SELECT CONVERT(date, Close_Date_Time), 
                CASE WHEN DATENAME(dw,CONVERT(date, Close_Date_Time)) = 'Friday' OR DATENAME(dw,CONVERT(date, Close_Date_Time)) = 'Saturday' 
	            THEN (SELECT Value FROM tlSettings WHERE Property = 'WeekendCap') - SUM(Students_Enrolled) ELSE (SELECT Value FROM tlSettings WHERE Property = 'WeekdayCap') - SUM(Students_Enrolled)
                END AS 'OpenCount'
            FROM dbo.tlTests
            WHERE Term = (SELECT Value FROM tlSettings WHERE Property = 'CurrentTerm')
            GROUP BY CONVERT(date, Close_Date_Time)";
            cmd = new SqlCommand(query, con);
            con.Open();
            rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                if (Convert.ToInt32(rdr[1].ToString()) < CourseEnrollment)
                    fulls.Add(Convert.ToDateTime(rdr[0].ToString()));
            }

            con.Close();


            FullDates = fulls;

            SetFullDatesArray();
        }

        public void SetBlackedOutDatesArray()
        {
            string[] aryBOD;
            aryBOD = new string[BlackedOutDates.Count];
            int i = 0;

            foreach (DateTime day in BlackedOutDates)
            {
                aryBOD[i] = day.ToString("yyyy-MM-dd");
                i++;
            }

            BlackedOutDatesArray = aryBOD;
        }

        public void SetFullDatesArray()
        {
            string[] aryBOD;
            aryBOD = new string[FullDates.Count];
            int i = 0;

            foreach (DateTime day in FullDates)
            {
                aryBOD[i] = day.ToString("yyyy-MM-dd");
                i++;
            }

            FullDatesArray = aryBOD;
        }


        public SelectList _Names()
        {

            List<SelectListItem> temp = new List<SelectListItem>();
            temp.Add(new SelectListItem() { Text = "Quiz", Value = "Quiz" } );
            temp.Add(new SelectListItem() { Text = "Exam", Value = "Exam" });
            temp.Add(new SelectListItem() { Text = "Midterm", Value = "Midterm" });
            temp.Add(new SelectListItem() { Text = "Final", Value = "Final" });
            temp.Add(new SelectListItem() { Text = "Makeup", Value = "Makeup" });

            return new SelectList(temp, "Value", "Text");
        }
        

        public void getTitles()
        {
            List<string> tmpTitles = new List<string>();
            titles = new List<SelectListItem>();
            foreach (Course course in courses)
            {
                if (!tmpTitles.Contains(course.Title + " (" + course.Prefix + " " + course.Number + ')'))
                    tmpTitles.Add(course.Title + " (" + course.Prefix + " " + course.Number + ')');
            }

            foreach (string title in tmpTitles)
            {
                titles.Add(new SelectListItem() { Text=title, Value=title.Split('(')[1].Replace(")", "") });
            }
        }

        public void getCourses()
        {
            List<Course> tmpCourses = new List<Course>();

            string query = @"SELECT Course_ID
                            FROM dbo.tlProfessors_Courses
                            WHERE Term = (SELECT Value FROM dbo.tlSettings WHERE Property = 'CurrentTerm') AND Professor_ID = '" + prof.Id + "'";

            SqlConnection con = new SqlConnection(Database.ConnectionString);
            SqlCommand cmd = new SqlCommand(query, con);
            con.Open();
            SqlDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                tmpCourses.Add(Course.Get(Convert.ToInt32(rdr[0].ToString())));
            }
            con.Close();
            courses = tmpCourses;
        }

        public void getSections(string value)
        {
            string prefix = value.Split(' ')[0];
            string number = value.Split(' ')[1];

            List<Course> tmpCourses = new List<Course>();

            string query = @"SELECT Course_ID
                             FROM dbo.tlProfessors_Courses
                             WHERE Term = (SELECT Value FROM dbo.tlSettings WHERE Property = 'CurrentTerm') AND Professor_ID = '" + prof.Id.ToString() + @"'
	                            AND Course_ID IN (SELECT Course_ID FROM dbo.tlCourses WHERE Course_Prefix = '" + prefix + "' AND Course_Number = '" + number + "')";

            SqlConnection con = new SqlConnection(Database.ConnectionString);
            SqlCommand cmd = new SqlCommand(query, con);
            con.Open();
            SqlDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                tmpCourses.Add(Course.Get(Convert.ToInt32(rdr[0].ToString())));
            }
            con.Close();
            courses = tmpCourses;
        }

    }
}