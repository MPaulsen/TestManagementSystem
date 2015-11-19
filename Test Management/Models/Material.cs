using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Test_Management.Models
{
    public class Material
    {
        public int MaterialID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Quantity { get; set; }

        public Material()
        {
            Quantity = false;
        }
    }
}