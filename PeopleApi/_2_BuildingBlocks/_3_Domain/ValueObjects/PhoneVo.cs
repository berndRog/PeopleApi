using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.RegularExpressions;
using PeopleApi._2_BuildingBlocks._3_Domain.Errors;

namespace PeopleApi._2_BuildingBlocks._3_Domain.ValueObjects;

// Canonical representation: digits with an optional leading plus sign.
[ComplexType]
public sealed record PhoneVo {
   public string Value { get; private init; }

   private PhoneVo() => Value = default!;
   private PhoneVo(string value) => Value = value;

   public static Result<PhoneVo> Create(string? input) {
      var normalized = NormalizeFromInput(input);
      return normalized.IsFailure
         ? Result<PhoneVo>.Failure(normalized.Error)
         : Result<PhoneVo>.Success(new PhoneVo(normalized.Value));
   }

   public static PhoneVo FromPersisted(string value) {
      if (!IsCanonical(value))
         throw new InvalidOperationException($"Invalid phone in database: '{value}'");
      return new PhoneVo(value);
   }

   private static Result<string> NormalizeFromInput(string? input) {
      if (string.IsNullOrWhiteSpace(input))
         return Result<string>.Failure(CommonErrors.InvalidPhone);

      var number = input.Trim();
      if (!Allowed.IsMatch(number))
         return Result<string>.Failure(CommonErrors.InvalidPhone);

      if (number.StartsWith("00"))
         number = "+" + number[2..];

      var international = number.StartsWith('+');
      number = OptionalTrunkZero.Replace(number, string.Empty);
      var digits = Regex.Replace(number, @"\D", string.Empty);

      if (international)
         digits = RemoveTrunkZeroAfterCountryCode(digits);
      if (digits.Length is < 7 or > 15)
         return Result<string>.Failure(CommonErrors.InvalidPhone);

      return Result<string>.Success(international ? "+" + digits : digits);
   }

   private static string RemoveTrunkZeroAfterCountryCode(string digits) {
      ReadOnlySpan<string> countryCodes = ["49", "41", "43"];
      foreach (var countryCode in countryCodes) {
         if (digits.StartsWith(countryCode) &&
             digits.Length > countryCode.Length &&
             digits[countryCode.Length] == '0')
            return countryCode + digits[(countryCode.Length + 1)..];
      }
      return digits;
   }

   private static bool IsCanonical(string value) {
      if (string.IsNullOrWhiteSpace(value)) return false;
      var digits = value.AsSpan();
      if (digits[0] == '+') digits = digits[1..];
      if (digits.Length is < 7 or > 15) return false;
      foreach (var digit in digits)
         if (digit is < '0' or > '9')
            return false;
      return true;
   }

   public override string ToString() => Value.StartsWith('+')
      ? "+" + GroupFromRight(Value[1..])
      : GroupFromRight(Value);

   private static string GroupFromRight(string digits) {
      var result = new StringBuilder();
      var firstGroupLength = digits.Length % 4;
      if (firstGroupLength == 0) firstGroupLength = 4;
      result.Append(digits[..firstGroupLength]);
      for (var index = firstGroupLength; index < digits.Length; index += 4) {
         result.Append(' ');
         result.Append(digits.Substring(index, Math.Min(4, digits.Length - index)));
      }
      return result.ToString();
   }

   private static readonly Regex Allowed =
      new(@"^(?=.*\d)[0-9 +()/\-\.]{7,30}$", RegexOptions.Compiled);

   private static readonly Regex OptionalTrunkZero =
      new(@"\(\s*0\s*\)", RegexOptions.Compiled);
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - PhoneVo trennt die kanonische Speicherung von der formatierten Darstellung.
 * - Unterschiedlich formatierte Eingaben können dadurch zuverlässig verglichen werden.
 * - FromPersisted prüft die Invariante ohne Eingabedaten erneut zu normalisieren.
 */
