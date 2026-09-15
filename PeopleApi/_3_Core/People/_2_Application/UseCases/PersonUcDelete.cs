using PeopleApi._2_BuildingBlocks;
using PeopleApi._2_BuildingBlocks._1_Ports;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._3_Core.People._3_Domain.Errors;

namespace PeopleApi._3_Core.People._2_Application.UseCases;

public sealed class PersonUcDelete(
   IPersonRepository repository,
   IUnitOfWork unitOfWork,
   ILogger<PersonUcDelete> logger
) {
   public async Task<Result> ExecuteAsync(
      Guid id,
      CancellationToken ct
   ) {
      var person = await repository.FindByIdAsync(id, ct);
      if (person is null)
         return Result.Failure(PersonErrors.PersonNotFound);

      repository.Remove(person);
      var rows = await unitOfWork.SaveAllChangesAsync(nameof(PersonUcDelete), ct);

      logger.LogInformation(
         "Person deleted: personId={PersonId} rows={Rows}",
         id,
         rows
      );

      return Result.Success();
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Delete entfernt ausschließlich den Person-Datensatz.
 * - Ein eventuell gespeicherter ImageUrl-String besitzt keinen serverseitigen
 *   Datei-Lebenszyklus und benötigt deshalb keine zusätzliche Bereinigung.
 */
