using PeopleApi._2_BuildingBlocks;
using PeopleApi._2_BuildingBlocks._1_Ports;
using PeopleApi._2_BuildingBlocks._2_Application.Utils;
using PeopleApi._3_Core.Images._1_Ports;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._3_Core.People._2_Application.Dtos;
using PeopleApi._3_Core.People._2_Application.Mappings;
using PeopleApi._3_Core.People._2_Application.Utils;
using PeopleApi._3_Core.People._3_Domain.Entities;
using PeopleApi._3_Core.People._3_Domain.Errors;

namespace PeopleApi._3_Core.People._2_Application.UseCases;

public sealed class PersonUcCreate(
   IPersonRepository repository,
   IUnitOfWork unitOfWork,
   IImageUseCases imageUseCases,
   ILogger<PersonUcCreate> logger
) {
   public async Task<Result<PersonDto>> ExecuteAsync(
      PersonCreateData dto,
      CancellationToken ct
   ) {
      // Preserve a client-generated UUID when present; otherwise create a new id.
      var resultId = EntityId.Resolve(dto.Id, PersonErrors.InvalidId);
      if (resultId.IsFailure)
         return Result<PersonDto>.Failure(resultId.Error);

      // Reject duplicate identifiers before any optional file is written.
      var id = resultId.Value;
      if (await repository.ExistsByIdAsync(id, ct))
         return Result<PersonDto>.Failure(PersonErrors.IdAlreadyExists);

      string? newImageFileName = null;
      string? imageUrl = null;

      // Store the optional image as part of this application use case.
      if (dto.Image is not null) {
         var imageResult = await imageUseCases.CreateAsync(dto.Image, ct);
         if (imageResult.IsFailure)
            return Result<PersonDto>.Failure(imageResult.Error);

         newImageFileName = imageResult.Value.FileName;
         imageUrl = PersonImageReference.BuildUrl(dto.ImageBaseUrl, newImageFileName);
      }

      // Domain creation performs validation and normalization in one place.
      var resultPerson = Person.Create(
         id,
         dto.FirstName,
         dto.LastName,
         dto.Email,
         dto.Phone,
         imageUrl
      );
      if (resultPerson.IsFailure) {
         // The new file is not referenced when domain validation fails.
         if (newImageFileName is not null)
            await DeleteImageQuietlyAsync(newImageFileName);

         return Result<PersonDto>.Failure(resultPerson.Error);
      }

      var person = resultPerson.Value;
      repository.Add(person);

      try {
         // Commit People persistence only after all data including ImageUrl is ready.
         var rows = await unitOfWork.SaveAllChangesAsync(nameof(PersonUcCreate), ct);

         logger.LogInformation(
            "Person created: personId={PersonId} rows={Rows}",
            person.Id,
            rows
         );
      }
      catch {
         // Compensate the preceding file-system write if the database commit throws.
         if (newImageFileName is not null)
            await DeleteImageQuietlyAsync(newImageFileName);

         throw;
      }

      return Result<PersonDto>.Success(person.ToPersonDto());
   }

   private async Task DeleteImageQuietlyAsync(string fileName) {
      // Cleanup should still run when the original HTTP request was cancelled.
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
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * CREATE mit optionalem Bild:
 *
 *   PersonCreateData
 *          |
 *          v
 *   ID bestimmen / Duplikat prüfen
 *          |
 *          v
 *   Bild vorhanden?
 *      |         |
 *     nein       ja
 *      |         |
 *      |     Image speichern
 *      |         |
 *      |     ImageUrl erzeugen
 *      |         |
 *      +----+----+
 *           v
 *      Person erzeugen
 *           |
 *           v
 *      EF Core speichern
 *
 * - Der UseCase koordiniert damit den vollständigen Anwendungsfall und nicht der
 *   Controller. Der Controller liefert nur ImageUpload und ImageBaseUrl.
 * - Wird das Bild erfolgreich gespeichert, aber Person.Create oder der DB-Commit
 *   schlägt anschließend fehl, wird die neue Datei wieder gelöscht (Kompensation).
 * - Die Duplikatprüfung erfolgt vor dem Upload, damit bei einem bekannten Fehler
 *   gar keine unnötige Datei erzeugt wird.
 * - Repository, UnitOfWork und IImageUseCases sind Ports; konkrete EF-Core- und
 *   File-System-Klassen bleiben außerhalb der Application-Schicht.
 */
