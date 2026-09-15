using Microsoft.EntityFrameworkCore;
using PeopleApi._3_Core.People._3_Domain.Entities;
using PeopleApi._4_Infrastructure.Persistence.People;

namespace PeopleApi._4_Infrastructure.Persistence.Database;

public sealed class AppDbContext(
   DbContextOptions<AppDbContext> options
) : DbContext(options) {
   // The database contains only the People aggregate.
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
 * - ImageUrl ist eine normale nullable String-Eigenschaft von Person und benötigt
 *   weder ein DbSet<Image> noch eine Beziehung.
 * - IEntityTypeConfiguration hält EF-Core-Details aus der Domain-Entität heraus.
 */
