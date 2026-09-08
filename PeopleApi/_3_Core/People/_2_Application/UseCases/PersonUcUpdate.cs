using PeopleApi._2_BuildingBlocks;
using PeopleApi._2_BuildingBlocks._1_Ports;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._3_Core.People._2_Application.Dtos;
using PeopleApi._3_Core.People._2_Application.Mappings;
using PeopleApi._3_Core.People._3_Domain.Errors;

namespace PeopleApi._3_Core.People._2_Application.UseCases;

public sealed class PersonUcUpdate(
   IPersonRepository repository,
   IUnitOfWork unitOfWork,
   ILogger<PersonUcUpdate> logger
) {
   public async Task<Result<PersonDto>> ExecuteAsync(
      Guid id,
      PersonUpdateData dto,
      CancellationToken ct
   ) {
      // Load the tracked domain entity that will be changed.
      var person = await repository.FindByIdAsync(id, ct);
      if (person is null)
         return Result<PersonDto>.Failure(PersonErrors.PersonNotFound);

      // Let the domain entity validate, normalize and apply the new values.
      var resultUpdate = person.Update(
         dto.FirstName,
         dto.LastName,
         dto.Email,
         dto.Phone,
         dto.ImageUrl
      );
      if (resultUpdate.IsFailure)
         return Result<PersonDto>.Failure(resultUpdate.Error);

      // EF Core already tracks the entity, therefore SaveChanges is sufficient.
      var rows = await unitOfWork.SaveAllChangesAsync(nameof(PersonUcUpdate), ct);

      logger.LogInformation(
         "Person updated: personId={PersonId} rows={Rows}",
         person.Id,
         rows
      );

      return Result<PersonDto>.Success(person.ToPersonDto());
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Ein Update lädt zunächst die vorhandene Entität und behandelt NotFound als
 *   erwartbaren fachlichen Fehler.
 * - Die Entität schützt ihre Invarianten selbst; der UseCase koordiniert nur.
 * - Durch EF-Core-Tracking ist kein explizites repository.Update(...) nötig.
 */
