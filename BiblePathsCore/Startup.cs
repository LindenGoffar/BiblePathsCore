using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using BiblePathsCore.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using BiblePathsCore.Services;
using BiblePathsCore.Models;
using BiblePathsCore.Hubs;

namespace BiblePathsCore
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
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("AuthConnection")));

            services.AddDbContext<BiblePathsCoreDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("AppConnection")));

            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddDefaultIdentity<IdentityUser>(options => {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.SignIn.RequireConfirmedEmail = false;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>();

            // requires
            // using Microsoft.AspNetCore.Identity.UI.Services;
            // using BiblePathsCore.Services;
            services.AddTransient<IEmailSender, EmailSender>();
            services.Configure<AuthMessageSenderOptions>(Configuration);

            services.AddAuthentication()
                    .AddFacebook(facebookOptions =>
                        {
                            facebookOptions.AppId = Configuration["Authentication:Facebook:AppId"];
                            facebookOptions.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
                        })
                    .AddGoogle(googleOptions =>
                     {
                         IConfigurationSection googleAuthNSection =
                             Configuration.GetSection("Authentication:Google");

                         googleOptions.ClientId = googleAuthNSection["ClientId"];
                         googleOptions.ClientSecret = googleAuthNSection["ClientSecret"];
                     })
                    .AddMicrosoftAccount(microsoftOptions =>
                    {
                        microsoftOptions.ClientId = Configuration["Authentication:Microsoft:ClientId"];
                        microsoftOptions.ClientSecret = Configuration["Authentication:Microsoft:ClientSecret"];
                    });

            services.AddRazorPages()
                .AddRazorPagesOptions(options =>
                 {
                     options.Conventions.AddPageRoute("/Paths/Path", "/Paths/{name}");
                     options.Conventions.AddPageRoute("/Search", "/Search/{SearchString?}");
                 });
            services.AddSignalR();
            services.AddApplicationInsightsTelemetry();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapHub<GameTeamHub>("/GameTeamHub");
            });
        }
    }
}
