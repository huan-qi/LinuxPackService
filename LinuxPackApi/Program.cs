
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using LinuxPackApi.Model;
using System.Diagnostics;
using Microsoft.Extensions.FileProviders;

namespace LinuxPackApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            DirectoryInfo zipPackageDirectoryInfo = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), "ZipPackage"));
            if (!zipPackageDirectoryInfo.Exists)
            {
                zipPackageDirectoryInfo.Create();
            }
            var fileProvider = new PhysicalFileProvider(zipPackageDirectoryInfo.FullName);
            var requestPath = "/ShareZipPackages";

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
                              .WithMethods("PUT", "DELETE", "GET", "POST")
                              .AllowCredentials()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
            }); //ע��Cors����
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<HttpResponseExceptionFilter>();
            }); //ע��Controller������
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(); //ע��Swagger

            var app = builder.Build();
            app.UseSwagger();  //ʹ��swagger
            app.UseSwaggerUI();//ʹ��swagger
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = fileProvider,
                RequestPath = requestPath
            }); //�����ļ�������
            app.UseDirectoryBrowser(new DirectoryBrowserOptions
            {
                FileProvider = fileProvider,
                RequestPath = requestPath
            }); //�����ļ�������Ŀ¼��ʾ
            app.UseCors("PolicyCors"); //����Cors
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