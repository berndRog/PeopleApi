namespace PeopleApi._4_Infrastructure.Storage;

// Configuration bound from the ImageStorage section in appsettings.json.
public sealed class ImageStorageOptions {
   // Relative paths are resolved below the application's content root.
   public string Directory { get; set; } = "App_Data/images";

   // Default upload limit: 5 MiB.
   public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Technische Speicherparameter gehören in Konfiguration statt in UseCases.
 * - Tests können Directory und MaxFileSizeBytes gezielt überschreiben.
 */
