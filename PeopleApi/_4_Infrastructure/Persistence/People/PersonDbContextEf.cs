using Microsoft.EntityFrameworkCore;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._3_Core.People._3_Domain.Entities;
using PeopleApi._4_Infrastructure.Persistence.Database;

namespace PeopleApi._4_Infrastructure.Persistence.People;

internal sealed class PersonDbContextEf(
   AppDbContext dbContext
) : IPersonDbContext {
   // Expose only the People set required by the Core port.
   public DbSet<Person> People => dbContext.People;
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Der Adapter verbindet den allgemeinen AppDbContext mit dem People-spezifischen Port.
 * - Andere DbSets würden dadurch nicht automatisch Teil des People-Core-Vertrags.
 */
