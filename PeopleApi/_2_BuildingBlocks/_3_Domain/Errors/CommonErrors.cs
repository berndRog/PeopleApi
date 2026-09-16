using PeopleApi._2_BuildingBlocks._3_Domain.Enums;

namespace PeopleApi._2_BuildingBlocks._3_Domain.Errors;

public static class CommonErrors {
   public static readonly DomainError InvalidEmail =
      new("common.email_invalid", "Email address is invalid.", WebErrorStatus.BadRequest);

   public static readonly DomainError InvalidPhone =
      new("common.phone_invalid", "Phone number is invalid.", WebErrorStatus.BadRequest);
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Fehler gemeinsam verwendeter Value Objects gehören zu den Building Blocks.
 * - Fachspezifische Person-Fehler bleiben dagegen im People-Modul.
 */
