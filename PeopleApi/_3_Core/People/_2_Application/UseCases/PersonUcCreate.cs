using PeopleApi._2_BuildingBlocks;
using PeopleApi._2_BuildingBlocks._1_Ports;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._3_Core.People._2_Application.Dtos;
using PeopleApi._3_Core.People._2_Application.Mappings;
using PeopleApi._3_Core.People._3_Domain.Entities;
using PeopleApi._3_Core.People._3_Domain.Errors;
using PeopleApi._2_BuildingBlocks._3_Domain.ValueObjects;

namespace PeopleApi._3_Core.People._2_Application.UseCases;

public sealed class PersonUcCreate(
   IPersonRepository repository,
   IUnitOfWork unitOfWork,
   IClock clock,
   ILogger<PersonUcCreate> logger
) {
   public async Task<Result<PersonDto>> ExecuteAsync(
      PersonDto dto,
      CancellationToken ct
   ) {
      // Android normally supplies the UUID. Empty keeps the endpoint usable for
      // other clients and lets the server generate one.
      var id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;

      if (await repository.ExistsByIdAsync(id, ct))
         return Result<PersonDto>.Failure(PersonErrors.IdAlreadyExists);

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

      var resultPerson = Person.Create(
         id,
         dto.FirstName,
         dto.LastName,
         emailVo,
         phoneVo,
         dto.ImageUrl,
         clock.UtcNow
      );
      if (resultPerson.IsFailure)
         return Result<PersonDto>.Failure(resultPerson.Error);

      var person = resultPerson.Value;
      repository.Add(person);
      var rows = await unitOfWork.SaveAllChangesAsync(nameof(PersonUcCreate), ct);

      logger.LogInformation(
         "Person created: personId={PersonId} rows={Rows}",
         person.Id,
         rows
      );

      return Result<PersonDto>.Success(person.ToPersonDto());
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Create prüft die Id, erzeugt eine gültige Domain-Entität und speichert sie.
 * - ImageUrl durchläuft denselben Weg wie Email und Phone und bleibt ein String.
 * - Es gibt keine Dateioperation und daher auch keine Kompensationslogik.
 */
