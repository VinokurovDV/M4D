using Microsoft.AspNetCore.Builder;

namespace API
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        }
        public Startup()
        {
            var builder = new ConfigurationBuilder()
                .AddXmlFile("Web.config");
            AppConfiguration = builder.Build();
        }
        public IConfiguration AppConfiguration { get; set; }
    }
}
