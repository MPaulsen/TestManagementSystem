using System.Web;
using System.Web.Optimization;

namespace Test_Management
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            // Scripts Js
            bundles.Add(new ScriptBundle("~/Js/Scripts").Include(
                "~/Js/bootstrap.js",
                "~/Js/bootbox.js",
                "~/Js/jquery-{version}.js",
                "~/Js/jquery-ui.js",
                "~/Js/jquery.unobtrusive*",
                "~/Js/jquery.validate*",
                "~/Js/jquery.inputmask.js",
                "~/Js/jquery.inputmask.*",
                "~/Js/jquery.timepicker.js"
                ));

            // Custom Js
            bundles.Add(new ScriptBundle("~/Js/Custom").Include(
                "~/Js/main.js"
                ));

            // Admin Js
            bundles.Add(new ScriptBundle("~/Js/Admin").Include(
                "~/Js/jquery.dataTables.js",
                "~/Js/dataTables.*",
                "~/Js/jquery.notify.js",
                "~/Js/jquery.combobox.js"
                ));

            // Main Css
            bundles.Add(new StyleBundle("~/Css/Main").Include(
                "~/Css/reset.css",
                "~/Css/bootstrap.css",
                "~/Css/font-awesome.css",
                "~/Css/jquery-ui.css",
                "~/Css/jquery.timepicker.css",
                "~/Css/main.css"
                ));

            // Admin Css
            bundles.Add(new StyleBundle("~/Css/Admin").Include(
                "~/Css/dataTables.*",
                "~/Css/admin.css",
                "~/Css/jquery-ui.notify.css"
                ));

            // Print Css
            bundles.Add(new StyleBundle("~/Css/Print").Include(
                "~/Css/print.css"
                ));
        }
    }
}
