# API Localization Middleware

Middleware that automatically localizes `problem+json` error responses based on the user's culture preferences.

## Features

- **Automatic Localization**: Intercepts all problem+json responses and translates error messages
- **Multi-Culture Support**: Supports en-US, es-ES, fr-FR, de-DE, ja-JP, zh-CN
- **Culture Detection**: Uses Accept-Language header, query string (`?culture=es-ES`), or cookie
- **Fallback**: Returns original message if no translation available
- **Culture Tracking**: Adds `culture` field to problem response extensions for debugging

## Configuration

Registered in `Program.cs`:

```csharp
// Service registration
builder.Services.AddApiLocalization();

// Middleware pipeline (before exception handler)
app.UseLocalization();
```

## Culture Priority

1. Query string: `?culture=es-ES`
2. Cookie: `.AspNetCore.Culture`
3. Accept-Language header

## Supported Cultures

- **en-US** (English - United States) - Default
- **es-ES** (Spanish - Spain)
- **fr-FR** (French - France)
- **de-DE** (German - Germany)
- **ja-JP** (Japanese - Japan)
- **zh-CN** (Chinese - China)

## Adding Translations

1. Create resource file: `Resources/Middleware.Localization.LocalizationMiddleware.{culture}.resx`
2. Add translations with error message as key:

```xml
<data name="Product not found" xml:space="preserve">
  <value>Producto no encontrado</value>
</data>
```

3. Rebuild the project

## Example Usage

**Request with Spanish culture:**
```http
GET /api/v1/products/invalid-id
Accept-Language: es-ES
```

**Response (localized):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Producto no encontrado",
  "status": 404,
  "instance": "/api/v1/products/invalid-id",
  "extensions": {
    "traceId": "0HN7...",
    "correlationId": "abc123",
    "culture": "es-ES"
  }
}
```

## Testing

Test localization with query string:
```http
GET /api/v1/products/invalid-id?culture=fr-FR
```

## Resource Files

- `Middleware.Localization.LocalizationMiddleware.resx` - Default (en-US)
- `Middleware.Localization.LocalizationMiddleware.es-ES.resx` - Spanish
- `Middleware.Localization.LocalizationMiddleware.fr-FR.resx` - French

## Architecture

```
┌─────────────────┐
│  HTTP Request   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Localization   │ ← Detects culture
│   Middleware    │   (Accept-Language)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Controller    │ ← Returns ProblemDetails
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Localization   │ ← Translates messages
│   Middleware    │   Adds culture to response
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  HTTP Response  │ ← Localized problem+json
└─────────────────┘
```

## Implementation Details

- **Interception**: Captures response body stream, parses JSON, localizes, writes back
- **Performance**: Only processes responses with `application/problem+json` content type
- **Error Handling**: Falls back to original response on parse errors
- **Culture Feature**: Uses `IRequestCultureFeature` to access current culture

## Related Tasks

- **T061**: Implement localization middleware (completed)
- **T062**: Add localization tests (pending)
