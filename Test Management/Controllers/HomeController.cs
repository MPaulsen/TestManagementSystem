using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Test_Management.Models;
using System.Net.Mail;

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
            DateTime eaOpen = new DateTime();
            DateTime eaClose = new DateTime();
            bool InEarlyAccess = false;

            SqlConnection con = new SqlConnection(Models.Database.ConnectionString);
            string query = @"SELECT StartDate, EndDate
                             FROM tlEarlyAccess EA, tlProfessors P
                             WHERE EA.Professor_ID = P.Professor_ID AND CBAUserName = '" + Session["User"].ToString() + "'";
            SqlCommand cmd = new SqlCommand(query, con);
            con.Open();
            SqlDataReader rdr = cmd.ExecuteReader();
            rdr.Read();
            try
            {
                eaOpen = DateTime.Parse(rdr[0].ToString());
                eaClose = DateTime.Parse(rdr[1].ToString());
                if (DateTime.Now < eaClose && DateTime.Now > eaOpen)
                    InEarlyAccess = true;
            }
            catch
            {
                InEarlyAccess = false;
            }
            con.Close();


            if ( (Open > DateTime.Now || DateTime.Now > Close) && !InEarlyAccess )
            {
                ModelState.Clear();
                ModelState.AddModelError("", "The Add Test function is currently closed. An email will be sent out during each semester break with the open dates for the test management system. If you have any questions, please contact the Testing Lab at testinglab@ucf.edu");

                return View("Home");
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

            tcvm.GetMaterialList();

            return View("TestInfo", tcvm);
        }

        public ActionResult SubmitTest(TestCourseVM tcvm)
        {
            if (!CheckCookie())
                return RedirectToAction("Index");

            //Most of this should be moved to the VM. Learning MVC best practices FTW.
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
                ModelState.AddModelError("Error", "Closing time not in range for selected closing day.");
                return RedirectToAction("LoadTestInfo", tcvm);
            }


            string title = "Title";
            string sSections = "";
            List<string> courseIDs = new List<string>();
            foreach (Course thing in tcvm.courses)
            {
                if (thing.Selected)
                {
                    Course courseDetails = Course.Get(thing.Id);
                    courseIDs.Add(thing.Id.ToString());
                    title = courseDetails.Name;
                    sSections += (courseDetails.Section + " ");
                }
            }

            string query;

            query = @"SELECT COUNT(NID)
                      FROM tlStudents_Courses
                      WHERE Enrollment_Status LIKE 'Enrol%' AND TERM = (SELECT VALUE FROM tlSettings WHERE Property='TestSubmissionTerm') AND (Course_ID = '" + courseIDs[0] + "' ";

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

            if (String.IsNullOrEmpty(tcvm.test.Notes))
                tcvm.test.Notes = "No Notes";
            
            query = "INSERT INTO tlTests VALUES('" + tcvm.test.Name + "', '1', '" + tcvm.test.StartDate + "', '" + tcvm.test.EndDate + "', '1', NULL, NULL, '" + tcvm.test.Notes.Replace("'", "''") + "', '0', (SELECT Value FROM tlSettings WHERE Property = 'TestSubmissionTerm'), 1, '" + enrollment + "', 0, '1', '" + DateTime.Now + "', '0', NULL, '" + tcvm.test.Length + @"', '0', '0')
                     SELECT SCOPE_IDENTITY()";

            string insertedID = Models.Database.ScalarString(query);

            foreach (string CID in courseIDs)
                Models.Database.nonQuery("INSERT INTO tlTests_Courses VALUES('" + insertedID + "', '" + CID + "')");

            string sMaterials = "";
            foreach (Models.Material mat in tcvm.materialList)
            {
                if (mat.Quantity)
                {
                    string matName = Database.ScalarString("SELECT Material_Name FROM tlTest_Materials WHERE Material_ID = '" + mat.MaterialID + "'");
                    sMaterials += (matName + " ");
                    Models.Database.nonQuery("INSERT INTO tlTests_Test_Materials VALUES ('" + insertedID + "', '" + mat.MaterialID + "', 1, '')");
                }
            }

            Professor emailProf = Professor.Get(Session["User"].ToString());
            Test emailTest = Test.Get(Convert.ToInt16(insertedID));

            String ProfessorName = emailProf.FirstName + " " + emailProf.LastName;
            String ProfessorEmail = emailProf.Email;



            Email(ProfessorEmail, "Your Test has been submitted", "This is a verification that you submitted a test to the CBA Online Test Submission System at " + DateTime.Now.ToString() + "<br/><br/>You will receive a notice when your exam is either accepted or denied.<br/><br/>Test Details:<br/>Title: " + emailTest.Name + "<br/>Opens: " + emailTest.StartDate.ToString() + "<br/>Closes: " + emailTest.EndDate.ToString() + "<br/>Length: " + tcvm.test.Length + " minutes<br/>Notes: " + tcvm.test.Notes + "<br/><br/>Course: " + title + "<br/>Sections: " + sSections + "<br/><br/>Materials: " + sMaterials);
            Email("testinglab@bus.ucf.edu", ProfessorName + " has submitted a test", "There is a new test for you to review, please open labman to view it!</br><br/>Test Details:<br/>Title: " + emailTest.Name + "<br/>Opens: " + emailTest.StartDate.ToString() + "<br/>Closes: " + emailTest.EndDate.ToString() + "<br/>Length: " + tcvm.test.Length + " minutes<br/>Notes: " + tcvm.test.Notes + "<br/><br/>Course: " + title + "<br/>Sections: " + sSections + "<br/><br/>Materials: " + sMaterials);
            Email("rctesting@ucf.edu", ProfessorName + " has submitted a test", "There is a new test for you to review, please open labman to view it!</br><br/>Test Details:<br/>Title: " + emailTest.Name + "<br/>Opens: " + emailTest.StartDate.ToString() + "<br/>Closes: " + emailTest.EndDate.ToString() + "<br/>Length: " + tcvm.test.Length + " minutes<br/>Notes: " + tcvm.test.Notes + "<br/><br/>Course: " + title + "<br/>Sections: " + sSections + "<br/><br/>Materials: " + sMaterials);
            

            return RedirectToAction("Confirmation");
        }

        protected void Email(String To, String Subject, String Body)
        {
            SmtpClient smptC = new SmtpClient("hermes.bus.ucf.edu");
            MailMessage mm = new MailMessage("donotreply@bus.ucf.edu", To, Subject, Body);

            mm.IsBodyHtml = true;

            smptC.Send(mm);
        }

        public ActionResult Confirmation()
        {
            return View();
        }

        public ActionResult EditTest()
        {
            if (!CheckCookie())
                return RedirectToAction("Index");
            TestCourseVM tcvm = new TestCourseVM();
            tcvm.prof = Models.Professor.Get(Session["User"].ToString());
            tcvm.getCourses();
            return View(tcvm);
        }

        [HttpPost]
        public ActionResult EditTest(TestCourseVM tcvm)
        {
            if (!CheckCookie())
                return RedirectToAction("Index");
            return RedirectToAction("EditTest2", tcvm);
        }

        public ActionResult EditTest2(TestCourseVM tcvm)
        {
            if (!CheckCookie())
                return RedirectToAction("Index");
            tcvm.prof = new Professor();
            tcvm.prof.Username = Session["User"].ToString();
            tcvm.test = new Test();
            return View(tcvm);
        }

        [HttpPost]
        public ActionResult TestSelect(TestCourseVM tcvm)
        {
            if (!CheckCookie())
                return RedirectToAction("Index");

            

            return RedirectToAction("EditTestInfo", tcvm);
        }

        public ActionResult EditTestInfo(TestCourseVM tcvm)
        {
            if (!CheckCookie())
                return RedirectToAction("Index");

            tcvm.prof = Professor.Get(Session["User"].ToString());

            try
            {
                tcvm.test = Test.Get(Convert.ToInt32(tcvm.SelectedTestID));
            }
            catch
            {
                ModelState.AddModelError(String.Empty, "Failed to retrieve test.");
                return View("EditTest");
            }

            tcvm.testDateOpen = tcvm.test.StartDate.Date;
            tcvm.testTimeOpen = tcvm.test.StartDate.ToString();
            tcvm.testDateClose = tcvm.test.EndDate.Date;
            tcvm.testTimeClose = tcvm.test.EndDate.ToString();

            tcvm.getSectionsByTest(tcvm.SelectedTestID, tcvm.prof.Id);
            tcvm.GetBlackedOutDates();


            return View(tcvm);
        }

        public ActionResult SubmitTestEdit(TestCourseVM tcvm)
        {
            if (!CheckCookie())
                return RedirectToAction("Index");


            tcvm.prof = Professor.Get(Session["User"].ToString());
            tcvm.FixDateTimesEdit();
            tcvm.GetBlackedOutDates();

            if (tcvm.BlackedOutDates.Contains(tcvm.testDateClose.Date) || tcvm.FullDates.Contains(tcvm.testDateClose.Date) || (tcvm.testDateClose.Date - DateTime.Now.Date).Days > tcvm.DaysLeft)
            {
                ModelState.AddModelError("Error", "Invalid value selected for closing date.");
                return RedirectToAction("EditTestInfo", tcvm);
            }
            if (tcvm.BlackedOutDates.Contains(tcvm.testDateOpen.Date))
            {
                ModelState.AddModelError(String.Empty, "Invalid value selected for open date.");
                return RedirectToAction("EditTestInfo", tcvm);
            }
            if (CheckTimes(tcvm.test.StartDate))
            {
                ModelState.AddModelError(String.Empty, "Opening time not in range for selected opening day.");
                return RedirectToAction("EditTestInfo", tcvm);
            }
            if (CheckTimes(tcvm.test.EndDate))
            {
                ModelState.AddModelError(String.Empty, "Closing time not in range for selected closing day.");
                return RedirectToAction("EditTestInfo", tcvm);
            }

            string title = "Title";
            string sSections = "";
            List<string> courseIDs = new List<string>();
            foreach (Course thing in tcvm.courses)
            {
                if (thing.Selected)
                {
                    Course courseDetails = Course.Get(thing.Id);
                    courseIDs.Add(thing.Id.ToString());
                    title = courseDetails.Name;
                    sSections += (courseDetails.Section + " ");
                }
            }

            string query;

            query = @"SELECT COUNT(NID)
                      FROM tlStudents_Courses
                      WHERE Enrollment_Status LIKE 'Enrol%' AND TERM = (SELECT VALUE FROM tlSettings WHERE Property='TestSubmissionTerm') AND (Course_ID = '" + courseIDs[0] + "' ";

            if (courseIDs.Count > 1)
            {
                //For each course after the first, add the string below with the course ids
                for (int i = 1; i < courseIDs.Count; i++)
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
                return RedirectToAction("EditTestInfo", tcvm);
            }

            if (String.IsNullOrEmpty(tcvm.test.Notes))
                tcvm.test.Notes = "No Notes";

            Database.nonQuery("DELETE FROM tlTests_Revisions WHERE Test_ID = '" + tcvm.test.Id + "'");
            Database.nonQuery("INSERT INTO tlTests_Revisions (Test_ID, Test_Name, Open_Date_Time, Close_Date_Time, Notes, TestLength) VALUES('" + tcvm.test.Id + "', '" + tcvm.test.Name + "', '" + tcvm.test.StartDate.ToString() + "', '" + tcvm.test.EndDate.ToString() + "', '" + tcvm.test.Notes.Replace("'", "''") + "', '" + tcvm.test.Length + "')");

            //Delete Old Sections
            Database.nonQuery("DELETE FROM tlTests_Courses WHERE Test_ID = '" + tcvm.test.Id + "'");
            //Connect new sections
            foreach (string id in courseIDs)
                Database.nonQuery("INSERT INTO tlTests_Courses VALUES ('" + tcvm.test.Id + "', '" + id + "')");

            //Delete Old Materials
            Database.nonQuery("DELETE FROM tlTests_Test_Materials WHERE Test_ID = '" + tcvm.test.Id + "'");

            string sMaterials = "";
            foreach (Models.Material mat in tcvm.materialList)
            {
                if (mat.Quantity)
                {
                    string matName = Database.ScalarString("SELECT Material_Name FROM tlTest_Materials WHERE Material_ID = '" + mat.MaterialID + "'");
                    sMaterials += (matName + " ");
                    Models.Database.nonQuery("INSERT INTO tlTests_Test_Materials VALUES ('" + tcvm.test.Id + "', '" + mat.MaterialID + "', 1, '')");
                }
            }

            Professor emailProf = Professor.Get(Session["User"].ToString());
            Test emailTest = tcvm.test;

            String ProfessorName = emailProf.FirstName + " " + emailProf.LastName;
            String ProfessorEmail = emailProf.Email;



            Email(ProfessorEmail, "Your edited Test has been submitted", "This is a verification that you submitted an edited test to the CBA Online Test Submission System at " + DateTime.Now.ToString() + "</br><br/>Test Details:<br/>Title: " + tcvm.test.Name + "<br/>Opens: " + tcvm.test.StartDate.ToString() + "<br/>Closes: " + tcvm.test.EndDate.ToString() + "<br/>Length: " + tcvm.test.Length + " minutes<br/>Notes: " + tcvm.test.Notes + "<br/><br/>Course: " + title + "<br/>Sections: " + sSections + "<br/><br/>Materials: " + sMaterials);
            Email("testinglab@bus.ucf.edu", ProfessorName + " has edited a test", "There is a revised test for you to review, please open labman to view it!</br><br/>Test Details:<br/>Title: " + tcvm.test.Name + "<br/>Opens: " + tcvm.test.StartDate.ToString() + "<br/>Closes: " + tcvm.test.EndDate.ToString() + "<br/>Length: " + tcvm.test.Length + " minutes<br/>Notes: " + tcvm.test.Notes + "<br/><br/>Course: " + title + "<br/>Sections: " + sSections + "<br/><br/>Materials: " + sMaterials);
            Email("rctesting@ucf.edu", ProfessorName + " has edited a test", "There is a revised test for you to review, please open labman to view it!</br><br/>Test Details:<br/>Title: " + tcvm.test.Name + "<br/>Opens: " + tcvm.test.StartDate.ToString() + "<br/>Closes: " + tcvm.test.EndDate.ToString() + "<br/>Length: " + tcvm.test.Length + " minutes<br/>Notes: " + tcvm.test.Notes + "<br/><br/>Course: " + title + "<br/>Sections: " + sSections + "<br/><br/>Materials: " + sMaterials);
        


            return View("Confirmation");
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

        public ActionResult AddTestException()
        {
            if (!CheckCookie())
                return RedirectToAction("Index");

            return View();
        }

        [HttpPost]
        public ActionResult AddTestException(ExceptionVM EVM)
        {
            if (!CheckCookie())
                return RedirectToAction("Index");

            return View();
        }

        [HttpPost]
        public ActionResult ExceptionInfo(ExceptionVM EVM)
        {
            if (!CheckCookie())
                return RedirectToAction("Index");
            EVM.Insert(Session["User"].ToString());
            return RedirectToAction("ExceptionConfirmation"); 
        }

        [HttpPost]
        public ActionResult ExceptionNID(ExceptionVM EVM)
        {
            if (!CheckCookie())
                return RedirectToAction("Index");

            EVM.Prof = Session["User"].ToString();

            //Verify NID exists and is related to logged in professor by a class somehow.
            if (!EVM.VerifyNID())
            {
                ModelState.Clear();
                ModelState.AddModelError(String.Empty, "Problem processing NID. Verify that the entered NID is a student currently enrolled in one of your courses.");
                return View("AddTestException");
            }

            EVM.GetBlackedOutDates();
            return View("ExceptionInfo", EVM);
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
            string query = @"SELECT Property, Value FROM tlSettings WHERE Property = 'FinalsOpen' OR Property = 'FinalsClose' OR Property = 'TestSubmission:StartTimeWeekday' 
                             OR Property = 'TestSubmission:StartTimeFriday' OR Property = 'TestSubmission:StartTimeSaturday' OR Property = 'TestSubmission:EndTimeWeekday' 
                             OR Property = 'TestSubmission:EndTimeFriday' OR Property = 'TestSubmission:EndTimeSaturday'
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
                if (rdr[0].ToString() == "TestSubmission:StartTimeWeekday")
                    startTime = Convert.ToDateTime(rdr[1].ToString());
                if (rdr[0].ToString() == "TestSubmission:StartTimeFriday")
                    startTimeFriday = Convert.ToDateTime(rdr[1].ToString());
                if (rdr[0].ToString() == "TestSubmission:StartTimeSaturday")
                    startTimeSaturday = Convert.ToDateTime(rdr[1].ToString());
                if (rdr[0].ToString() == "TestSubmission:EndTimeWeekday")
                    endTime = Convert.ToDateTime(rdr[1].ToString());
                if (rdr[0].ToString() == "TestSubmission:EndTimeFriday")
                    endTimeFriday = Convert.ToDateTime(rdr[1].ToString());
                if (rdr[0].ToString() == "TestSubmission:EndTimeSaturday")
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
                if (date.Hour < startTime.Hour || date.Hour > endTime.Hour)
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
            
            string inDB = Database.ScalarString(@"SELECT COUNT(*)
                                    FROM tlProfessors
                                    WHERE CBAUserName = '" + prof.Username + "'");

            try
            {
                if ( !(Convert.ToInt32(inDB) > 0) )
                    return false;
            }
            catch 
            {
                return false;
            }

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