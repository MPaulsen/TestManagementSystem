using System;
using System.Collections.Generic;
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
                //Session["User"] = prof.Username;
                Session["User"] = "bdurham";
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
                    query += "OR Course_ID = '" + courseIDs[i] +"' ";
                }
            }

            query += ")";

            int enrollment = Convert.ToInt32(Models.Database.ScalarString(query));

            //Query for enrollment first into string variable Enrollment

            query = "INSERT INTO tlTests VALUES('" + tcvm.test.Name + "', '1', '" + tcvm.test.StartDate + "', '" + tcvm.test.EndDate + "', '1', NULL, NULL, '" + tcvm.test.Notes.Replace("'", "''") + "', '0', '" + enrollment + "', '', '1', '" + DateTime.Now + "', '0', NULL, '" + tcvm.test.Length + @"', '0', '0')
                     SELECT SCOPE_IDENTITY()";
            //Use that OUTPUT thing to get testID back after insert?

            //User can alter dates despite being blacked out, check them and be ready to clear model state and return with model error.
            //Some kind of confirmation page?
            return RedirectToAction("NewTest");
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