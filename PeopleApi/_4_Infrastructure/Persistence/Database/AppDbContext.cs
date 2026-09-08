using Microsoft.EntityFrameworkCore;
using PeopleApi._3_Core.People._3_Domain.Entities;
using PeopleApi._4_Infrastructure.Persistence.People;

namespace PeopleApi._4_Infrastructure.Persistence.Database;

public sealed class AppDbContext(
   DbContextOptions<AppDbContext> options
) : DbContext(options) {
   // The database contains People only. Images intentionally have no DbSet.
   public DbSet<Person> People => Set<Person>();

   protected override void OnModelCreating(ModelBuilder modelBuilder) {
      // Keep persistence mapping outside the domain entity.
      modelBuilder.ApplyConfiguration(new ConfigPerson());
      base.OnModelCreating(modelBuilder);
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - AppDbContext bildet ausschließlich die relational persistierten Ressourcen ab.
 * - Dass hier kein DbSet<Image> existiert, ist eine bewusste Architekturentscheidung:
 *   Images werden ausschließlich als Dateien gespeichert.
 * - IEntityTypeConfiguration hält EF-Core-Details aus der Domain-Entität heraus.
 */
