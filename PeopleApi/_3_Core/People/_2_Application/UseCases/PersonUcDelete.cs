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
      // Find the tracked entity that should be removed.
      var person = await repository.FindByIdAsync(id, ct);
      if (person is null)
         return Result.Failure(PersonErrors.PersonNotFound);

      // Mark the People resource for deletion and commit the change.
      repository.Remove(person);
      var rows = await unitOfWork.SaveAllChangesAsync(nameof(PersonUcDelete), ct);

      // This focused People use case deletes only the database entity. The Web
      // orchestration cleans up a locally managed image after this commit succeeds.
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
 * - Dieser UseCase betrifft ausschließlich die persistierte People-Ressource.
 * - Datei-I/O bleibt außerhalb des People-Core und wird von der Web-Schicht über
 *   die allgemeinen Image-UseCases orchestriert.
 * - Dadurch bleibt die Image-Infrastruktur wiederverwendbar, obwohl der Server
 *   beim fachlichen Delete anschließend das zugehörige lokale Bild aufräumt.
 */
