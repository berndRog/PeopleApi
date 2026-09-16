using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Mail;
using PeopleApi._2_BuildingBlocks._3_Domain.Errors;

namespace PeopleApi._2_BuildingBlocks._3_Domain.ValueObjects;

// Canonical representation: trimmed and lower-case.
[ComplexType]
public sealed record EmailVo {
   public string Value { get; private init; }

   private EmailVo() => Value = default!;
   private EmailVo(string value) => Value = value;

   public static Result<EmailVo> Create(string? input) {
      var normalized = NormalizeFromInput(input);
      return normalized.IsFailure
         ? Result<EmailVo>.Failure(normalized.Error)
         : Result<EmailVo>.Success(new EmailVo(normalized.Value));
   }

   public static EmailVo FromPersisted(string value) {
      if (!IsCanonical(value))
         throw new InvalidOperationException($"Invalid email in database: '{value}'");
      return new EmailVo(value);
   }

   private static Result<string> NormalizeFromInput(string? input) {
      if (string.IsNullOrWhiteSpace(input))
         return Result<string>.Failure(CommonErrors.InvalidEmail);

      var email = input.Trim().ToLowerInvariant();
      if (email.Length > 254 || email.Contains(' '))
         return Result<string>.Failure(CommonErrors.InvalidEmail);

      var at = email.IndexOf('@');
      if (at <= 0 || at >= email.Length - 1 || !email[(at + 1)..].Contains('.'))
         return Result<string>.Failure(CommonErrors.InvalidEmail);

      try {
         _ = new MailAddress(email);
      }
      catch {
         return Result<string>.Failure(CommonErrors.InvalidEmail);
      }

      return Result<string>.Success(email);
   }

   private static bool IsCanonical(string value) {
      if (string.IsNullOrWhiteSpace(value) ||
          value != value.Trim() ||
          value != value.ToLowerInvariant() ||
          value.Length > 254 ||
          value.Contains(' '))
         return false;

      var at = value.IndexOf('@');
      return at > 0 && at < value.Length - 1;
   }

   public override string ToString() => Value;
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - EmailVo normalisiert und validiert eine E-Mail-Adresse genau einmal.
 * - Wertgleichheit entsteht automatisch durch record.
 * - FromPersisted normalisiert nicht heimlich, sondern erkennt beschädigte Daten.
 */
