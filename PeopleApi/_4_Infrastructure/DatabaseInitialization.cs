using Microsoft.EntityFrameworkCore;
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
      // The People table intentionally stays empty; clients may seed it via the API.
      await dbContext.Database.EnsureCreatedAsync();
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Die Initialisierung wird beim Programmstart explizit ausgeführt.
 * - EnsureCreated eignet sich für dieses einfache Lehrbeispiel ohne Migrationen.
 * - Die WebAPI erzeugt nur das Schema und keine fachlichen Beispieldaten.
 *   Ein Client kann GET /people/count verwenden und eine leere Datenbank bei Bedarf
 *   über die normalen POST-Endpunkte initialisieren.
 * - Für Images existiert bewusst keine Datenbanktabelle; das Image-Verzeichnis
 *   wird vom FileStorage angelegt.
 */
