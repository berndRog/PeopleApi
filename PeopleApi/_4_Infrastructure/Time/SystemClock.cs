using PeopleApi._2_BuildingBlocks._1_Ports;

namespace PeopleApi._4_Infrastructure.Time;

public sealed class SystemClock : IClock {
   public DateTime UtcNow => DateTime.UtcNow;
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Nur die Infrastruktur liest die Systemzeit.
 * - DateTime.UtcNow liefert einen DateTime mit DateTimeKind.Utc.
 */
