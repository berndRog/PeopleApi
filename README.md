# PeopleApi

ASP.NET Core Web API for the Android Mobile Systems examples.

The API exposes two reusable resources:

- `PeopleController` for `/people`
- `ImageController` for `/images`

Images are deliberately **not** stored in the database. EF Core/SQLite is used
only for People; image resources are physical files.

A `Person` stores only an optional, absolute `ImageUrl`. The same `/images` API
can later be reused for cars without introducing a second image implementation.

## Persistence

EF Core and SQLite are used **only for People**.

Images use **no database at all**. There is:

- no `ImageAsset` entity,
- no `DbSet<ImageAsset>`,
- no image table,
- no image repository based on EF Core,
- no foreign-key relationship from `Person` to an image.

The image file itself is the image resource. Files are stored in the directory
configured by `ImageStorage:Directory` (`App_Data/images` by default).

## Main endpoints

- `GET    /peopleapi/v1/people`
- `GET    /peopleapi/v1/people/count`
- `GET    /peopleapi/v1/people/{id}`
- `POST   /peopleapi/v1/people`
- `PUT    /peopleapi/v1/people/{id}`
- `DELETE /peopleapi/v1/people/{id}`
- `POST   /peopleapi/v1/images`
- `GET    /peopleapi/v1/images/{fileName}`
- `DELETE /peopleapi/v1/images/{fileName}`

`GET /peopleapi/v1/people/count` returns only the number of stored people. A
client can use this inexpensive query before optionally sending its own seed data.

## People CRUD with server-side image orchestration

The Android client no longer has to coordinate People and Images itself.
`POST /people` and `PUT /people/{id}` use `multipart/form-data` and can contain
an optional image file.

The `PeopleController` is only the HTTP adapter: it binds form/route data,
converts `IFormFile` into the web-neutral `ImageUpload` and creates HTTP responses.
The actual sequence of People persistence, image storage and compensation is
implemented by `PersonUcCreate`, `PersonUcUpdate` and `PersonUcDelete`.

### Create

`PersonCreateDto` contains the person data plus optional `Image`.

If no image is supplied:

```text
POST /people
   -> create Person
   -> ImageUrl = null
```

If an image is supplied:

```text
POST /people
   -> store image file
   -> create absolute ImageUrl
   -> create Person with ImageUrl
```

If the People operation fails after the image was stored, the create use case
removes the new file again. This avoids an orphaned image after a failed create.

The client therefore does **not** send `ImageUrl` during create.

### Update

`PersonUpdateDto` supports three explicit image states:

```text
Image == null, RemoveImage == false
-> keep the existing image

Image != null, RemoveImage == false
-> store replacement image
-> update Person with new ImageUrl
-> delete old local image after the DB update succeeded

Image == null, RemoveImage == true
-> update Person with ImageUrl = null
-> delete old local image after the DB update succeeded
```

`Image != null` together with `RemoveImage == true` is rejected with
`400 Bad Request` by the update use case.

The replacement order is intentional. The old image is deleted only after the
Person successfully references the new image. If the People update fails, the
newly uploaded file is removed again and the old image remains available.

### Delete

`DELETE /people/{id}` first deletes the People row. Only after the database
operation succeeds does the delete use case remove a locally managed profile image.

The People Application contains the orchestration but no concrete file-system
or EF-Core implementation. It depends on `IImageUseCases`, `IPersonRepository`
and `IUnitOfWork`; the adapters remain in Infrastructure.

## Standalone Image API

The separate `/images` endpoints remain available. They are useful whenever a
client wants to manage an image resource directly and can later be reused by
other resources such as Cars.

`POST /images` uses `multipart/form-data` with the form field `file`.
`IFormFile` is intentionally used **without** `[FromForm]` when it is the direct
action parameter. ASP.NET Core infers file binding from `IFormFile` itself, and
this avoids the Swashbuckle error caused by `[FromForm] IFormFile`.

The server does not trust the uploaded file name for storage. It creates a new
random Guid-based name and keeps only the supported extension, for example:

```text
8fc6b646421548cf88f252753cc54493.jpg
```

