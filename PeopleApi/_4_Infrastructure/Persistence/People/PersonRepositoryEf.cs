using Microsoft.EntityFrameworkCore;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._3_Core.People._3_Domain.Entities;

namespace PeopleApi._4_Infrastructure.Persistence.People;

internal sealed class PersonRepositoryEf(
   IPersonDbContext dbContext
) : IPersonRepository {
   // Return a tracked entity because update/delete use cases may modify it.
   public Task<Person?> FindByIdAsync(Guid id, CancellationToken ct) =>
      dbContext.People.SingleOrDefaultAsync(person => person.Id == id, ct);

   // Use an efficient EXISTS query when only duplicate detection is needed.
   public Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct) =>
      dbContext.People.AnyAsync(person => person.Id == id, ct);

   // Changes are staged in the DbContext; UnitOfWork commits them later.
   public void Add(Person person) => dbContext.People.Add(person);
   public void Remove(Person person) => dbContext.People.Remove(person);
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Das Repository kapselt schreiborientierten Datenzugriff für Aggregate.
 * - Add/Remove speichern noch nicht sofort; SaveChanges erfolgt zentral über
 *   IUnitOfWork. Damit bleibt ein Anwendungsfall eine gemeinsame Transaktion.
 */
