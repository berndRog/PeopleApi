namespace PeopleApi._3_Core.People._2_Application.Dtos;

// JSON DTO used for People requests and responses.
public sealed record PersonDto(
   Guid Id,
   string FirstName,
   string LastName,
   string? Email,
   string? Phone,
   string? ImageUrl
);

/*
 * Lernziele und Didaktik
 * ----------------------
 * - DTOs verhindern, dass die Domain-Entität direkt zum HTTP-Vertragsmodell wird.
 * - Das erleichtert spätere Änderungen an Domain oder API unabhängig voneinander.
 * - ImageUrl enthält nur Text; Bilddaten gehören nicht zu diesem Vertrag.
 */
