using PeopleApi._2_BuildingBlocks._3_Domain.Enums;
using PeopleApi._2_BuildingBlocks._3_Domain.Errors;

namespace PeopleApi._3_Core.People._3_Domain.Errors;

// Central catalog of expected People errors.
public static class PersonErrors {
   public static readonly DomainError InvalidId =
      new("person.invalid_id", "The person id is invalid.", WebErrorStatus.BadRequest);

   public static readonly DomainError IdAlreadyExists =
      new("person.id_exists", "A person with this id already exists.", WebErrorStatus.Conflict);

   public static readonly DomainError PersonNotFound =
      new("person.not_found", "The person was not found.", WebErrorStatus.NotFound);

   public static readonly DomainError FirstNameTooShort =
      new("person.first_name_too_short", "First name is too short.", WebErrorStatus.BadRequest);

   public static readonly DomainError FirstNameTooLong =
      new("person.first_name_too_long", "First name is too long.", WebErrorStatus.BadRequest);

   public static readonly DomainError LastNameTooShort =
      new("person.last_name_too_short", "Last name is too short.", WebErrorStatus.BadRequest);

   public static readonly DomainError LastNameTooLong =
      new("person.last_name_too_long", "Last name is too long.", WebErrorStatus.BadRequest);

   public static readonly DomainError EmailInvalid =
      new("person.email_invalid", "Email address is invalid.", WebErrorStatus.BadRequest);

   public static readonly DomainError PhoneInvalid =
      new("person.phone_invalid", "Phone number is invalid.", WebErrorStatus.BadRequest);

   public static readonly DomainError ImageUrlInvalid =
      new("person.image_url_invalid", "Image URL must be an absolute HTTP or HTTPS URL.", WebErrorStatus.BadRequest);
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Feste Fehlerobjekte machen fachliche Fehlerfälle explizit und wiederverwendbar.
 * - Der Web-Status ist bereits kategorisiert, ohne ASP.NET-Core-Klassen zu referenzieren.
 */
