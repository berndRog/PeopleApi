using Microsoft.AspNetCore.Http;

namespace PeopleApi._1_Web.Dtos;

/// <summary>
/// Data required to create a person, optionally including a profile image.
/// </summary>
public sealed class PersonCreateDto {
   /// <summary>Person's first name.</summary>
   public string FirstName { get; set; } = string.Empty;

   /// <summary>Person's last name.</summary>
   public string LastName { get; set; } = string.Empty;

   /// <summary>Optional email address.</summary>
   public string? Email { get; set; }

   /// <summary>Optional phone number.</summary>
   public string? Phone { get; set; }

   /// <summary>
   /// Optional client-generated UUID. If omitted, the server creates a new UUID.
   /// </summary>
   public string? Id { get; set; }

   /// <summary>
   /// Optional image file. The server stores the file and writes the generated absolute URL into the person.
   /// </summary>
   public IFormFile? Image { get; set; }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Dieses DTO beschreibt den multipart/form-data-Request der WebAPI.
 * - IFormFile ist bewusst auf die Web-Schicht beschränkt und gelangt nicht in
 *   Domain oder Application.
 * - Der Client muss keine ImageUrl erzeugen und keinen separaten Upload
 *   orchestrieren; das übernimmt die API beim Anlegen der Person.
 */
