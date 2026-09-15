# PeopleApi

PeopleApi is the server for the introductory Android project
`A5_10_PeopleRetrofit`. It exposes JSON-based CRUD operations for people and
stores them with EF Core in SQLite.

This first Retrofit example deliberately contains no image upload, no
`multipart/form-data` and no server-side file storage. A person's `ImageUrl`
is simply a nullable string attribute.

## Learning goal

The project introduces one concern at a time:

1. Android sends and receives JSON with Retrofit.
2. A transport DTO is mapped to the domain model.
3. The API validates and persists a person.
4. Image transfer is added only in the later People Images example.

The distinction on Android is:

| Layer | Property | Meaning |
|---|---|---|
| Domain | `Person.imagePath` | Image reference used by the Android app |
| Retrofit DTO | `PersonDto.imageUrl` | Nullable string in the JSON contract |

PeopleApi stores `imageUrl` unchanged. It may contain a local Android file
path, a `content://` or `file://` URI, a remote URL, or `null`. Such a local
reference is meaningful only to the Android installation that created it.

## HTTP API

Base path:

```text
/peopleapi/v1
```

| Method | Endpoint | Result |
|---|---|---|
| GET | `/people/count` | Number of people |
| GET | `/people` | All people |
| GET | `/people/{id}` | One person |
| POST | `/people` | Create a person |
| PUT | `/people/{id}` | Replace editable properties |
| DELETE | `/people/{id}` | Delete a person |

POST and PUT use `Content-Type: application/json`.

Example:

```json
{
  "id": "01000000-0000-0000-0000-000000000000",
  "firstName": "Ada",
  "lastName": "Lovelace",
  "email": "ada.lovelace@example.org",
  "phone": "+49 30 123456",
  "imageUrl": "/data/user/0/de.rogallab.mobile/files/people510/ada.jpg"
}
```

No image bytes are contained in this request. Setting `imageUrl` to `null`
only clears the database column.

## Architecture

```text
Controller
    -> IPersonUseCases
    -> Person use case
    -> Person domain entity
    -> IPersonRepository / IUnitOfWork
    -> EF Core / SQLite
```

The main components are:

- `PeopleController`: translates HTTP requests and responses.
- `PersonDto`: JSON request and response model.
- `Person`: domain entity with validation and normalization.
- `PersonUcCreate`, `PersonUcUpdate`, `PersonUcDelete`: write use cases.
- `IPersonReadModel`: read queries.
- `PersonRepositoryEf` and `AppDbContext`: SQLite persistence.
- `DiRoot`, `DiPeople`, `DiInfrastructureModule`: dependency injection.

The database starts empty. The Android client calls `GET /people/count` and
can seed its examples through the normal POST endpoint.

## Run

The required SDK version is defined in `global.json`.

```bash
dotnet restore
dotnet run --project PeopleApi/PeopleApi.csproj
```

Swagger is available in the Development environment at:

```text
http://localhost:5080/swagger
```

The request examples are stored in:

```text
PeopleApi/_5_ApiTest/People.http
```

## Test

```bash
dotnet test PeopleApi.sln
```

The end-to-end tests use `WebApplicationFactory` and a separate temporary
SQLite database. They verify JSON CRUD, local image-reference strings,
validation, duplicate identifiers and not-found responses.

## Relation to PeopleMultipartApi

`PeopleMultipartApi` is the later variant. It adds image endpoints, file
storage and multipart orchestration. Keeping both repositories separate makes
the learning progression explicit:

```text
PeopleApi
    JSON person data
    ImageUrl as string

PeopleMultipartApi
    JSON/form data plus image bytes
    server-side image storage
```
