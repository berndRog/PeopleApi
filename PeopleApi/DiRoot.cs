using Asp.Versioning;
using Microsoft.OpenApi;

namespace PeopleApi;

public static class DiRoot {
   public static IServiceCollection AddApiVersioningForPeopleApi(
      this IServiceCollection services
   ) {
      // Use version 1.0 whenever a client omits an explicit version.
      services
         .AddApiVersioning(options => {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;

            // Read the API version from the URL segment /v{version}/.
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
         })
         .AddMvc()
         .AddApiExplorer(options => {
            // Create Swagger groups such as "v1".
            options.GroupNameFormat = "'v'VVV";

            // Replace {version:apiVersion} with the concrete version in Swagger.
            options.SubstituteApiVersionInUrl = true;
         });

      return services;
   }

   public static IServiceCollection AddSwaggerForPeopleApi(
      this IServiceCollection services
   ) {
      // Let Swagger inspect controller endpoints and their metadata.
      services.AddEndpointsApiExplorer();
      services.AddSwaggerGen(options => {
         // Describe the first public API version.
         options.SwaggerDoc("v1", new OpenApiInfo {
            Title = "PeopleApi",
            Version = "v1",
            Description = "People and reusable image resources"
         });

         // Include XML documentation when the compiler generated the XML file.
         var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
         var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
         if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
      });

      return services;
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - API-Versionierung und Swagger sind technische Querschnittsfunktionen und
 *   werden deshalb zentral registriert.
 * - Die Version ist Teil der URL und damit für Client und Dokumentation sichtbar.
 * - ApiExplorer liefert die Metadaten, aus denen Swagger das OpenAPI-Dokument
 *   erzeugt. Dadurch werden Controller-Signaturen Teil des API-Vertrags.
 * - Die vom Compiler erzeugte XML-Dokumentation ergänzt in Swagger Summary,
 *   Remarks, Parameterbeschreibungen und Response-Texte der Controller.
 */
