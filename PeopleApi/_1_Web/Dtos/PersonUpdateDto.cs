using Microsoft.AspNetCore.Http;

namespace PeopleApi._1_Web.Dtos;

/// <summary>
/// Data used to replace the editable state of an existing person.
/// </summary>
public sealed class PersonUpdateDto {
   /// <summary>Person's first name.</summary>
   public string FirstName { get; set; } = string.Empty;

   /// <summary>Person's last name.</summary>
   public string LastName { get; set; } = string.Empty;

   /// <summary>Optional email address.</summary>
   public string? Email { get; set; }

   /// <summary>Optional phone number.</summary>
   public string? Phone { get; set; }

   /// <summary>
   /// Optional replacement image. If supplied, the server stores it and removes the previously referenced local image after a successful update.
   /// </summary>
   public IFormFile? Image { get; set; }

   /// <summary>
   /// Set to true to remove the current image without uploading a replacement.
   /// </summary>
   public bool RemoveImage { get; set; }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Das Update unterscheidet drei Fälle: Bild beibehalten, Bild ersetzen und
 *   Bild entfernen.
 * - Image == null und RemoveImage == false bedeutet: vorhandenes Bild behalten.
 * - Image != null bedeutet: neues Bild speichern und altes nach erfolgreichem
 *   People-Update löschen.
 * - RemoveImage == true bedeutet: ImageUrl auf null setzen und altes Bild löschen.
 */
