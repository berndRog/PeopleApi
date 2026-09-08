using PeopleApi._2_BuildingBlocks;
using PeopleApi._3_Core.Images._1_Ports;

namespace PeopleApi._3_Core.Images._2_Application.UseCases;

public sealed class ImageUcDelete(
   IImageFileStorage fileStorage,
   ILogger<ImageUcDelete> logger
) {
   public async Task<Result> ExecuteAsync(
      string fileName,
      CancellationToken ct
   ) {
      // Ask the storage adapter to remove the physical image resource.
      var result = await fileStorage.DeleteAsync(fileName, ct);

      if (result.IsSuccess) {
         logger.LogInformation(
            "Image file deleted: fileName={FileName}",
            fileName
         );
      }

      return result;
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Ein Image wird ausschließlich über seinen serverseitigen Dateinamen adressiert.
 * - Ohne Image-Datenbank besteht der Delete-Anwendungsfall vollständig aus dem
 *   Löschen der Datei über den abstrahierten Storage-Port.
 */
