using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PeopleApi._4_Infrastructure.Persistence.Converters;

public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime> {
   public UtcDateTimeConverter()
      : base(
         value => EnsureUtc(value),
         value => DateTime.SpecifyKind(value, DateTimeKind.Utc)
      ) {
   }

   private static DateTime EnsureUtc(DateTime value) {
      if (value.Kind != DateTimeKind.Utc)
         throw new InvalidOperationException("Only UTC DateTime values may be persisted.");
      return value;
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - SQLite speichert keinen DateTimeKind; beim Lesen wird Utc deshalb restauriert.
 * - Nicht-UTC-Werte werden beim Schreiben abgewiesen statt stillschweigend umgerechnet.
 * - Interne Zeitwerte besitzen damit genau eine eindeutige UTC-Repräsentation.
 */
