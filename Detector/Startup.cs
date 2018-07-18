using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Detector.Startup))]
namespace Detector
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
