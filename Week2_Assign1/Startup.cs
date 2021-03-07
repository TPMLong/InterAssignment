using AuthDemo.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Week2_Assign1.Data;
using Week2_Assign1.Models;

namespace Week2_Assign1
{
    //This project not build from my own, i learn it from many resource!!
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

            // DB Context
            services.AddDbContext<DBContext>(opt => opt.UseSqlServer(Configuration.GetConnectionString("DBContext")));

            //Configure Identity Service
            services.AddIdentity<User, IdentityRole>().AddEntityFrameworkStores<DBContext>().AddDefaultTokenProviders();
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            });

            // JWT Configuration
            var jwtSettings = Configuration.GetSection("JwtSettings");
            //add JWT authentication middleware
            services.AddAuthentication(opt =>
            {
                //enables authentication and sets "Bearer" as default Scheme
                //When you use [Authorize] without specifying an authentication scheme, it
                //will by defualt challenge the us using the handler configured for "Bearer"
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                //provide some parameters to validate the JWT
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    //the issuer will be validated during token validation.
                    ValidateIssuer = false,
                    //Gets or sets a boolean to control if the audience will be validated during token validation.
                    //validateAudience:false ignores the audience claim
                    //true is 'The receiver of the token is a valid recipient'
                    ValidateAudience = false,
                    //The token has not expired
                    ValidateLifetime = true,
                    //The signing key is valid and is trusted by the server
                    ValidateIssuerSigningKey = true,

                    //the server uses those below values to generate the signature for JWT
                    // value is configurate in appsettings and the server will get those
                    ValidIssuer = jwtSettings.GetSection("validIssuer").Value,
                    ValidAudience = jwtSettings.GetSection("validAudience").Value,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.GetSection("securityKey").Value))
                
                };
            });

            // Auto Mapper Configurations
            var mappingConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new MappingProfile());
            });

            IMapper mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);
            //services.AddScoped<IUserData, SqlUserData>();
            services.AddControllersWithViews();

            // In production, the React files will be served from this directory
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            //use the authentication middleware
            //you MUST, it's a MUST to Authentication then Authorization
            //you may wonder why, here is the reason
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
