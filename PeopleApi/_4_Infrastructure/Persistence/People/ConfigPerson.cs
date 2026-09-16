using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleApi._3_Core.People._3_Domain.Entities;
using PeopleApi._2_BuildingBlocks._3_Domain.ValueObjects;
using PeopleApi._4_Infrastructure.Persistence.Converters;

namespace PeopleApi._4_Infrastructure.Persistence.People;

internal sealed class ConfigPerson(
   UtcDateTimeConverter utcDateTimeConverter
) : IEntityTypeConfiguration<Person> {
   public void Configure(EntityTypeBuilder<Person> builder) {
      // Map the aggregate to a simple table named Person.
      builder.ToTable("Person");

      // IDs may originate from Android, therefore EF must not generate them.
      builder.HasKey(person => person.Id);
      builder.Property(person => person.Id).ValueGeneratedNever();

      // Mirror relevant domain length constraints in the database schema.
      builder.Property(person => person.FirstName).HasMaxLength(Person.NameMaxLength).IsRequired();
      builder.Property(person => person.LastName).HasMaxLength(Person.NameMaxLength).IsRequired();
      builder.Property(person => person.EmailVo)
         .HasConversion(
            valueObject => valueObject!.Value,
            value => EmailVo.FromPersisted(value)
         )
         .HasColumnName("Email")
         .HasMaxLength(254);
      builder.Property(person => person.PhoneVo)
         .HasConversion(
            valueObject => valueObject!.Value,
            value => PhoneVo.FromPersisted(value)
         )
         .HasColumnName("Phone")
         .HasMaxLength(16);
      builder.Property(person => person.ImageUrl).HasMaxLength(Person.ImageUrlMaxLength);
      builder.Property(person => person.CreatedAt)
         .HasConversion(utcDateTimeConverter)
         .IsRequired();
      builder.Property(person => person.UpdatedAt)
         .HasConversion(utcDateTimeConverter)
         .IsRequired();

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
 * - Value Objects werden über Konverter als einfache Textspalten gespeichert.
 * - CreatedAt und UpdatedAt bleiben DateTime-Werte mit DateTimeKind.Utc.
 * - ImageUrl ist lediglich eine String-Spalte ohne Foreign Key oder Dateizugriff.
 */
