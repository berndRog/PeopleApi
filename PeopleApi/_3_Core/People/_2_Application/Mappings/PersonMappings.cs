using PeopleApi._3_Core.People._2_Application.Dtos;
using PeopleApi._3_Core.People._3_Domain.Entities;

namespace PeopleApi._3_Core.People._2_Application.Mappings;

public static class PersonMappings {
   // Keep entity-to-DTO conversion in one reusable place.
   public static PersonDto ToPersonDto(this Person person) =>
      new(
         person.Id,
         person.FirstName,
         person.LastName,
         person.Email,
         person.Phone,
         person.ImageUrl
      );
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Mapping trennt interne Domain-Objekte von extern verwendeten DTOs.
 * - Eine zentrale Extension Method vermeidet duplizierten Mapping-Code.
 */
