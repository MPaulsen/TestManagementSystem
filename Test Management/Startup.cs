using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Test_Management.Startup))]
namespace Test_Management
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            
        }
    }
}
