using Autofac;
using Autofac.Extensions.DependencyInjection;
using Catalog.API.Helper;
using Catalog.API.Installers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;

namespace Catalog.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.Seq("http://localhost:5341") // <- Added
            .CreateLogger();

            services.AddSingleton(Log.Logger);

            services.AddControllers();
            services.AddCustomDbContext(Configuration)
                .AddIntegrationServices(Configuration)
                .AddEventBus(Configuration)
                .AddSwagger(Configuration);

        }
        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new AutofacModule());
            //...
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger()
                 .UseSwaggerUI(c =>
                 {
                     c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog.API");
                 });
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}