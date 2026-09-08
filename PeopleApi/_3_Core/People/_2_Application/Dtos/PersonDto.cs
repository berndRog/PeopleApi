namespace PeopleApi._3_Core.People._2_Application.Dtos;

// DTO returned to clients for People read/write responses.
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
 */
