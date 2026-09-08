using Microsoft.EntityFrameworkCore;
using PeopleApi._4_Infrastructure.Persistence;
using PeopleApi._4_Infrastructure.Persistence.Database;

namespace PeopleApi._4_Infrastructure;

public static class DatabaseInitialization {
   public static async Task InitializeDatabaseAsync(
      this IServiceProvider services
   ) {
      // Create a short-lived DI scope because AppDbContext is registered scoped.
      await using var scope = services.CreateAsyncScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

      // Create the SQLite schema if no database exists yet.
      await dbContext.Database.EnsureCreatedAsync();

      // Fill an empty People table with deterministic demo data.
      await Seed.SeedPeopleAsync(dbContext);
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Die Initialisierung wird beim Programmstart explizit ausgeführt.
 * - EnsureCreated eignet sich für dieses einfache Lehrbeispiel ohne Migrationen.
 * - Nur die People-Datenbank wird initialisiert. Für Images existiert bewusst
 *   keine Datenbanktabelle; das Image-Verzeichnis wird vom FileStorage angelegt.
 */
