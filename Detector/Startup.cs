using Microsoft.Owin;
using Owin;
using Hangfire;

[assembly: OwinStartupAttribute(typeof(Detector.Startup))]
namespace Detector
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseHangfireDashboard();
            ConfigureAuth(app);
        }
    }
}
