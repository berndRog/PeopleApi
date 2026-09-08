using Microsoft.EntityFrameworkCore;
using PeopleApi._3_Core.People._3_Domain.Entities;
using PeopleApi._4_Infrastructure.Persistence.Database;

namespace PeopleApi._4_Infrastructure.Persistence;

internal static class Seed {
   // Deterministic sample data makes manual Swagger tests and automated tests
   // reproducible across fresh databases.
   private static readonly string[] FirstNames = [
      "Arne", "Berta", "Cord", "Dagmar", "Ernst", "Frieda", "Günter", "Hanna",
      "Ingo", "Johanna", "Klaus", "Luise", "Martin", "Nadja", "Otto", "Patrizia",
      "Quirin", "Rebecca", "Stefan", "Tanja", "Uwe", "Veronika", "Walter", "Xenia",
      "Yannick", "Zwantje"
   ];

   private static readonly string[] LastNames = [
      "Arndt", "Bauer", "Conrad", "Diehl", "Engel", "Fischer", "Graf", "Hoffmann",
      "Imhoff", "Jung", "Klein", "Lang", "Meier", "Neumann", "Olbrich", "Peters",
      "Quart", "Richter", "Schmidt", "Thormann", "Ulrich", "Vogel", "Wagner", "Xander",
      "Yakov", "Zander"
   ];

   public static async Task SeedPeopleAsync(
      AppDbContext dbContext,
      CancellationToken ct = default
   ) {
      // Never duplicate sample data in an already initialized database.
      if (await dbContext.People.AnyAsync(ct))
         return;

      for (var index = 0; index < FirstNames.Length; index++) {
         // Use stable GUIDs so examples can refer to known resources if needed.
         var id = Guid.Parse($"{index + 1:00}000000-0000-0000-0000-000000000000");

         // Seed through the domain factory so sample data obeys the same rules
         // as normal API-created persons.
         var result = Person.Create(
            id,
            FirstNames[index],
            LastNames[index],
            $"person{index + 1:00}@example.org",
            $"030 1234-{1000 + index}",
            imageUrl: null
         );

         if (result.IsFailure)
            throw new InvalidOperationException($"Invalid seed person: {result.Error.Code}");

         dbContext.People.Add(result.Value);
      }

      // Commit all sample rows in one database operation.
      await dbContext.SaveChangesAsync(ct);
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Seed-Daten erleichtern das explorative Testen der GET-Endpunkte in Swagger.
 * - Auch Test-/Seed-Daten werden über die Domain-Fabrik erzeugt und umgehen die
 *   Validierungsregeln nicht.
 * - Deterministische IDs und Namen machen Ergebnisse reproduzierbar.
 */
