using PeopleApi._2_BuildingBlocks;
using PeopleApi._2_BuildingBlocks._1_Ports;
using PeopleApi._3_Core.Images._1_Ports;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._3_Core.People._2_Application.Dtos;
using PeopleApi._3_Core.People._2_Application.Mappings;
using PeopleApi._3_Core.People._2_Application.Utils;
using PeopleApi._3_Core.People._3_Domain.Errors;

namespace PeopleApi._3_Core.People._2_Application.UseCases;

public sealed class PersonUcUpdate(
   IPersonRepository repository,
   IUnitOfWork unitOfWork,
   IImageUseCases imageUseCases,
   ILogger<PersonUcUpdate> logger
) {
   public async Task<Result<PersonDto>> ExecuteAsync(
      Guid id,
      PersonUpdateData dto,
      CancellationToken ct
   ) {
      // Replacing and removing the image at the same time is an invalid operation.
      if (dto.Image is not null && dto.RemoveImage)
         return Result<PersonDto>.Failure(PersonErrors.InvalidImageOperation);

      // Load the tracked domain entity that will be changed.
      var person = await repository.FindByIdAsync(id, ct);
      if (person is null)
         return Result<PersonDto>.Failure(PersonErrors.PersonNotFound);

      var previousImageUrl = person.ImageUrl;
      var nextImageUrl = previousImageUrl;
      string? newImageFileName = null;

      if (dto.Image is not null) {
         // Store the replacement first so the old image remains available until
         // the new People state has been committed successfully.
         var imageResult = await imageUseCases.CreateAsync(dto.Image, ct);
         if (imageResult.IsFailure)
            return Result<PersonDto>.Failure(imageResult.Error);

         newImageFileName = imageResult.Value.FileName;
         nextImageUrl = PersonImageReference.BuildUrl(
            dto.ImageBaseUrl,
            newImageFileName
         );
      }
      else if (dto.RemoveImage) {
         // Explicit removal is represented by a null ImageUrl in the Person.
         nextImageUrl = null;
      }

      // Let the domain entity validate, normalize and apply the new values.
      var resultUpdate = person.Update(
         dto.FirstName,
         dto.LastName,
         dto.Email,
         dto.Phone,
         nextImageUrl
      );
      if (resultUpdate.IsFailure) {
         // The replacement image is not referenced when domain validation fails.
         if (newImageFileName is not null)
            await DeleteImageQuietlyAsync(newImageFileName);

         return Result<PersonDto>.Failure(resultUpdate.Error);
      }

      try {
         // EF Core already tracks the entity, therefore SaveChanges is sufficient.
         var rows = await unitOfWork.SaveAllChangesAsync(nameof(PersonUcUpdate), ct);

         logger.LogInformation(
            "Person updated: personId={PersonId} rows={Rows}",
            person.Id,
            rows
         );
      }
      catch {
         // If the database commit throws, remove the unreferenced replacement.
         if (newImageFileName is not null)
            await DeleteImageQuietlyAsync(newImageFileName);

         throw;
      }

      // Delete the old managed image only after the Person now references the new
      // image (or no image). Cleanup errors do not invalidate a successful update.
      if (dto.Image is not null || dto.RemoveImage)
         await DeleteReferencedImageQuietlyAsync(previousImageUrl, dto.ImageBaseUrl);

      return Result<PersonDto>.Success(person.ToPersonDto());
   }

   private async Task DeleteImageQuietlyAsync(string fileName) {
      var deleteResult = await imageUseCases.DeleteAsync(
         fileName,
         CancellationToken.None
      );

      if (deleteResult.IsFailure) {
         logger.LogWarning(
            "Could not clean up image {FileName}: {ErrorCode}",
            fileName,
            deleteResult.Error.Code
         );
      }
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
            "Could not delete previous image {FileName}: {ErrorCode}",
            fileName,
            deleteResult.Error.Code
         );
      }
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * UPDATE mit optionalem Bild:
 *
 *   Person laden
 *       |
 *       v
 *   Bildoperation bestimmen
 *       |
 *       +-- kein Bild, RemoveImage=false --> bisherige ImageUrl behalten
 *       |
 *       +-- neues Bild -------------------> neue Datei speichern
 *       |                                   neue ImageUrl erzeugen
 *       |
 *       +-- RemoveImage=true -------------> ImageUrl = null
 *       |
 *       v
 *   Person aktualisieren
 *       |
 *       v
 *   EF Core speichern
 *       |
 *       v
 *   altes lokales Bild löschen
 *
 * - Die Reihenfolge ist absichtlich so gewählt: Das alte Bild bleibt bestehen,
 *   solange die neue Person noch nicht erfolgreich gespeichert wurde.
 * - Schlägt Domain-Validierung oder DB-Commit nach dem Upload fehl, wird das neu
 *   gespeicherte Bild wieder gelöscht; das alte Bild bleibt erhalten.
 * - Erst nach erfolgreichem People-Update wird das vorherige, von dieser API
 *   verwaltete Bild entfernt. Ein Fehler beim Aufräumen rollt das gültige Update
 *   nicht zurück, sondern wird protokolliert.
 * - Die Regel Image != null zusammen mit RemoveImage == true ist Application-
 *   Logik und wird deshalb hier und nicht im Controller geprüft.
 */
