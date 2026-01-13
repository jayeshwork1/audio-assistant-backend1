# Audio Assistant API - Phase 1

A comprehensive .NET 8 Web API for the Audio Assistant application, providing authentication, API key management, and a robust foundation for AI-powered conversation features.

## Features

### Phase 1 Implementation

- **User Authentication**: Secure registration and login with JWT tokens
- **Password Security**: BCrypt-based password hashing with salt
- **API Key Management**: Encrypted storage of external provider API keys (AES-256)
- **Database**: SQLite with Entity Framework Core
- **Middleware**: Error handling, rate limiting, and JWT authentication
- **Logging**: Structured logging with Serilog
- **API Documentation**: Interactive Swagger/OpenAPI documentation
- **CORS Support**: Configurable cross-origin resource sharing

## Tech Stack

- **.NET 8.0**: Latest LTS version of .NET
- **Entity Framework Core 8.0**: ORM for database operations
- **SQLite**: Lightweight database for development and production
- **BCrypt.Net-Next**: Password hashing library
- **JWT Bearer Authentication**: Secure token-based authentication
- **Serilog**: Structured logging framework
- **Swagger/OpenAPI**: API documentation and testing

## Project Structure

```
AudioAssistant.Api/
├── Controllers/          # API endpoints
│   ├── AuthController.cs
│   └── ApiKeyController.cs
├── Services/            # Business logic
│   ├── AuthService.cs
│   └── ApiKeyService.cs
├── Models/              # Data models and DTOs
│   ├── User.cs
│   ├── ApiKey.cs
│   ├── Conversation.cs
│   └── DTOs/
├── Data/                # Database context
│   └── AudioAssistantDbContext.cs
├── Middleware/          # Custom middleware
│   ├── ErrorHandlingMiddleware.cs
│   └── RateLimitingMiddleware.cs
├── Utilities/           # Helper classes
│   ├── PasswordHasher.cs
│   ├── JwtTokenGenerator.cs
│   └── EncryptionService.cs
└── Migrations/          # EF Core migrations
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- A code editor (Visual Studio, VS Code, or JetBrains Rider)

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd AudioAssistant.Api
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Update configuration**
   
   Edit `appsettings.Development.json` to customize settings (optional):
   ```json
   {
     "JwtSettings": {
       "SecretKey": "your-secret-key-here-at-least-32-characters",
       "ExpirationMinutes": 1440
     },
     "EncryptionSettings": {
       "EncryptionKey": "your-encryption-key-here"
     }
   }
   ```

4. **Apply database migrations**
   ```bash
   dotnet ef database update
   ```
   
   Or simply run the application - migrations will be applied automatically on startup.

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the API**
   - API: `https://localhost:5001` or `http://localhost:5000`
   - Swagger UI: `https://localhost:5001/swagger`
   - Health Check: `https://localhost:5001/health`

## API Endpoints

### Authentication

#### Register a new user
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "user@example.com"
}
```

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "user@example.com"
}
```

#### Refresh Token
```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### API Key Management

All API key endpoints require authentication. Include the JWT token in the Authorization header:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Store API Key
```http
POST /api/apikey/store
Authorization: Bearer {token}
Content-Type: application/json

{
  "provider": "groq",
  "apiKey": "gsk_xxxxxxxxxxxxx"
}
```

#### Get Configured Providers
```http
GET /api/apikey/providers
Authorization: Bearer {token}
```

**Response:**
```json
{
  "providers": ["groq", "claude"]
}
```

#### Delete API Key
```http
DELETE /api/apikey/{provider}
Authorization: Bearer {token}
```

## Database Schema

The application uses SQLite with the following main entities:

- **User**: User accounts with email and password
- **UserPreferences**: User-specific settings and preferences
- **ApiKey**: Encrypted storage for external API keys
- **Conversation**: Conversation sessions
- **ConversationExchange**: Individual exchanges within conversations
- **Transcript**: Audio transcripts
- **Translation**: Translated transcripts
- **Meeting**: Meeting metadata
- **MeetingNotes**: Meeting notes and summaries
- **Export**: Exported meeting notes
- **TransactionLog**: API usage tracking

## Configuration

### JWT Settings
```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key-minimum-32-characters",
    "Issuer": "AudioAssistant.Api",
    "Audience": "AudioAssistant.Client",
    "ExpirationMinutes": 1440
  }
}
```

### Rate Limiting
```json
{
  "RateLimiting": {
    "RequestsPerMinute": 100
  }
}
```

### CORS
```json
{
  "CorsSettings": {
    "AllowedOrigins": [
      "chrome-extension://*",
      "http://localhost:3000"
    ]
  }
}
```

## Security Best Practices

1. **Never commit secrets**: Use environment variables for sensitive data in production
2. **Change default keys**: Update JWT and encryption keys in production
3. **Use HTTPS**: Always use HTTPS in production
4. **Rate limiting**: Adjust rate limits based on your needs
5. **Password requirements**: Minimum 8 characters enforced

## Development

### Running Migrations

Create a new migration:
```bash
dotnet ef migrations add MigrationName
```

Apply migrations:
```bash
dotnet ef database update
```

Remove last migration:
```bash
dotnet ef migrations remove
```

### Running Tests

```bash
dotnet test
```

### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

## Troubleshooting

### Database Issues

If you encounter database errors, try:
1. Delete the database file (`audioassistant.dev.db`)
2. Delete the `Migrations` folder
3. Run `dotnet ef migrations add InitialCreate`
4. Run the application

### JWT Token Issues

- Ensure the JWT secret key is at least 32 characters
- Check token expiration settings
- Verify the token is included in the Authorization header as `Bearer {token}`

### CORS Issues

Update the `CorsSettings:AllowedOrigins` in `appsettings.json` to include your client application's origin.

## License

[Your License Here]

## Support

For issues and questions, please create an issue in the repository.
