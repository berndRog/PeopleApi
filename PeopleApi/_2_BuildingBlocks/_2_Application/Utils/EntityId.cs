using PeopleApi._2_BuildingBlocks._3_Domain.Errors;

namespace PeopleApi._2_BuildingBlocks._2_Application.Utils;

public static class EntityId {
   public static Result<Guid> Resolve(
      string? externalId,
      DomainError invalidIdError
   ) {
      // Generate a server-side id when the client supplied none.
      if (string.IsNullOrWhiteSpace(externalId))
         return Result<Guid>.Success(Guid.NewGuid());

      // Preserve valid client-generated UUIDs, e.g. UUIDs created on Android.
      return Guid.TryParse(externalId.Trim(), out var id)
         ? Result<Guid>.Success(id)
         : Result<Guid>.Failure(invalidIdError);
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Mobile Clients dürfen ihre bereits erzeugten UUIDs an den Server übertragen.
 * - Fehlt eine ID, kann die API weiterhin selbst eine Guid erzeugen.
 */
