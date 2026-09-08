using PeopleApi._2_BuildingBlocks;
using PeopleApi._2_BuildingBlocks._1_Ports;
using PeopleApi._3_Core.Images._1_Ports;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._3_Core.People._2_Application.Utils;
using PeopleApi._3_Core.People._3_Domain.Errors;

namespace PeopleApi._3_Core.People._2_Application.UseCases;

public sealed class PersonUcDelete(
   IPersonRepository repository,
   IUnitOfWork unitOfWork,
   IImageUseCases imageUseCases,
   ILogger<PersonUcDelete> logger
) {
   public async Task<Result> ExecuteAsync(
      Guid id,
      string imageBaseUrl,
      CancellationToken ct
   ) {
      // Find the tracked entity and remember its image reference before deletion.
      var person = await repository.FindByIdAsync(id, ct);
      if (person is null)
         return Result.Failure(PersonErrors.PersonNotFound);

      var imageUrl = person.ImageUrl;

      // Delete the People resource first. If the database operation fails, the
      // image remains available because no file cleanup has happened yet.
      repository.Remove(person);
      var rows = await unitOfWork.SaveAllChangesAsync(nameof(PersonUcDelete), ct);

      logger.LogInformation(
         "Person deleted: personId={PersonId} rows={Rows}",
         id,
         rows
      );

      // Only after the database commit succeeds do we remove a managed local image.
      await DeleteReferencedImageQuietlyAsync(imageUrl, imageBaseUrl);

      return Result.Success();
   }

   private async Task DeleteReferencedImageQuietlyAsync(
      string? imageUrl,
      string imageBaseUrl
   ) {
      if (!PersonImageReference.TryGetManagedFileName(
             imageUrl,
             imageBaseUrl,
             out var fileName)) {
         return;
      }

      var deleteResult = await imageUseCases.DeleteAsync(
         fileName,
         CancellationToken.None
      );

      if (deleteResult.IsFailure) {
         logger.LogWarning(
            "Could not delete image {FileName}: {ErrorCode}",
            fileName,
            deleteResult.Error.Code
         );
      }
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * DELETE mit optionalem Bild:
 *
 *   Person laden
 *       |
 *       v
 *   ImageUrl merken
 *       |
 *       v
 *   Person aus DB löschen
 *       |
 *       v
 *   DB-Commit erfolgreich?
 *       |
 *       +-- nein --> Bild bleibt bestehen
 *       |
 *       +-- ja ----> zugehöriges lokales Bild löschen
 *
 * - Der vollständige Delete-Anwendungsfall liegt in der Application-Schicht.
 * - Die Datenbankoperation hat Vorrang: Erst wenn die Person erfolgreich gelöscht
 *   wurde, darf die referenzierte lokale Bilddatei entfernt werden.
 * - Ein Fehler beim anschließenden Datei-Cleanup macht das bereits erfolgreiche
 *   People-Delete nicht rückgängig; der Fehler wird deshalb nur protokolliert.
 * - IImageUseCases bleibt ein wiederverwendbarer Port. Der People-UseCase kennt
 *   weder konkrete File-System-Klassen noch EF-Core-Implementierungen.
 */
