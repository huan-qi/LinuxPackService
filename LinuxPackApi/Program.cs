
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using LinuxPackApi.Model;
using System.Diagnostics;

namespace LinuxPackApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration.SetBasePath(Environment.CurrentDirectory).AddJsonFile("appsettings.json").Build();
            var urls = configuration["Urls"];
            var url = urls ?? "http://localhost:10018/";

            builder.WebHost.UseKestrel();
            builder.WebHost.UseUrls(url);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("PolicyCors",
                    policy =>
                    {
                        policy.WithOrigins(url)
                              .AllowCredentials()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
            });
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<HttpResponseExceptionFilter>();
            });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCors("PolicyCors");
            //app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }

    public class HttpResponseExceptionFilter : IActionFilter, IOrderedFilter
    {
        public int Order => int.MaxValue - 10;

        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception is Exception httpResponseException)
            {
                context.Result = new ObjectResult(new ApiRequestMode(null, httpResponseException.Message))
                {
                    StatusCode = 400
                };

                context.ExceptionHandled = true;
            }
            else
            {
                if (context.Result is ObjectResult objectResult)
                {
                    var data = objectResult.Value;
                    var statusCode = objectResult.StatusCode;
                    var apiResult = new ApiRequestMode(data);
                    context.Result = new ObjectResult(apiResult) 
                    {
                        StatusCode = statusCode
                    };
                }
                
            }
        }
    }
}