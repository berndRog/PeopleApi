namespace PeopleApi._2_BuildingBlocks._3_Domain.Entities;

// Aggregate roots are the entry points to domain consistency boundaries.
public abstract class AggregateRoot : Entity {
   public DateTime CreatedAt { get; protected set; }
   public DateTime UpdatedAt { get; protected set; }

   protected void Initialize(DateTime createdAt) {
      EnsureUtc(createdAt, nameof(createdAt));
      CreatedAt = createdAt;
      UpdatedAt = createdAt;
   }

   protected void Touch(DateTime updatedAt) {
      EnsureUtc(updatedAt, nameof(updatedAt));
      UpdatedAt = updatedAt;
   }

   private static void EnsureUtc(DateTime value, string parameterName) {
      if (value == default)
         throw new ArgumentException("Timestamp must be set.", parameterName);
      if (value.Kind != DateTimeKind.Utc)
         throw new ArgumentException("Timestamp must have DateTimeKind.Utc.", parameterName);
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - AggregateRoot ergänzt die Identität um den Lebenszyklus des Aggregats.
 * - Intern werden ausschließlich DateTime-Werte mit DateTimeKind.Utc verwendet.
 * - Die Zeit kommt von IClock; die Domäne greift nicht direkt auf DateTime.UtcNow zu.
 */
