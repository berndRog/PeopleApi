using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleApi._3_Core.People._3_Domain.Entities;

namespace PeopleApi._4_Infrastructure.Persistence.People;

internal sealed class ConfigPerson : IEntityTypeConfiguration<Person> {
   public void Configure(EntityTypeBuilder<Person> builder) {
      // Map the aggregate to a simple table named Person.
      builder.ToTable("Person");

      // IDs may originate from Android, therefore EF must not generate them.
      builder.HasKey(person => person.Id);
      builder.Property(person => person.Id).ValueGeneratedNever();

      // Mirror relevant domain length constraints in the database schema.
      builder.Property(person => person.FirstName).HasMaxLength(Person.NameMaxLength).IsRequired();
      builder.Property(person => person.LastName).HasMaxLength(Person.NameMaxLength).IsRequired();
      builder.Property(person => person.Email).HasMaxLength(254);
      builder.Property(person => person.Phone).HasMaxLength(50);
      builder.Property(person => person.ImageUrl).HasMaxLength(Person.ImageUrlMaxLength);

      // FullName is derived from other values and must not become a column.
      builder.Ignore(person => person.FullName);

      // The list endpoint sorts by last name, so an index supports that access path.
      builder.HasIndex(person => person.LastName);
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Fluent Configuration beschreibt nur relationale Persistenzdetails.
 * - Domänenregeln und Datenbankschema ergänzen sich, sind aber nicht dasselbe.
 * - ImageUrl ist lediglich eine String-Spalte; es gibt keinen Foreign Key zu Images.
 */