A standalone upload returns metadata and the complete URL:

```json
{
  "fileName": "8fc6b646421548cf88f252753cc54493.jpg",
  "imageUrl": "http://localhost:5080/peopleapi/v1/images/8fc6b646421548cf88f252753cc54493.jpg",
  "contentType": "image/jpeg",
  "length": 123456,
  "originalFileName": "person.jpg"
}
```

The URL is generated from the current HTTP request. If the API is accessed as
`http://192.168.178.42:5080`, the generated image URL uses that host as well.

## Swagger XML documentation

Both controllers contain XML documentation comments such as:

```csharp
/// <summary>...</summary>
/// <remarks>...</remarks>
/// <param name="...">...</param>
/// <response code="...">...</response>
```

The project enables XML documentation generation with:

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

`AddSwaggerForPeopleApi()` loads the generated XML file with
`IncludeXmlComments(...)`. Together with `ProducesResponseType`, Swagger shows
endpoint descriptions, parameter explanations and the expected HTTP status
codes directly in the UI.

## Android AVD and localhost

On the Android Emulator, `localhost` means the emulator itself. The host
computer is available through `10.0.2.2`. Only URLs whose host is exactly
`localhost` should therefore be adapted. WLAN IP addresses and real host names
must remain unchanged.

```kotlin
import java.net.URI

object ServerUrl {

   private const val AVD_HOST = "10.0.2.2"

   fun resolve(url: String?): String? {
      if (url == null) return null

      val uri = URI(url)

      if (uri.host != "localhost")
         return url

      return URI(
         uri.scheme,
         uri.userInfo,
         AVD_HOST,
         uri.port,
         uri.path,
         uri.query,
         uri.fragment
      ).toString()
   }
}
```

Examples:

```text
http://localhost:5080/peopleapi/v1/images/8fc6b646421548cf88f252753cc54493.jpg
-> http://10.0.2.2:5080/peopleapi/v1/images/8fc6b646421548cf88f252753cc54493.jpg

http://192.168.178.42:5080/peopleapi/v1/images/8fc6b646421548cf88f252753cc54493.jpg
-> unchanged

https://people.example.org/peopleapi/v1/images/8fc6b646421548cf88f252753cc54493.jpg
-> unchanged
```

Coil can use the resolved URL directly:

```kotlin
AsyncImage(
   model = ServerUrl.resolve(person.imageUrl),
   contentDescription = null
)
```

## Automated tests

The solution contains a separate `PeopleApiTest` project. The API tests use
`WebApplicationFactory<Program>` and follow Arrange - Act - Assert like the
CampusLibrary end-to-end tests.

Each test factory creates its own temporary resources:

- a separate SQLite database for People,
- a separate image directory for real file I/O.

The production database and `App_Data/images` directory are never used by the
automated tests.

### People tests

The People end-to-end tests cover among other things:

- GET all people,
- GET people count,
- POST without image,
- POST with image including physical file creation and generated ImageUrl,
- GET by id,
- PUT without image keeps the existing image,
- PUT with new image replaces the file and removes the old one,
- PUT with `RemoveImage=true` clears ImageUrl and removes the old file,
- invalid `Image + RemoveImage` -> 400,
- DELETE removes the Person and its locally managed image,
- unknown id -> 404,
- duplicate id -> 409 Conflict and compensating image cleanup.

### Image tests

`ImageControllerE2eT` covers:

- multipart upload and physical file creation,
- absolute `ImageUrl` generation,
- download with Content-Type and exact byte comparison,
- delete and physical file removal,
- GET/DELETE of a missing file -> 404,
- unsupported file type -> 400,
- file larger than the configured limit -> 400.

Run the complete suite with:

```text
dotnet test PeopleApi.sln
```

## Comment convention

The C# source follows the course convention:

- English inline comments explain the execution flow close to the code.
- Each source file ends with a German `Lernziele und Didaktik` block that
  summarizes the architectural or technical teaching points of that file.
- Public controller actions additionally use XML documentation comments so the
  same API explanations are visible in Swagger.
