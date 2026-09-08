using PeopleApi._2_BuildingBlocks;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._3_Core.People._2_Application.Dtos;

namespace PeopleApi._3_Core.People._2_Application.UseCases;

public sealed class PersonUseCases(
   PersonUcCreate create,
   PersonUcUpdate update,
   PersonUcDelete delete
) : IPersonUseCases {
   // Delegate each operation to one focused use-case class.
   public Task<Result<PersonDto>> CreateAsync(PersonCreateData dto, CancellationToken ct) =>
      create.ExecuteAsync(dto, ct);

   public Task<Result<PersonDto>> UpdateAsync(Guid id, PersonUpdateData dto, CancellationToken ct) =>
      update.ExecuteAsync(id, dto, ct);

   public Task<Result> DeleteAsync(Guid id, CancellationToken ct) =>
      delete.ExecuteAsync(id, ct);
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Die Fassade reduziert die Zahl der Abhängigkeiten des Controllers.
 * - Einzelne UseCases bleiben trotzdem klein, fokussiert und separat testbar.
 */
