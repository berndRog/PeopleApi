using Microsoft.EntityFrameworkCore;
using PeopleApi._3_Core.People._3_Domain.Entities;

namespace PeopleApi._3_Core.People._1_Ports;

// Minimal EF-facing port needed by repository and read model adapters.
public interface IPersonDbContext {
   DbSet<Person> People { get; }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Der Port begrenzt den für People benötigten Ausschnitt des DbContext.
 * - Für das Lehrbeispiel bleibt diese Abstraktion klein und gut nachvollziehbar.
 */
