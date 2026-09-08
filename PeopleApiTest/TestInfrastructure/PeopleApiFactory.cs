using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using PeopleApi;

namespace PeopleApiTest.TestInfrastructure;

public sealed class PeopleApiFactory : WebApplicationFactory<Program> {
   // Every test factory gets its own root folder so database and image files
   // cannot leak state into another test or into the developer's local files.
   public string TestRootDirectory { get; } = Path.Combine(
      Path.GetTempPath(),
      "PeopleApiTests",
      Guid.NewGuid().ToString("N")
   );

   public string DatabasePath =>
      Path.Combine(TestRootDirectory, "people-test.db");

   public string ImageDirectory =>
      Path.Combine(TestRootDirectory, "images");

   public PeopleApiFactory() {
      // Create the root up front; the application will create the image subfolder
      // when the file-storage service is instantiated.
      Directory.CreateDirectory(TestRootDirectory);
   }

   protected override void ConfigureWebHost(IWebHostBuilder builder) {
      // Avoid development-only middleware such as Swagger in automated tests.
      builder.UseEnvironment("Test");

      builder.ConfigureAppConfiguration((_, configuration) => {
         // Override only external resources. The production DI graph and Program
         // are otherwise used unchanged by WebApplicationFactory.
         var settings = new Dictionary<string, string?> {
            ["ConnectionStrings:PeopleDb"] = $"Data Source={DatabasePath}",
            ["ImageStorage:Directory"] = ImageDirectory,

            // Keep the test limit small so the too-large case needs only 1 KiB.
            ["ImageStorage:MaxFileSizeBytes"] = "1024"
         };

         configuration.AddInMemoryCollection(settings);
      });
   }

   protected override void Dispose(bool disposing) {
      // Stop TestServer and release SQLite file handles first.
      base.Dispose(disposing);

      // Remove all test artifacts after the application has been disposed.
      if (disposing && Directory.Exists(TestRootDirectory))
         Directory.Delete(TestRootDirectory, recursive: true);
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - WebApplicationFactory startet die reale ASP.NET-Core-Anwendung im Testprozess.
 * - Nur externe Ressourcen werden umgebogen: SQLite und Image-Verzeichnis liegen
 *   in einem eindeutigen temporären Ordner.
 * - Dadurch testen wir dieselbe DI-Konfiguration und dieselben Controller/UseCases
 *   wie produktiv, ohne lokale Entwicklungsdaten zu verändern.
 * - Testisolation ist besonders wichtig, weil die Tests echte Datei-I/O ausführen.
 */
