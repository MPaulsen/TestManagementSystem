using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Test_Management.Models;

namespace Test_Management.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            Session.Clear();
            return View();
        }

        [HttpPost]
        public ActionResult Index(User prof)
        {
            if (Authenticate(prof))
                Session["User"] = prof.Username;
                //Session["User"] = "bdurham";
            else
            {
                ModelState.AddModelError(String.Empty, "Incorrect Username/Password");
                return View();
            }

            return RedirectToAction("Home");
        }

        public ActionResult Home()
        {
            if (!CheckCookie())
                return RedirectToAction("Index");

            return View();
        }

        [HttpPost]
        public ActionResult Home(User prof)
        {
            if (!CheckCookie())
                return RedirectToAction("Index");

            return View();
        }

        public ActionResult NewTest()
        {
            if (!CheckCookie())
                return RedirectToAction("Index");

            //Check Open/Close Dates
            DateTime Open = DateTime.Parse(Database.ScalarString("SELECT VALUE FROM tlSettings WHERE Property = 'TestSubmissionOpenDate'"));
            DateTime Close = DateTime.Parse(Database.ScalarString("SELECT VALUE FROM tlSettings WHERE Property = 'TestSubmissionClosedDate'"));

            if (false)
            //if (Open > DateTime.Now || DateTime.Now > Close)
            {
                ModelState.Clear();
                ModelState.AddModelError("", "The Add Test function is currently closed. An email will be sent out during each semester break with the open dates for the test management system. If you have any questions, please contact the Testing Lab at testinglab@ucf.edu");

                return View();
            }

            TestCourseVM TVM = new TestCourseVM();
            TVM.prof = Models.Professor.Get(Session["User"].ToString());
            TVM.test = new Test();
            TVM.getCourses();
            TVM.getTitles();

            

            return View( TVM );
        }


        public ActionResult LoadTestInfo(TestCourseVM tcvm)
        {
            if (!CheckCookie())
                return RedirectToAction("Index");
            if (String.IsNullOrWhiteSpace(tcvm.title))
                return RedirectToAction("NewTest");

            tcvm.prof = Models.Professor.Get(Session["User"].ToString());
            tcvm.getCourses();
            tcvm.getTitles();
            tcvm.getSections(tcvm.title);

            tcvm.GetBlackedOutDates();
            //Possibly have to run funtion that does calendar date blocking.  Call this function on section check box changes?

            tcvm.GetMaterialList();

            return View("TestInfo", tcvm);
            //return PartialView("_TestInfo", TVM);
        }

        public ActionResult SubmitTest(TestCourseVM tcvm)
        {
            DateTime openTime = Convert.ToDateTime(tcvm.testTimeOpen);
            DateTime closeTime = Convert.ToDateTime(tcvm.testTimeClose);
            tcvm.test.StartDate = tcvm.testDateOpen;
            tcvm.test.StartDate = tcvm.test.StartDate.AddHours(openTime.Hour);
            tcvm.test.StartDate = tcvm.test.StartDate.AddMinutes(openTime.Minute);
            tcvm.test.EndDate = tcvm.testDateClose;
            tcvm.test.EndDate = tcvm.test.EndDate.AddHours(closeTime.Hour);
            tcvm.test.EndDate = tcvm.test.EndDate.AddMinutes(closeTime.Minute);
            if (tcvm.testName != "Final")
                tcvm.test.Name = tcvm.testName + " " + tcvm.testNumber.ToString();
            else
                tcvm.test.Name = tcvm.testName;
            
            DateTime dtSubmitted = DateTime.Now;

            tcvm.GetBlackedOutDates();

            if (tcvm.BlackedOutDates.Contains(tcvm.testDateClose.Date) || tcvm.FullDates.Contains(tcvm.testDateClose.Date) || (tcvm.testDateClose.Date - DateTime.Now.Date).Days > tcvm.DaysLeft)
            {
                ModelState.AddModelError("Error", "Invalid value selected for closing date.");
                return RedirectToAction("LoadTestInfo", tcvm);
            }
            if ( tcvm.BlackedOutDates.Contains(tcvm.testDateOpen.Date) )
            {
                ModelState.AddModelError(String.Empty, "Invalid value selected for open date.");
                return RedirectToAction("LoadTestInfo", tcvm);
            }
            if (CheckTimes(tcvm.test.StartDate))
            {
                ModelState.AddModelError(String.Empty, "Opening time not in range for selected opening day.");
                return RedirectToAction("LoadTestInfo", tcvm);
            }
            if (CheckTimes(tcvm.test.EndDate))
            {
                ModelState.AddModelError(String.Empty, "Closing time not in range for selected closing day.");
                return RedirectToAction("LoadTestInfo", tcvm);
            }

            

            List<string> courseIDs = new List<string>();
            foreach (Course thing in tcvm.courses)
            {
                if (thing.Selected)
                    courseIDs.Add(thing.Id.ToString());
            }

            string query;

            query = @"SELECT COUNT(NID)
                      FROM tlStudents_Courses
                      WHERE Enrollment_Status LIKE 'Enrol%' AND TERM = (SELECT VALUE FROM tlSettings WHERE Property='CurrentTerm') AND (Course_ID = '" + courseIDs[0] + "' ";

            if (courseIDs.Count > 1)
            {
                //For each course after the first, add the string below with the course ids
                for (int i = 1; i < courseIDs.Count; i++ )
                {
                    query += "OR Course_ID = '" + courseIDs[i] + "' ";
                }
            }

            query += ")";

            string enrollment = Models.Database.ScalarString(query);

            //Query date's enrollment vs this enrollment (unpublished/verified test enrollment counts) and make sure the date population will still be kosher
            if (CheckEnrollment(enrollment, tcvm.test.EndDate))
            {
                ModelState.AddModelError(String.Empty, "Selected closing date is at capacity.  Please choose another closing date.");
                return RedirectToAction("LoadTestInfo", tcvm);
            }

            //Query for enrollment first into string variable Enrollment
            query = "INSERT INTO tlTests VALUES('" + tcvm.test.Name + "', '1', '" + tcvm.test.StartDate + "', '" + tcvm.test.EndDate + "', '1', NULL, NULL, '" + tcvm.test.Notes.Replace("'", "''") + "', '0', (SELECT Value FROM tlSettings WHERE Property = 'CurrentTerm'), 1, '" + enrollment + "', 0, '1', '" + DateTime.Now + "', '0', NULL, '" + tcvm.test.Length + @"', '0', '0')
                     SELECT SCOPE_IDENTITY()";

            string insertedID = Models.Database.ScalarString(query);

            foreach (string CID in courseIDs)
                Models.Database.nonQuery("INSERT INTO tlTests_Courses VALUES('" + insertedID + "', '" + CID + "')");

            foreach (Models.Material mat in tcvm.materialList)
                if (mat.Quantity)
                    Models.Database.nonQuery("INSERT INTO tlTests_Test_Materials VALUES ('" + insertedID + "', '" + mat.MaterialID + "', 1, '')");

            //Email Notifications Go Here

            return RedirectToAction("Confirmation");
        }

        public ActionResult Confirmation()
        {
            return View();
        }

        public ActionResult UpdateInfo()
        {
            if (!CheckCookie())
                return RedirectToAction("Index");

            string number = Models.Database.ScalarString("SELECT ISNULL(Emergency_Phone, '') FROM tlProfessors WHERE CBAUserName = '" + Session["User"].ToString() + "'");
            return View("UpdateInfo", null, number);
        }

        [HttpPost]
        public ActionResult UpdateInfo(string number)
        {
            if (!CheckCookie())
                return RedirectToAction("Index");

            try{
                Models.Database.nonQuery("UPDATE tlProfessors SET Emergency_Phone = '" + number + "' WHERE CBAUserName = '" + Session["User"].ToString() + "'");
            }
            catch{;}
            return RedirectToAction("Home");
        }

        private bool CheckEnrollment(string strRoll, DateTime date)
        {
            int enrollment = Convert.ToInt32(strRoll);

            int dateEnrollment = Convert.ToInt32(Models.Database.ScalarString(@"
                SELECT COUNT(NID) AS 'TotalEnrollment'
                FROM tlTests T, tlTests_Courses TC, tlStudents_Courses SC
                WHERE T.Test_ID = TC.Test_ID AND TC.Course_ID = SC.Course_ID AND SC.Enrollment_Status LIKE 'Enrol%'
                  AND CONVERT(date, Close_Date_Time) = '" + date.Date + "' AND T.Term = SC.Term"));

            int totalEnrollment = enrollment + dateEnrollment;

            DateTime finalsStart = Convert.ToDateTime(Models.Database.ScalarString("SELECT Value FROM tlSettings WHERE Property = 'FinalsOpen'"));
            DateTime finalsEnd = Convert.ToDateTime(Models.Database.ScalarString("SELECT Value FROM tlSettings WHERE Property = 'FinalsClose'"));
            bool finals = (date > finalsStart && date < finalsEnd);
            int enrollmentCap = 0;

            if (finals)
                enrollmentCap = Convert.ToInt32(Models.Database.ScalarString("SELECT Value FROM tlSettings WHERE Property = 'FinalsCap'"));
            else if (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday)
                enrollmentCap = Convert.ToInt32(Models.Database.ScalarString("SELECT Value FROM tlSettings WHERE Property = 'WeekendCap'"));
            else
                enrollmentCap = Convert.ToInt32(Models.Database.ScalarString("SELECT Value FROM tlSettings WHERE Property = 'WeekdayCap'"));

            if (totalEnrollment > enrollmentCap)
                return true;

            return false;
        }

        //Returns true if there's a problem with the times selected for a given date.
        private bool CheckTimes(DateTime date)
        {
            DateTime finalsStart = Convert.ToDateTime("01/01/1900");
            DateTime finalsEnd = Convert.ToDateTime("01/01/1900");
            DateTime startTime = Convert.ToDateTime("01/01/1900");
            DateTime startTimeFriday = Convert.ToDateTime("01/01/1900");
            DateTime startTimeSaturday = Convert.ToDateTime("01/01/1900");
            DateTime endTime = Convert.ToDateTime("01/01/1900");
            DateTime endTimeFriday = Convert.ToDateTime("01/01/1900");
            DateTime endTimeSaturday = Convert.ToDateTime("01/01/1900");
            SqlConnection con = new SqlConnection(Models.Database.ConnectionString);
            string query = @"SELECT Property, Value FROM tlSettings WHERE Property = 'FinalsOpen' OR Property = 'FinalsClose' OR Property = 'TestCalendar:StartTime' 
                             OR Property = 'TestCalendar:StartTimeFriday' OR Property = 'TestCalendar:StartTimeSaturday' OR Property = 'TestCalendar:EndTime' 
                             OR Property = 'TestCalendar:EndTimeFriday' OR Property = 'TestCalendar:EndTimeSaturday'
                             ORDER BY Property ASC";
            SqlCommand cmd = new SqlCommand(query, con);
            con.Open();
            SqlDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                if (rdr[0].ToString() == "FinalsOpen")
                    finalsStart = Convert.ToDateTime(rdr[1].ToString());
                if (rdr[0].ToString() == "FinalsClose")
                    finalsEnd = Convert.ToDateTime(rdr[1].ToString());
                if (rdr[0].ToString() == "TestCalendar:StartTime")
                    startTime = Convert.ToDateTime(rdr[1].ToString());
                if (rdr[0].ToString() == "TestCalendar:StartTimeFriday")
                    startTimeFriday = Convert.ToDateTime(rdr[1].ToString());
                if (rdr[0].ToString() == "TestCalendar:StartTimeSaturday")
                    startTimeSaturday = Convert.ToDateTime(rdr[1].ToString());
                if (rdr[0].ToString() == "TestCalendar:EndTime")
                    endTime = Convert.ToDateTime(rdr[1].ToString());
                if (rdr[0].ToString() == "TestCalendar:EndTimeFriday")
                    endTimeFriday = Convert.ToDateTime(rdr[1].ToString());
                if (rdr[0].ToString() == "TestCalendar:EndTimeSaturday")
                    endTimeSaturday = Convert.ToDateTime(rdr[1].ToString());

            }
            con.Close();

            bool finals = false;
            if (date > finalsStart && date < finalsEnd)
                finals = true;
            
            if (date.DayOfWeek == DayOfWeek.Friday && !finals)
            {
                if (date.Hour < startTimeFriday.Hour || (date.Hour == endTimeFriday.Hour && date.Minute > endTimeFriday.Minute) || date.Hour > endTimeFriday.Hour)
                    return true;
            }
            else if (date.DayOfWeek == DayOfWeek.Saturday && !finals)
            {
                if (date.Hour < startTimeSaturday.Hour || (date.Hour == startTimeSaturday.Hour && date.Minute < startTimeSaturday.Minute) || date.Hour > endTimeSaturday.Hour || (date.Hour == endTimeSaturday.Hour && date.Minute > endTimeSaturday.Minute) )
                    return true;
            }
            else if (date.DayOfWeek == DayOfWeek.Sunday)
                return true;
            else
            {
                if (date.Hour < startTime.Hour)
                    return true;
            }

            //No errors found
            return false;
        }

        public bool Authenticate(User prof)
        {
            //Check for prof.Username to be in dbo.tlProfessors
            //Otherwise return false because we need to be able
            //to tie the username to their classes and what not.
            


            if (!this.ModelState.IsValid)
                return false;

            if (Membership.ValidateUser(prof.Username, prof.Password))
            {
                FormsAuthentication.SetAuthCookie(prof.Username, true);
                return true;
            }

            return false;

        }

        public bool CheckCookie()
        {
            try
            {
                HttpCookie cookie = System.Web.HttpContext.Current.Request.Cookies.Get(FormsAuthentication.FormsCookieName);

                FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(cookie.Value);
                String userName = ticket.Name;
                if (String.IsNullOrWhiteSpace(userName) || Session["User"] == null)
                    return false;
                return true;
            }

            catch
            {
                return false;
            }
        }

    }
}