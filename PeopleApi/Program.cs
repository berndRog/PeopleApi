using PeopleApi._4_Infrastructure;
using PeopleApi.Configure;

namespace PeopleApi;

public class Program {
   public static async Task Main(string[] args) {
      // Create the ASP.NET Core application builder and load configuration,
      // logging and environment information.
      var builder = WebApplication.CreateBuilder(args);

      // Register the HTTP layer first. Controllers translate HTTP requests
      // into calls to the application layer.
      builder.Services.AddControllers();

      // Register the People application module.
      builder.Services.AddPeopleModule();

      // Register the EF Core persistence implementation.
      builder.Services.AddInfrastructureModule(builder.Configuration);

      // Add URL-segment API versioning and Swagger/OpenAPI support.
      builder.Services.AddApiVersioningForPeopleApi();
      builder.Services.AddSwaggerForPeopleApi();

      // Build the dependency injection container and HTTP pipeline.
      var app = builder.Build();

      // Expose Swagger only while developing the API.
      if (app.Environment.IsDevelopment()) {
         app.UseDeveloperExceptionPage();
         app.UseSwagger();
         app.UseSwaggerUI(options => {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "PeopleApi v1");
            options.RoutePrefix = "swagger";
         });
      }

      // Create the empty People database if necessary.
      await app.Services.InitializeDatabaseAsync();

      // Map attribute-routed controllers and start processing HTTP requests.
      app.MapControllers();
      await app.RunAsync();
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Program.cs zeigt den Composition Root der WebAPI: Hier werden alle Module
 *   registriert und anschließend zur laufenden Anwendung zusammengesetzt.
 * - Die Reihenfolge macht die Schichten sichtbar: Web -> Core -> Infrastructure.
 * - Dieses erste Retrofit-Beispiel enthält bewusst nur die People-Ressource.
 *   Bildübertragung und Dateispeicherung folgen erst im nächsten Lernschritt.
 * - Swagger wird nur für die Entwicklungsumgebung aktiviert.
 */
