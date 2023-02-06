
namespace LinuxPackApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration.SetBasePath(Environment.CurrentDirectory).AddJsonFile("appsettings.json").Build();
            var urls = configuration["Urls"];

            builder.WebHost.UseKestrel();
            builder.WebHost.UseUrls(urls ?? "http://localhost:10018/");

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();
            app.UseSwagger();
            app.UseSwaggerUI();
            //app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}