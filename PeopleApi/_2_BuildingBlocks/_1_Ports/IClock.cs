namespace PeopleApi._2_BuildingBlocks._1_Ports;

public interface IClock {
   DateTime UtcNow { get; }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - IClock macht zeitabhängige UseCases deterministisch testbar.
 * - Der Name UtcNow macht den erwarteten DateTimeKind explizit.
 */
