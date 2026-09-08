using Microsoft.Extensions.Options;
using PeopleApi._2_BuildingBlocks;
using PeopleApi._3_Core.Images._1_Ports;
using PeopleApi._3_Core.Images._2_Application.Dtos;
using PeopleApi._3_Core.Images._3_Domain.Errors;

namespace PeopleApi._4_Infrastructure.Storage;

// Stores image resources exclusively as files. No database is involved.
internal sealed class ImageFileStorageFs : IImageFileStorage {
   // Keep the accepted extensions and MIME types in one whitelist. The first MIME
   // type of each entry is used as the canonical response Content-Type.
   private static readonly Dictionary<string, string[]> AllowedTypes =
      new(StringComparer.OrdinalIgnoreCase) {
         [".jpg"] = ["image/jpeg", "image/jpg"],
         [".jpeg"] = ["image/jpeg", "image/jpg"],
         [".png"] = ["image/png"],
         [".webp"] = ["image/webp"]
      };

   private readonly string _directory;
   private readonly long _maxFileSizeBytes;
   private readonly ILogger<ImageFileStorageFs> _logger;

   public ImageFileStorageFs(
      IHostEnvironment environment,
      IOptions<ImageStorageOptions> options,
      ILogger<ImageFileStorageFs> logger
   ) {
      var storageOptions = options.Value;

      // Resolve relative paths below the application's content root. Tests can
      // provide an absolute temporary path without changing production code.
      _directory = Path.IsPathRooted(storageOptions.Directory)
         ? storageOptions.Directory
         : Path.Combine(environment.ContentRootPath, storageOptions.Directory);

      _maxFileSizeBytes = storageOptions.MaxFileSizeBytes;
      _logger = logger;

      // Ensure the target folder exists before the first upload arrives.
      Directory.CreateDirectory(_directory);
   }

   public async Task<Result<ImageDto>> StoreAsync(
      ImageUpload upload,
      CancellationToken ct
   ) {
      // Reject missing or empty uploads before touching the file system.
      if (upload.Length <= 0)
         return Result<ImageDto>.Failure(ImageErrors.ImageRequired);

      // Enforce the configured upload size limit.
      if (upload.Length > _maxFileSizeBytes)
         return Result<ImageDto>.Failure(ImageErrors.ImageTooLarge);

      // Validate extension and MIME type against the small teaching whitelist.
      var extension = Path.GetExtension(upload.FileName).ToLowerInvariant();
      if (!AllowedTypes.TryGetValue(extension, out var contentTypes) ||
          !contentTypes.Contains(upload.ContentType, StringComparer.OrdinalIgnoreCase)) {
         return Result<ImageDto>.Failure(ImageErrors.ImageTypeUnsupported);
      }

      var contentType = contentTypes[0];

      // Never trust or reuse a client file name as the stored resource name.
      // A generated Guid avoids collisions and removes path information.
      var fileName = $"{Guid.NewGuid():N}{extension}";
      var filePath = Path.Combine(_directory, fileName);

      try {
         // CreateNew protects against accidentally overwriting an existing file.
         await using var target = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous
         );

         // Stream the request body to disk and respect request cancellation.
         await upload.Content.CopyToAsync(target, ct);

         // Return metadata only; the physical path is an infrastructure detail.
         return Result<ImageDto>.Success(
            new ImageDto(
               fileName,
               contentType,
               upload.Length,
               Path.GetFileName(upload.FileName)
            )
         );
      }
      catch (Exception exception) when (
         exception is IOException or UnauthorizedAccessException
      ) {
         _logger.LogError(exception, "Could not store image file {FileName}", fileName);
         return Result<ImageDto>.Failure(ImageErrors.ImageStorageFailed);
      }
   }

   public Task<Result<ImageFile>> OpenReadAsync(
      string fileName,
      CancellationToken ct
   ) {
      // Stop early when the HTTP request was already cancelled.
      ct.ThrowIfCancellationRequested();

      // Accept only a plain file name with a supported extension. This prevents
      // path traversal attempts such as ../../some-file.
      if (!TryGetSafeFile(fileName, out var safeFileName, out var filePath, out var contentType))
         return Task.FromResult(Result<ImageFile>.Failure(ImageErrors.ImageNotFound));

      if (!File.Exists(filePath))
         return Task.FromResult(Result<ImageFile>.Failure(ImageErrors.ImageNotFound));

      try {
         // Keep the stream open for ASP.NET Core; FileResult disposes it after
         // the response has been sent.
         Stream stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan
         );

         return Task.FromResult(
            Result<ImageFile>.Success(new ImageFile(stream, contentType))
         );
      }
      catch (Exception exception) when (
         exception is IOException or UnauthorizedAccessException
      ) {
         _logger.LogError(exception, "Could not open image file {FileName}", safeFileName);
         return Task.FromResult(Result<ImageFile>.Failure(ImageErrors.ImageStorageFailed));
      }
   }

   public Task<Result> DeleteAsync(
      string fileName,
      CancellationToken ct
   ) {
      ct.ThrowIfCancellationRequested();

      // Apply the same file-name validation used by reads.
      if (!TryGetSafeFile(fileName, out var safeFileName, out var filePath, out _))
         return Task.FromResult(Result.Failure(ImageErrors.ImageNotFound));

      if (!File.Exists(filePath))
         return Task.FromResult(Result.Failure(ImageErrors.ImageNotFound));

      try {
         // Deleting the file deletes the complete image resource because no
         // additional image metadata is stored in a database.
         File.Delete(filePath);
         return Task.FromResult(Result.Success());
      }
      catch (Exception exception) when (
         exception is IOException or UnauthorizedAccessException
      ) {
         _logger.LogError(exception, "Could not delete image file {FileName}", safeFileName);
         return Task.FromResult(Result.Failure(ImageErrors.ImageStorageFailed));
      }
   }

   private bool TryGetSafeFile(
      string fileName,
      out string safeFileName,
      out string filePath,
      out string contentType
   ) {
      // Strip directory components and verify that nothing changed. A mismatch
      // means the caller tried to pass a path instead of a resource file name.
      safeFileName = Path.GetFileName(fileName);
      filePath = string.Empty;
      contentType = string.Empty;

      if (!string.Equals(fileName, safeFileName, StringComparison.Ordinal) ||
          string.IsNullOrWhiteSpace(safeFileName)) {
         return false;
      }

      // Only files with an extension known by this API can be opened/deleted.
      var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
      if (!AllowedTypes.TryGetValue(extension, out var contentTypes))
         return false;

      contentType = contentTypes[0];
      filePath = Path.Combine(_directory, safeFileName);
      return true;
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Diese Klasse ist der konkrete Adapter für Datei-I/O und implementiert einen
 *   Port aus dem Core. Dadurch bleibt die Application-Schicht unabhängig vom OS.
 * - Bilder werden gestreamt und nicht vollständig in den Arbeitsspeicher geladen.
 * - Ein generierter Dateiname verhindert Kollisionen und entkoppelt den
 *   gespeicherten Namen vom Namen des Clients.
 * - Die Prüfung mit Path.GetFileName demonstriert eine einfache Abwehr gegen
 *   Path-Traversal. Zusätzlich begrenzen Whitelist und MaxFileSize den Upload.
 * - Da keine Image-Datenbank existiert, entspricht Löschen der Datei dem Löschen
 *   der kompletten Image-Ressource.
 */
