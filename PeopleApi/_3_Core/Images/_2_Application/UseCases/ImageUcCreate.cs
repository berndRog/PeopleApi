using PeopleApi._2_BuildingBlocks;
using PeopleApi._3_Core.Images._1_Ports;
using PeopleApi._3_Core.Images._2_Application.Dtos;

namespace PeopleApi._3_Core.Images._2_Application.UseCases;

public sealed class ImageUcCreate(
   IImageFileStorage fileStorage,
   ILogger<ImageUcCreate> logger
) {
   public async Task<Result<ImageDto>> ExecuteAsync(
      ImageUpload upload,
      CancellationToken ct
   ) {
      // Delegate validation and physical file writing to the storage port.
      var result = await fileStorage.StoreAsync(upload, ct);

      // Log successful resource creation without exposing physical file paths.
      if (result.IsSuccess) {
         logger.LogInformation(
            "Image file created: fileName={FileName}",
            result.Value.FileName
         );
      }

      return result;
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Der UseCase beschreibt den Anwendungsfall "Bild anlegen", ohne selbst
 *   FileStream oder Verzeichnisse zu kennen.
 * - IImageFileStorage trennt Application-Logik von konkretem Datei-I/O.
 * - Logging erfolgt auf UseCase-Ebene nur für erfolgreich abgeschlossene Aktionen.
 */
