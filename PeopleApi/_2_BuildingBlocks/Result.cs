using PeopleApi._2_BuildingBlocks._3_Domain.Errors;

namespace PeopleApi._2_BuildingBlocks;

// Result<T> represents an expected success/failure outcome without using
// exceptions for ordinary validation, conflict or not-found situations.
public sealed class Result<T> {
   public bool IsFailure { get; }
   public bool IsSuccess => !IsFailure;
   public DomainError Error { get; }
   public T Value { get; }

   private Result(bool isFailure, T value, DomainError error) {
      IsFailure = isFailure;
      Value = value;
      Error = error;
   }

   // A successful generic Result always carries a value and DomainError.None.
   public static Result<T> Success(T value) =>
      new(false, value, DomainError.None);

   public static Result<T> Failure(DomainError error) {
      // Prevent invalid failure objects that contain no actual error.
      if (error == DomainError.None)
         throw new ArgumentException("Failure requires a real domain error.", nameof(error));

      return new(true, default!, error);
   }
}

// Non-generic Result is used for commands such as DELETE that have no return value.
public sealed class Result {
   public bool IsSuccess { get; }
   public bool IsFailure => !IsSuccess;
   public DomainError Error { get; }

   private Result(bool isSuccess, DomainError error) {
      IsSuccess = isSuccess;
      Error = error;
   }

   public static Result Success() =>
      new(true, DomainError.None);

   public static Result Failure(DomainError error) {
      if (error == DomainError.None)
         throw new ArgumentException("Failure requires a real domain error.", nameof(error));

      return new(false, error);
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Result macht erwartbare Fehler zu normalen Rückgabewerten statt Exceptions.
 * - Dadurch können UseCases ihre Fehler explizit an den Controller weiterreichen.
 * - Der Controller entscheidet anschließend, welcher HTTP-Status dazu gehört.
 */
