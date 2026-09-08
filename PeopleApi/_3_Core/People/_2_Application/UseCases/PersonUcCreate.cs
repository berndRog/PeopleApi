using PeopleApi._2_BuildingBlocks;
using PeopleApi._2_BuildingBlocks._1_Ports;
using PeopleApi._2_BuildingBlocks._2_Application.Utils;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._3_Core.People._2_Application.Dtos;
using PeopleApi._3_Core.People._2_Application.Mappings;
using PeopleApi._3_Core.People._3_Domain.Entities;
using PeopleApi._3_Core.People._3_Domain.Errors;

namespace PeopleApi._3_Core.People._2_Application.UseCases;

public sealed class PersonUcCreate(
   IPersonRepository repository,
   IUnitOfWork unitOfWork,
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

      // Reject duplicate identifiers before creating a new aggregate.
      var id = resultId.Value;
      if (await repository.ExistsByIdAsync(id, ct))
         return Result<PersonDto>.Failure(PersonErrors.IdAlreadyExists);

      // Domain creation performs validation and normalization in one place.
      var resultPerson = Person.Create(
         id,
         dto.FirstName,
         dto.LastName,
         dto.Email,
         dto.Phone,
         dto.ImageUrl
      );
      if (resultPerson.IsFailure)
         return Result<PersonDto>.Failure(resultPerson.Error);

      // Add the valid entity to the repository and commit the unit of work.
      var person = resultPerson.Value;
      repository.Add(person);
      var rows = await unitOfWork.SaveAllChangesAsync(nameof(PersonUcCreate), ct);

      logger.LogInformation(
         "Person created: personId={PersonId} rows={Rows}",
         person.Id,
         rows
      );

      // Return a DTO instead of exposing the tracked domain entity to the Web layer.
      return Result<PersonDto>.Success(person.ToPersonDto());
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Der Create-UseCase orchestriert einen vollständigen Anwendungsfall:
 *   ID bestimmen -> Duplikat prüfen -> Domain-Objekt erzeugen -> speichern.
 * - Validierungsregeln liegen in Person und nicht im Controller.
 * - Repository und UnitOfWork sind Ports; konkrete EF-Core-Klassen bleiben aus
 *   der Application-Schicht heraus.
 */
