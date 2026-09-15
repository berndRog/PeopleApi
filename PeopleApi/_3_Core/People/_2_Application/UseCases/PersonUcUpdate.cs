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
      PersonDto dto,
      CancellationToken ct
   ) {
      var person = await repository.FindByIdAsync(id, ct);
      if (person is null)
         return Result<PersonDto>.Failure(PersonErrors.PersonNotFound);

      var resultUpdate = person.Update(
         dto.FirstName,
         dto.LastName,
         dto.Email,
         dto.Phone,
         dto.ImageUrl
      );
      if (resultUpdate.IsFailure)
         return Result<PersonDto>.Failure(resultUpdate.Error);

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
 * - Die Route-Id bestimmt, welche Person geändert wird.
 * - Alle editierbaren Eigenschaften einschließlich ImageUrl werden gemeinsam
 *   durch die Domain validiert und anschließend mit EF Core gespeichert.
 * - ImageUrl=null entfernt nur den gespeicherten String; keine Datei wird gelöscht.
 */
