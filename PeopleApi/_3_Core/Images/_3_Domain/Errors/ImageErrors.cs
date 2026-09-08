using PeopleApi._2_BuildingBlocks._3_Domain.Enums;
using PeopleApi._2_BuildingBlocks._3_Domain.Errors;

namespace PeopleApi._3_Core.Images._3_Domain.Errors;

// Central catalog of expected image errors.
public static class ImageErrors {
   public static readonly DomainError ImageRequired =
      new("image.required", "An image file is required.", WebErrorStatus.BadRequest);

   public static readonly DomainError ImageTooLarge =
      new("image.too_large", "The image file is too large.", WebErrorStatus.BadRequest);

   public static readonly DomainError ImageTypeUnsupported =
      new("image.type_unsupported", "The image type is not supported.", WebErrorStatus.BadRequest);

   public static readonly DomainError ImageNotFound =
      new("image.not_found", "The image was not found.", WebErrorStatus.NotFound);

   public static readonly DomainError ImageStorageFailed =
      new("image.storage_failed", "The image could not be stored or read.", WebErrorStatus.InternalServerError);
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Erwartbare Fehler besitzen feste Codes und werden nicht als Exceptions benutzt.
 * - Storage-Fehler werden getrennt von Validierungs- und NotFound-Fehlern modelliert.
 */
