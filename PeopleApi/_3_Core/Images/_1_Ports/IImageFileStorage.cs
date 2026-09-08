using PeopleApi._2_BuildingBlocks;
using PeopleApi._3_Core.Images._2_Application.Dtos;

namespace PeopleApi._3_Core.Images._1_Ports;

// Port for the physical persistence of image files.
public interface IImageFileStorage {
   Task<Result<ImageDto>> StoreAsync(
      ImageUpload upload,
      CancellationToken ct
   );

   Task<Result<ImageFile>> OpenReadAsync(
      string fileName,
      CancellationToken ct
   );

   Task<Result> DeleteAsync(
      string fileName,
      CancellationToken ct
   );
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Der Port beschreibt benötigte Dateioperationen ohne eine konkrete Technologie.
 * - Die aktuelle Implementierung nutzt das lokale Dateisystem; der Core bliebe bei
 *   einem späteren Wechsel zu anderem Storage unverändert.
 */
