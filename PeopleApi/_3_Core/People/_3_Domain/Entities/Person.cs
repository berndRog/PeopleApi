using System.Text.RegularExpressions;
using PeopleApi._2_BuildingBlocks;
using PeopleApi._3_Core.People._3_Domain.Errors;

namespace PeopleApi._3_Core.People._3_Domain.Entities;

public sealed class Person {
   // Keep validation limits close to the domain object that enforces them.
   public const int NameMinLength = 2;
   public const int NameMaxLength = 50;
   public const int PhoneDigitsMin = 6;
   public const int PhoneDigitsMax = 15;
   public const int ImageUrlMaxLength = 2048;

   // The regex intentionally stays small for the teaching example. It verifies
   // the basic shape of an email address without trying to implement RFC 5322.
   private static readonly Regex EmailRegex =
      new(
         @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
         RegexOptions.Compiled | RegexOptions.CultureInvariant
      );

   // Setters are private so callers cannot bypass domain validation.
   public Guid Id { get; private set; }
   public string FirstName { get; private set; } = string.Empty;
   public string LastName { get; private set; } = string.Empty;
   public string? Email { get; private set; }
   public string? Phone { get; private set; }
   public string? ImageUrl { get; private set; }

   // Derived values are calculated from the current state and need no persistence.
   public string FullName => $"{FirstName} {LastName}".Trim();

   // EF Core needs a parameterless constructor when materializing an entity.
   private Person() {
   }

   private Person(
      Guid id,
      string firstName,
      string lastName,
      string? email,
      string? phone,
      string? imageUrl
   ) {
      Id = id;
      FirstName = firstName;
      LastName = lastName;
      Email = email;
      Phone = phone;
      ImageUrl = imageUrl;
   }

   public static Result<Person> Create(
      Guid id,
      string firstName,
      string lastName,
      string? email,
      string? phone,
      string? imageUrl
   ) {
      // Validate and normalize before an entity can enter the valid domain state.
      var validation = ValidateAndNormalize(firstName, lastName, email, phone, imageUrl);
      if (validation.IsFailure)
         return Result<Person>.Failure(validation.Error);

      // Construct the entity only from already normalized values.
      var values = validation.Value;
      return Result<Person>.Success(
         new Person(
            id,
            values.FirstName,
            values.LastName,
            values.Email,
            values.Phone,
            values.ImageUrl
         )
      );
   }

   public Result Update(
      string firstName,
      string lastName,
      string? email,
      string? phone,
      string? imageUrl
   ) {
      // Reuse the same rules for creation and later modification.
      var validation = ValidateAndNormalize(firstName, lastName, email, phone, imageUrl);
      if (validation.IsFailure)
         return Result.Failure(validation.Error);

      // Apply all values only after the complete input has passed validation.
      // This avoids leaving the entity in a partially updated state.
      var values = validation.Value;
      FirstName = values.FirstName;
      LastName = values.LastName;
      Email = values.Email;
      Phone = values.Phone;
      ImageUrl = values.ImageUrl;
      return Result.Success();
   }

   private static Result<NormalizedPerson> ValidateAndNormalize(
      string firstName,
      string lastName,
      string? email,
      string? phone,
      string? imageUrl
   ) {
      // Trim mandatory names and convert optional blank strings to null.
      var normalizedFirstName = firstName?.Trim() ?? string.Empty;
      var normalizedLastName = lastName?.Trim() ?? string.Empty;
      var normalizedEmail = NullIfBlank(email);
      var normalizedPhone = NullIfBlank(phone);
      var normalizedImageUrl = NullIfBlank(imageUrl);

      // Return the first violated business rule as a typed domain error.
      if (normalizedFirstName.Length < NameMinLength)
         return Result<NormalizedPerson>.Failure(PersonErrors.FirstNameTooShort);
      if (normalizedFirstName.Length > NameMaxLength)
         return Result<NormalizedPerson>.Failure(PersonErrors.FirstNameTooLong);
      if (normalizedLastName.Length < NameMinLength)
         return Result<NormalizedPerson>.Failure(PersonErrors.LastNameTooShort);
      if (normalizedLastName.Length > NameMaxLength)
         return Result<NormalizedPerson>.Failure(PersonErrors.LastNameTooLong);
      if (normalizedEmail is not null && !EmailRegex.IsMatch(normalizedEmail))
         return Result<NormalizedPerson>.Failure(PersonErrors.EmailInvalid);
      if (normalizedPhone is not null && !IsPhoneValid(normalizedPhone))
         return Result<NormalizedPerson>.Failure(PersonErrors.PhoneInvalid);
      if (normalizedImageUrl is not null && !IsImageUrlValid(normalizedImageUrl))
         return Result<NormalizedPerson>.Failure(PersonErrors.ImageUrlInvalid);

      return Result<NormalizedPerson>.Success(
         new NormalizedPerson(
            normalizedFirstName,
            normalizedLastName,
            normalizedEmail,
            normalizedPhone,
            normalizedImageUrl
         )
      );
   }

   private static bool IsPhoneValid(string phone) {
      // Permit common display separators but reject arbitrary letters/symbols.
      var hasAllowedCharacters = phone.All(character =>
         char.IsDigit(character) ||
         character == '+' || character == ' ' || character == '-' ||
         character == '/' || character == '.' || character == '(' || character == ')'
      );
      if (!hasAllowedCharacters)
         return false;

      // A plus sign is optional, but if present it must occur exactly once first.
      var plusCount = phone.Count(character => character == '+');
      var hasValidPlus = plusCount == 0 || (plusCount == 1 && phone.StartsWith('+'));
      if (!hasValidPlus)
         return false;

      // Count only digits when applying the minimum/maximum phone length.
      var digitCount = phone.Count(char.IsDigit);
      return digitCount is >= PhoneDigitsMin and <= PhoneDigitsMax;
   }

   private static bool IsImageUrlValid(string imageUrl) =>
      // Store only complete HTTP(S) URLs so clients such as Coil can consume
      // ImageUrl directly without reconstructing the server path.
      imageUrl.Length <= ImageUrlMaxLength &&
      Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) &&
      (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

   private static string? NullIfBlank(string? value) =>
      string.IsNullOrWhiteSpace(value) ? null : value.Trim();

   // The normalized record transports validated values internally only.
   private sealed record NormalizedPerson(
      string FirstName,
      string LastName,
      string? Email,
      string? Phone,
      string? ImageUrl
   );
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Die Entität schützt ihre Daten über private Setter und zentrale Fabrik-/
 *   Update-Methoden. Ungültige Zustände sollen gar nicht erst entstehen.
 * - Validierung und Normalisierung sind Domänenlogik und gehören nicht in den
 *   Controller oder in EF-Core-Konfigurationen.
 * - ImageUrl ist eine vollständige HTTP(S)-Referenz, aber keine Beziehung zu
 *   einer Image-Entity. Images besitzen einen unabhängigen REST-Lebenszyklus.
 * - Create und Update verwenden dieselbe Regelmenge, sodass sich die Regeln nicht
 *   zwischen verschiedenen Anwendungsfällen auseinanderentwickeln.
 */
