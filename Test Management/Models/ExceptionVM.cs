using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net.Mail;

namespace Test_Management.Models
{
    public class ExceptionVM
    {
        public string NID { get; set; }
        public string TID { get; set; }
        public DateTime Open { get; set; }
        public DateTime Close { get; set; }
        public List<Models.Course> Courses { get; set; }
        public List<String> Students { get; set; }
        public List<Models.Test> Tests { get; set; }
        public string Prof { get; set; }
        public string exceptionTimeOpen { get; set; }
        public string exceptionTimeClose { get; set; }
        public List<DateTime> BlackedOutDates { get; set; }
        public string[] BlackedOutDatesArray { get; set; }
        public int DaysLeft { get; set; }
        

        public void Insert(string profUserName)
        {
            //Check that date/time isn't during a date exception and that the time submitted falls within the times the lab is open.
            DateTime openTime = Convert.ToDateTime(exceptionTimeOpen);
            DateTime closeTime = Convert.ToDateTime(exceptionTimeClose);
            Open = Open.AddHours(openTime.Hour);
            Open = Open.AddMinutes(openTime.Minute);

            Close = Close.AddHours(closeTime.Hour);
            Close = Close.AddMinutes(closeTime.Minute);

            Models.Database.nonQuery("INSERT INTO tlTests_Exceptions VALUES ('" + NID + "', '" + TID + "', '" + Open + "', '" + Close + "', 1, '" + DateTime.Now + "', 0)");

            //Email notifications.
            Professor emailProf = Professor.Get(profUserName);
            Test emailTest = Test.Get(Convert.ToInt16(TID));
            String StudentsName = Database.ScalarString("SELECT First_Name + ' ' + Last_Name FROM tlStudents WHERE NID = '" + NID + "'");

            String ProfessorName = emailProf.FirstName + " " + emailProf.LastName;
            String ProfessorEmail = emailProf.Email;

            Email(ProfessorEmail, "Text Exception Confirmation", "This is a receipt for your submission of a test exception for " + emailTest.Name + " on " + Open + " and ending on " + Close + " for " + StudentsName + "(NID: " + NID + "). This submission has been added to the Testing Centers list and is now active for the student to use. Please remember to activate the exam in web courses so that the student may take the exam!");
            Email("testinglab@bus.ucf.edu", ProfessorName + " has submitted a Test Exception", "A test exception was created by " + ProfessorName + " for the test '" + emailTest.Name + "' starting at " + Open + " and ending " + Close + " for " + StudentsName + "(NID: " + NID + ").");
            Email("raw@ucf.edu", ProfessorName + " has submitted a Test Exception", "A test exception was created by " + ProfessorName + " for the test '" + emailTest.Name + "' starting at " + Open + " and ending " + Close + " for " + StudentsName + "(NID: " + NID + ").");
            
        }

        protected void Email(String To, String Subject, String Body)
        {
            SmtpClient smptC = new SmtpClient("hermes.bus.ucf.edu");
            MailMessage mm = new MailMessage("donotreply@bus.ucf.edu", To, Subject, Body);

            mm.IsBodyHtml = true;

            smptC.Send(mm);
        }


        public SelectList _Tests()
        {
            //Foreach Test in Tests, if NID is in a course that is tied to the test, add that test as a select list item.
            List<SelectListItem> temp = new List<SelectListItem>();
            SqlConnection con = new SqlConnection(Database.ConnectionString);
            string query = @"SELECT T.Test_ID, T.Test_Name
                             FROM tlStudents_Courses SC, tlProfessors_Courses PC, tlProfessors P, tlTests_Courses TC, tlTests T
                             WHERE SC.Course_ID = PC.Course_ID AND PC.Professor_ID = P.Professor_ID AND SC.Term = PC.Term 
                             AND SC.Course_ID = TC.Course_ID AND TC.Test_ID = T.Test_ID AND P.CBAUserName = '" + this.Prof + "' AND NID = '" + this.NID + @"'
                             AND SC.Term = (SELECT Value FROM tlSettings WHERE Property = 'TestSubmissionTerm')";
            SqlCommand cmd = new SqlCommand(query, con);
            con.Open();
            SqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                string testID = rdr[0].ToString();
                string name = rdr[1].ToString();
                temp.Add(new SelectListItem() { Text = name, Value = testID });
            }
            con.Close();
            return new SelectList(temp, "Value", "Text");
        }

        public bool VerifyNID()
        {
            int nidExists = Convert.ToInt32(Database.ScalarString(@"
                            SELECT COUNT(NID)
                            FROM tlStudents_Courses SC, tlProfessors_Courses PC, tlProfessors P
                            WHERE SC.Course_ID = PC.Course_ID AND PC.Professor_ID = P.Professor_ID AND SC.Term = PC.Term 
                                AND P.CBAUserName = '" + this.Prof + "' AND NID = '" + this.NID + @"' 
                                AND SC.Term = (SELECT Value FROM tlSettings WHERE Property = 'TestSubmissionTerm')"));
            if (nidExists > 0)
                return true;
            else
                return false;
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


    }
}