using Autofac;
using Basket.API.Controllers;
using Basket.API.Dependency;
using Basket.API.Filters;
using Basket.API.Helper;
using Basket.API.IntegrationEvents.EventHandling;
using Basket.API.IntegrationEvents.Events;
using EventBus.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Threading.Tasks;

namespace Basket.API
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
            services.AddControllers(options =>
            {
                options.Filters.Add(typeof(HttpGlobalExceptionFilter));
                options.Filters.Add(typeof(ValidateModelStateFilter));
            }) // Added for functional tests
                .AddApplicationPart(typeof(BasketController).Assembly)
                .AddNewtonsoftJson();

            Log.Logger = new LoggerConfiguration()
                            .Enrich.FromLogContext()
                            .WriteTo.Console()
                            .WriteTo.Seq("http://localhost:5341") // <- Added
                            .CreateLogger();

            services.AddSingleton(Log.Logger);
            services.AddSwagger(Configuration)
                    .AddRedis(Configuration)
                    .AddIntegrationServices(Configuration)
                    .RegisterEventBus(Configuration);
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
                   c.SwaggerEndpoint("/swagger/v1/swagger.json", "Basket.API");
               });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            Task.Run(async ()=> await ConfigureEventBus(app));
        }

        private async Task ConfigureEventBus(IApplicationBuilder app)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

           await eventBus.SubscribeAsync<ProductPriceChangedIntegrationEvent, ProductPriceChangedIntegrationEventHandler>();
        }
    }
}