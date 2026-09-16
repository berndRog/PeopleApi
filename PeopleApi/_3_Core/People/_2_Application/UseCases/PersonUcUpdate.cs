using PeopleApi._2_BuildingBlocks;
using PeopleApi._2_BuildingBlocks._1_Ports;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._3_Core.People._2_Application.Dtos;
using PeopleApi._3_Core.People._2_Application.Mappings;
using PeopleApi._3_Core.People._3_Domain.Errors;
using PeopleApi._2_BuildingBlocks._3_Domain.ValueObjects;

namespace PeopleApi._3_Core.People._2_Application.UseCases;

public sealed class PersonUcUpdate(
   IPersonRepository repository,
   IUnitOfWork unitOfWork,
   IClock clock,
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

      EmailVo? emailVo = null;
      if (!string.IsNullOrWhiteSpace(dto.Email)) {
         var resultEmail = EmailVo.Create(dto.Email);
         if (resultEmail.IsFailure)
            return Result<PersonDto>.Failure(resultEmail.Error);
         emailVo = resultEmail.Value;
      }

      PhoneVo? phoneVo = null;
      if (!string.IsNullOrWhiteSpace(dto.Phone)) {
         var resultPhone = PhoneVo.Create(dto.Phone);
         if (resultPhone.IsFailure)
            return Result<PersonDto>.Failure(resultPhone.Error);
         phoneVo = resultPhone.Value;
      }

      var resultUpdate = person.Update(
         dto.FirstName,
         dto.LastName,
         emailVo,
         phoneVo,
         dto.ImageUrl,
         clock.UtcNow
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
