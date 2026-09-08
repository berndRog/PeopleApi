using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PeopleApi;
using PeopleApi._4_Infrastructure.Persistence.Database;
using PeopleApi._4_Infrastructure.Storage;

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
      // Create the root up front. SQLite creates the database file lazily and
      // ImageFileStorageFs creates the image subfolder when it is constructed.
      Directory.CreateDirectory(TestRootDirectory);
   }

   protected override void ConfigureWebHost(IWebHostBuilder builder) {
      // Avoid development-only middleware such as Swagger in automated tests.
      builder.UseEnvironment("Test");

      builder.ConfigureTestServices(services => {
         // The production module has already registered AppDbContext. Remove
         // that registration explicitly so tests can never fall back to the
         // developer database from appsettings.json.
         services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
         services.RemoveAll<DbContextOptions<AppDbContext>>();
         services.RemoveAll<AppDbContext>();

         // Register a new DbContext that points to this factory's unique SQLite
         // file. Program.InitializeDatabaseAsync() now creates and seeds exactly
         // this database when the TestServer starts.
         services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={DatabasePath}")
         );

         // Override file-storage settings after the production configuration was
         // bound. Each factory therefore also receives its own image directory.
         services.PostConfigure<ImageStorageOptions>(options => {
            options.Directory = ImageDirectory;

            // Keep the test limit small so the too-large test needs only 1 KiB.
            options.MaxFileSizeBytes = 1024;
         });
      });
   }

   protected override void Dispose(bool disposing) {
      // Stop TestServer and release SQLite/file-system handles first.
      base.Dispose(disposing);

      // Remove all test artifacts only after the application has been disposed.
      if (disposing && Directory.Exists(TestRootDirectory))
         Directory.Delete(TestRootDirectory, recursive: true);
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - WebApplicationFactory startet die reale ASP.NET-Core-Anwendung im Testprozess.
 * - Die produktiven Ports und UseCases bleiben unverändert; nur externe Ressourcen
 *   werden im Testhost gezielt ersetzt bzw. umkonfiguriert.
 * - Der AppDbContext wird explizit neu registriert. Dadurch besitzt jede Factory
 *   eine eigene SQLite-Datei und Tests können keine Personen untereinander teilen.
 * - ImageStorageOptions werden nach der produktiven Konfiguration überschrieben,
 *   sodass auch Datei-I/O ausschließlich in einem temporären Testordner stattfindet.
 * - Explizites Ersetzen der Infrastruktur ist robuster als nur einen anderen
 *   Connection-String in eine zusätzliche ConfigurationSource einzutragen.
 */
