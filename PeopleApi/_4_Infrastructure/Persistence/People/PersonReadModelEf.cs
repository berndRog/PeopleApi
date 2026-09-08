using Microsoft.EntityFrameworkCore;
using PeopleApi._2_BuildingBlocks;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._3_Core.People._2_Application.Dtos;
using PeopleApi._3_Core.People._2_Application.Mappings;
using PeopleApi._3_Core.People._3_Domain.Errors;

namespace PeopleApi._4_Infrastructure.Persistence.People;

internal sealed class PersonReadModelEf(
   IPersonDbContext dbContext
) : IPersonReadModel {
   public async Task<Result<IReadOnlyList<PersonDto>>> SelectAllAsync(
      CancellationToken ct
   ) {
      // Read-only queries do not need EF Core change tracking.
      var people = await dbContext.People
         .AsNoTracking()
         .OrderBy(person => person.LastName)
         .ThenBy(person => person.FirstName)
         .ToListAsync(ct);

      // Map persistence/domain objects to the DTOs exposed by the application.
      IReadOnlyList<PersonDto> dtos =
         people.Select(person => person.ToPersonDto()).ToList();

      return Result<IReadOnlyList<PersonDto>>.Success(dtos);
   }

   public async Task<Result<PersonDto>> FindByIdAsync(
      Guid id,
      CancellationToken ct
   ) {
      // SingleOrDefault expresses the expected uniqueness of the primary key.
      var person = await dbContext.People
         .AsNoTracking()
         .SingleOrDefaultAsync(item => item.Id == id, ct);

      // A missing row is represented as a domain/application Result failure.
      return person is null
         ? Result<PersonDto>.Failure(PersonErrors.PersonNotFound)
         : Result<PersonDto>.Success(person.ToPersonDto());
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Das ReadModel kapselt lesende EF-Core-Abfragen getrennt von schreibenden
 *   Repository-Operationen.
 * - AsNoTracking reduziert unnötiges Tracking bei reinen GET-Anfragen.
 * - Die Web-Schicht erhält DTOs und keine EF-Core-Entitäten.
 */
