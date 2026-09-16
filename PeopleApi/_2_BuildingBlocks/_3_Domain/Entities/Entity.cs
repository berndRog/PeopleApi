namespace PeopleApi._2_BuildingBlocks._3_Domain.Entities;

// Base class for entities whose identity remains stable while their state changes.
public abstract class Entity : IEquatable<Entity> {
   public Guid Id { get; protected set; }

   public override bool Equals(object? obj) {
      if (obj is not Entity other)
         return false;
      if (ReferenceEquals(this, other))
         return true;
      if (Id == Guid.Empty || other.Id == Guid.Empty)
         return false;
      return Id == other.Id;
   }

   public bool Equals(Entity? other) => Equals((object?)other);

   public override int GetHashCode() => Id.GetHashCode();

   public static bool operator ==(Entity? left, Entity? right) {
      if (left is null && right is null) return true;
      if (left is null || right is null) return false;
      return left.Equals(right);
   }

   public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Eine Entity wird über ihre Id und nicht über alle Eigenschaftswerte erkannt.
 * - Zwei noch nicht persistierte Entities mit Guid.Empty gelten nicht als gleich.
 * - Die gemeinsame Basisklasse hält diese Identitätssemantik aus Person heraus.
 */
