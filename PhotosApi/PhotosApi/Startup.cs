using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using PhotoStorageAPI.ExternalApi;
using PhotoStorageAPI.Models;
using PhotoStorageAPI.Searching;

namespace PhotosApi
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
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Photos searching service", Version = "v1" });
            });

            services.AddHttpClient();
            services.AddMemoryCache();

            services.Configure<AppConfig>(cfg => Configuration.Bind(cfg));

            services.AddSingleton(sp =>
            {
                IOptions<AppConfig> serviceConfigHolder =
                    sp.GetRequiredService<IOptions<AppConfig>>();

                return serviceConfigHolder.Value.ExternalApi;
            });

            services.AddSingleton<IPhotosExternalClient, PhotosExternalClient>();
            services.AddSingleton<IPhotosSearcher, PhotosSearcher>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PhotosApi v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
