using Dotnettency;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Sample.Authentication
{
    public class Startup
    {
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var defaultServices = services.Clone();

            try
            {
                var sp = services.AddMultiTenancy<Tenant>((builder) =>
                {
                    builder.IdentifyTenantsWithRequestAuthorityUri()
                           .InitialiseTenant<TenantShellFactory>()
                           .AddAspNetCore()
                           .ConfigureTenantContainers((containerOptions) =>
                           {
                               containerOptions
                               .SetDefaultServices(defaultServices)
                               .Autofac((tenant, tenantServices) =>
                               {
                                   if (tenant.Name == "Moogle")
                                   {
                                       tenantServices.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                                       .AddCookie((c) =>
                                       {
                                           c.Cookie.Name = tenant.Name;
                                       });
                                   }
                               });
                           })
                           .ConfigureTenantMiddleware((tenantOptions) =>
                           {
                               tenantOptions.AspNetCorePipeline((context, tenantAppBuilder) =>
                               {

                                   tenantAppBuilder.UseDeveloperExceptionPage();
                                   tenantAppBuilder.UseStaticFiles();

                               //  var log = c.ApplicationServices.GetRequiredService<ILogger<Startup>>();
                               if (context.Tenant.Name == "Moogle")
                                   {
                                       tenantAppBuilder.UseAuthentication();

                                   // Browse to /Protected endpoint, will issue a challenge if not authenticated.
                                   // This challenge automatically redirects to the default login path = /Account/Login
                                   tenantAppBuilder.Map("/Protected", (d) =>
                                       {
                                           d.Run(async (h) =>
                                           {
                                               if (!h.User.Identity?.IsAuthenticated ?? false)
                                               {
                                                   await h.ChallengeAsync();
                                               }
                                               else
                                               {
                                                   await h.Response.WriteAsync("Authenticated as: " + h.User.FindFirstValue(ClaimTypes.Name));
                                               }
                                           });
                                       });

                                   // Browse to /Account/Login will automatically create a sign in cookie then redirect to /Protected
                                   tenantAppBuilder.Map("/Account/Login", (d) =>
                                       {
                                           d.Run(async (h) =>
                                           {
                                               List<Claim> claims = new List<Claim>{
                                            new Claim(ClaimTypes.Name, "testuser"),
                                            new Claim("FullName", "test user"),
                                            new Claim(ClaimTypes.Role, "Administrator"),
                                            };

                                               ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                                               AuthenticationProperties authProperties = new AuthenticationProperties
                                               {
                                                   RedirectUri = "/Protected"
                                               };

                                               await h.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                                   new ClaimsPrincipal(claimsIdentity), authProperties);
                                           });
                                       });

                                   }

                               // All tenants have welcome page middleware enabled.
                               tenantAppBuilder.UseWelcomePage();

                               });
                           });

                });
                return sp;

            }
            catch (Exception ex)
            {

                throw ex;
            }

            // When using tenant containers, must return IServiceProvider.
            // Note: in asp.netcore 3.0.0 we don't need to, but something additional must be registered in program createhostbuilder.

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app = app.UseMultitenancy<Tenant>((options) =>
            {
                options.UseTenantContainers();
                options.UsePerTenantMiddlewarePipeline(app);
            });
        }
    }
}
