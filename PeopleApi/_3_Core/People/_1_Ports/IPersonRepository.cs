using PeopleApi._3_Core.People._3_Domain.Entities;

namespace PeopleApi._3_Core.People._1_Ports;

// Write-oriented aggregate repository used by People use cases.
public interface IPersonRepository {
   Task<Person?> FindByIdAsync(Guid id, CancellationToken ct);
   Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct);
   void Add(Person person);
   void Remove(Person person);
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Das Repository arbeitet mit Domain-Entitäten und bereitet Änderungen vor.
 * - Der eigentliche Commit erfolgt separat über IUnitOfWork.
 */
