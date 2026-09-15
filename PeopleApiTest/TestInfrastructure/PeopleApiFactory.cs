using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PeopleApi;
using PeopleApi._4_Infrastructure.Persistence.Database;

namespace PeopleApiTest.TestInfrastructure;

public sealed class PeopleApiFactory : WebApplicationFactory<Program> {
   // Every factory owns one isolated SQLite file.
   public string TestRootDirectory { get; } = Path.Combine(
      Path.GetTempPath(),
      "PeopleApiTests",
      Guid.NewGuid().ToString("N")
   );

   public string DatabasePath =>
      Path.Combine(TestRootDirectory, "people-test.db");

   public PeopleApiFactory() {
      Directory.CreateDirectory(TestRootDirectory);
   }

   protected override void ConfigureWebHost(IWebHostBuilder builder) {
      builder.UseEnvironment("Test");

      builder.ConfigureTestServices(services => {
         services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
         services.RemoveAll<DbContextOptions<AppDbContext>>();
         services.RemoveAll<AppDbContext>();

         services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={DatabasePath}")
         );
      });
   }

   protected override void Dispose(bool disposing) {
      base.Dispose(disposing);

      if (disposing && Directory.Exists(TestRootDirectory))
         Directory.Delete(TestRootDirectory, recursive: true);
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - WebApplicationFactory startet die reale ASP.NET-Core-Anwendung im Testprozess.
 * - Jede Factory verwendet eine eigene SQLite-Datei.
 * - Da die API keine Bilddateien verarbeitet, muss der Testhost ausschließlich
 *   die Datenbank-Infrastruktur ersetzen.
 */
