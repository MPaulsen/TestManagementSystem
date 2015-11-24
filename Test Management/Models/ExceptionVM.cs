using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Test_Management.Models
{
    public class ExceptionVM
    {
        public string NID { get; set; }
        public string TID { get; set; }
        public DateTime Open { get; set; }
        public DateTime Close { get; set; }
        

        public void Insert()
        {
            //Check that date/time isn't during a date exception and that the time submitted falls within the times the lab is open.

            Models.Database.nonQuery("INSERT INTO tlTests_Exceptions VALUES ('" + NID + "', '" + TID + "', '" + Open + "', '" + Close + "', 1, '" + DateTime.Now + "', 0)");

            //Email notifications.
        }
    }
}