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
        public string testDateOpenEdit { get; set; }
        public string testTimeOpen { get; set; }
        public DateTime testDateClose { get; set; }
        public string testDateCloseEdit { get; set; }
        public string testTimeClose { get; set; }
        public List<DateTime> BlackedOutDates { get; set; }
        public string[] BlackedOutDatesArray { get; set; }
        public List<DateTime> FullDates { get; set; }
        public string[] FullDatesArray { get; set; }
        public int DaysLeft { get; set; }
        public List<Material> materialList { get; set; }
        public Course SelectedCourse { get; set; }
        public string SelectedCourseName { get; set; }
        public string SelectedTestID { get; set; }

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
            DaysLeft = Convert.ToInt32(Database.ScalarString("SELECT DATEDIFF(DAY, GETDATE(), CONVERT(date, (SELECT Value FROM [dbo].[tlSettings] WHERE Property = 'FinalsClose')))"));

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
            


            //NOTE: Not accounting for Finals Week Logic yet?

            int CourseEnrollment = Convert.ToInt32(Database.ScalarString(@"
            SELECT COUNT(NID)
            FROM dbo.tlStudents_Courses
            WHERE Term = (SELECT Value FROM tlSettings WHERE Property = 'TestSubmissionTerm') AND Enrollment_Status LIKE 'Enrolled%'
            AND Course_ID IN 
            (
	            SELECT C.Course_ID
	            FROM dbo.tlCourses C, dbo.tlProfessors_Courses PC
	            WHERE PC.Term = (SELECT Value FROM tlSettings WHERE Property = 'TestSubmissionTerm') AND C.Course_ID = PC.Course_ID 
	            AND Course_Prefix = '" + courses[0].Prefix + "' AND Course_Number = '" + courses[0].Number + "' AND PC.Professor_ID = '" + prof.Id + @"'
            )"
            ));

            query = @"
            SELECT CONVERT(date, Close_Date_Time), 
                CASE WHEN DATENAME(dw,CONVERT(date, Close_Date_Time)) = 'Friday' OR DATENAME(dw,CONVERT(date, Close_Date_Time)) = 'Saturday' 
	            THEN (SELECT Value FROM tlSettings WHERE Property = 'WeekendCap') - SUM(Students_Enrolled) ELSE (SELECT Value FROM tlSettings WHERE Property = 'WeekdayCap') - SUM(Students_Enrolled)
                END AS 'OpenCount'
            FROM dbo.tlTests
            WHERE Term = (SELECT Value FROM tlSettings WHERE Property = 'TestSubmissionTerm')
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

        public SelectList _Courses()
        {
            List<string> courseNames = new List<string>();
            List<SelectListItem> temp = new List<SelectListItem>();
            temp.Add(new SelectListItem() { Text = "Select Course", Value = "default" });
            foreach (Course course in courses)
            {
                if (!courseNames.Contains(course.Name))
                {
                    courseNames.Add(course.Name);
                    temp.Add(new SelectListItem() { Text = course.Name, Value = course.Prefix + " " + course.Number});
                }
            }

            return new SelectList(temp, "Value", "Text");
        }

        public SelectList _Tests()
        {
            List<SelectListItem> temp = new List<SelectListItem>();
            temp.Add(new SelectListItem() { Text = "Select Test", Value = "default" });
            string prefix = SelectedCourseName.Split(' ')[0];
            string number = SelectedCourseName.Split(' ')[1];

            string query = @"SELECT DISTINCT T.Test_ID, Test_Name
                             FROM tlTests T, tlTests_Courses TC, tlProfessors_Courses PC, tlProfessors P, tlCourses C
                             WHERE T.Test_ID = TC.Test_ID AND TC.Course_ID = PC.Course_ID AND PC.Professor_ID = P.Professor_ID
	                            AND P.CBAUserName = '" + prof.Username + @"' AND T.Term = (SELECT Value FROM tlSettings WHERE Property = 'TestSubmissionTerm')
	                            AND T.Term = PC.Term AND C.Course_Prefix = '" + prefix + "' AND C.Course_Number = '" + number + "' AND PC.Course_ID = C.Course_ID";

            SqlConnection con = new SqlConnection(Database.ConnectionString);
            SqlCommand cmd = new SqlCommand(query, con);
            con.Open();
            SqlDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                temp.Add(new SelectListItem() { Text = rdr[1].ToString(), Value = rdr[0].ToString() });
            }
            con.Close();



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
                            WHERE Term = (SELECT Value FROM dbo.tlSettings WHERE Property = 'TestSubmissionTerm') AND Professor_ID = '" + prof.Id + "'";

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
                             WHERE Term = (SELECT Value FROM dbo.tlSettings WHERE Property = 'TestSubmissionTerm') AND Professor_ID = '" + prof.Id.ToString() + @"'
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

        public void getSectionsByTest(string testID, int profID)
        {

            List<Course> tmpCourses = new List<Course>();

            string query =
            @"SELECT Course_ID, ISNULL((SELECT Course_Number FROM tlTests_Courses WHERE Test_ID = '" + testID + @"' AND Course_ID = C.Course_ID), 0)
              FROM tlCourses C
              WHERE C.Course_Prefix + ' ' + C.Course_Number = 
	              (SELECT TOP 1 C2.Course_Prefix + ' ' + C2.Course_Number
	              FROM tlTests_Courses TC, tlCourses C2, tlProfessors_Courses PC
	              WHERE Term = (SELECT Value FROM dbo.tlSettings WHERE Property = 'TestSubmissionTerm') 
	              AND TC.Course_ID = C2.Course_ID AND C2.Course_ID = PC.Course_ID
	              AND TC.Test_ID = '" + testID + @"')
	              AND C.Course_ID IN 
	              (SELECT Course_ID 
	              FROM tlProfessors_Courses PC 
	              WHERE PC.Term = (SELECT Value FROM dbo.tlSettings WHERE Property = 'TestSubmissionTerm')
		              AND PC.Professor_ID = '" + profID + "')";

            SqlConnection con = new SqlConnection(Database.ConnectionString);
            SqlCommand cmd = new SqlCommand(query, con);
            con.Open();
            SqlDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                Course tmpCourse = Course.Get(Convert.ToInt32(rdr[0].ToString()));
                if (rdr[1].ToString() != "0")
                    tmpCourse.Selected = true;
                tmpCourses.Add(tmpCourse);

            }
            con.Close();
            courses = tmpCourses;

            string LengthNotes = Database.ScalarString("SELECT CONVERT(varchar(5), TestLength) + ' ' + CONVERT(varchar(max), Notes) FROM tlTests WHERE Test_ID = '" + testID + "'");
            this.test.Length = Convert.ToInt32(LengthNotes.Split(' ')[0]);
            this.test.Notes = LengthNotes.Substring(LengthNotes.IndexOf(" "));
            GetMaterialList();
            query = @"SELECT Material_ID
                      FROM tlTests_Test_Materials
                      WHERE Test_ID = '" + testID + "'";
            cmd = new SqlCommand(query, con);
            con.Open();
            rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                foreach (Material mat in materialList)
                {
                    if (rdr[0].ToString() == mat.MaterialID.ToString())
                        mat.Quantity = true;
                }

            }
            con.Close();

            testDateOpenEdit = testDateOpen.ToString("MM/dd/yyyy");
            testDateCloseEdit = testDateClose.ToString("MM/dd/yyyy");
            testTimeOpen = "11:59 PM";
            testTimeClose = "11:58";
            
        }

        public void FixDateTimesEdit()
        {
            DateTime openTime = Convert.ToDateTime(this.testTimeOpen);
            DateTime closeTime = Convert.ToDateTime(this.testTimeClose);
            this.test.StartDate = Convert.ToDateTime(this.testDateOpenEdit);
            this.test.StartDate = this.test.StartDate.AddHours(openTime.Hour);
            this.test.StartDate = this.test.StartDate.AddMinutes(openTime.Minute);
            this.test.EndDate = Convert.ToDateTime(this.testDateCloseEdit);
            this.test.EndDate = this.test.EndDate.AddHours(closeTime.Hour);
            this.test.EndDate = this.test.EndDate.AddMinutes(closeTime.Minute);

           


        }

    }
}