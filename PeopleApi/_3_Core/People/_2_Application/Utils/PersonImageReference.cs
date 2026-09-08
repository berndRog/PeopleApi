namespace PeopleApi._3_Core.People._2_Application.Utils;

// Helper for URLs that reference image files managed by this API.
internal static class PersonImageReference {
   public static string BuildUrl(
      string imageBaseUrl,
      string fileName
   ) => $"{imageBaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(fileName)}";

   public static bool TryGetManagedFileName(
      string? imageUrl,
      string imageBaseUrl,
      out string fileName
   ) {
      fileName = string.Empty;

      if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var imageUri) ||
          !Uri.TryCreate(imageBaseUrl, UriKind.Absolute, out var baseUri)) {
         return false;
      }

      // Compare the API path, but intentionally not the host. A Person may have
      // been created via localhost and later updated through a WLAN address.
      var basePath = baseUri.AbsolutePath.TrimEnd('/');
      var imagePath = imageUri.AbsolutePath;
      var prefix = $"{basePath}/";

      if (!imagePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
         return false;

      // A managed image URL has exactly one file-name segment after /images.
      var relativePath = imagePath[prefix.Length..];
      if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Contains('/'))
         return false;

      var candidate = Uri.UnescapeDataString(relativePath);
      if (!string.Equals(candidate, Path.GetFileName(candidate), StringComparison.Ordinal))
         return false;

      // ImageFileStorageFs creates Guid-based names with one supported extension.
      var extension = Path.GetExtension(candidate).ToLowerInvariant();
      var idPart = Path.GetFileNameWithoutExtension(candidate);
      var supportedExtension = extension is ".jpg" or ".jpeg" or ".png" or ".webp";

      if (!supportedExtension || !Guid.TryParseExact(idPart, "N", out _))
         return false;

      fileName = candidate;
      return true;
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Die Person speichert eine vollständige ImageUrl, während der Image-Storage
 *   intern nur mit einem serverseitig erzeugten Dateinamen arbeitet.
 * - BuildUrl verbindet beide Sichten, ohne HTTP-Typen wie HttpContext zu benötigen.
 * - Beim späteren Löschen wird nur eine URL akzeptiert, deren Pfad zur eigenen
 *   /images-Ressource und deren Dateiname zum serverseitigen Namensschema passt.
 * - Der Host wird bewusst nicht verglichen: localhost, 10.0.2.2 bzw. eine WLAN-IP
 *   können unterschiedliche Zugriffswege auf denselben Server darstellen.
 */
