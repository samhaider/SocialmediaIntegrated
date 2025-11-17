# Social Media Management API Platform

A unified REST API that connects to 13+ social networks (Facebook, Instagram, Twitter, TikTok, LinkedIn, YouTube, etc.) through a single interface.

## Features

- **Multi-platform posting**: Publish content to multiple social networks with a single API call
- **Server-side scheduling & automation**: Schedule posts for future publishing
- **Real-time analytics aggregation**: Track engagement metrics across all platforms
- **Comment & DM management**: Manage interactions from a centralized interface
- **Multi-tenant user profiles**: Support for multiple tenants with JWT SSO authentication
- **Media hosting & processing**: Upload and manage media files

## Technology Stack

- **ASP.NET Core 8.0** - Web API framework
- **Entity Framework Core 8.0** - ORM for database access
- **Microsoft SQL Server** - Database
- **JWT Authentication** - Secure token-based authentication
- **Swagger/OpenAPI** - API documentation

## Supported Platforms (13+)

The API provides unified interfaces for 13+ social media platforms:
1. **Facebook** - World's largest social network
2. **Instagram** - Photo and video sharing platform
3. **Twitter/X** - Microblogging and social networking
4. **LinkedIn** - Professional networking platform
5. **TikTok** - Short-form video platform
6. **YouTube** - Video sharing and streaming
7. **Pinterest** - Visual discovery and bookmarking
8. **Snapchat** - Multimedia messaging
9. **Reddit** - Social news aggregation and discussion
10. **Tumblr** - Microblogging and social networking
11. **Medium** - Online publishing platform
12. **VK** - Russian social networking service
13. **Telegram** - Cloud-based instant messaging

Each platform implements a common interface, allowing for seamless multi-platform posting and management.

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- SQL Server (LocalDB or full SQL Server)
- Visual Studio 2022 or VS Code (optional)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/samhaider/SocialmediaIntegrated.git
cd SocialmediaIntegrated
```

2. Update the connection string in `SocialMediaAPI/appsettings.json` to match your SQL Server instance:

**For Windows with LocalDB:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SocialMediaDB;Trusted_Connection=true;MultipleActiveResultSets=true"
}
```

**For SQL Server with authentication:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=SocialMediaDB;User Id=sa;Password=YourPassword;TrustServerCertificate=true;MultipleActiveResultSets=true"
}
```

**For Azure SQL:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=tcp:yourserver.database.windows.net,1433;Database=SocialMediaDB;User ID=yourusername;Password=yourpassword;Encrypt=True;TrustServerCertificate=False;"
}
```

3. Update the JWT secret key in `SocialMediaAPI/appsettings.json` (must be at least 32 characters):
```json
"Jwt": {
  "Key": "YourSecureKeyHere_AtLeast32Characters!"
}
```

4. Create the database (migrations are already included):
```bash
cd SocialMediaAPI
dotnet ef database update
```

5. Run the application:
```bash
dotnet run
```

The API will be available at `https://localhost:5001` or `http://localhost:5000`.

## API Documentation

Once the application is running, navigate to `/swagger` to view the interactive API documentation.

### Main Endpoints

#### Authentication
- `POST /api/auth/register` - Register a new user
- `POST /api/auth/login` - Login and receive JWT token

#### Social Accounts
- `POST /api/socialaccounts` - Connect a social media account
- `GET /api/socialaccounts` - Get all connected accounts
- `DELETE /api/socialaccounts/{id}` - Disconnect an account

#### Posts
- `POST /api/posts` - Create a new post
- `GET /api/posts` - Get all posts for the authenticated user
- `GET /api/posts/{id}` - Get a specific post
- `POST /api/posts/{id}/publish` - Publish a post to connected platforms
- `DELETE /api/posts/{id}` - Delete a post

#### Analytics
- `GET /api/analytics/posts/{id}` - Get analytics for a post
- `POST /api/analytics/posts/{id}/refresh` - Refresh analytics data

#### Media
- `POST /api/media/upload` - Upload media file
- `DELETE /api/media/{id}` - Delete media file

## Authentication

All endpoints except `/api/auth/register` and `/api/auth/login` require authentication. Include the JWT token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## Example Usage

### 1. Register a User
```bash
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "john_doe",
    "email": "john@example.com",
    "password": "SecurePassword123",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

### 2. Login
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePassword123"
  }'
```

### 3. Connect a Social Account
```bash
curl -X POST https://localhost:5001/api/socialaccounts \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <your-token>" \
  -d '{
    "platform": "Facebook",
    "accessToken": "facebook-access-token"
  }'
```

### 4. Create and Publish a Post
```bash
curl -X POST https://localhost:5001/api/posts \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <your-token>" \
  -d '{
    "content": "Hello from Social Media API!",
    "platforms": ["Facebook", "Twitter", "LinkedIn"]
  }'

# Then publish it
curl -X POST https://localhost:5001/api/posts/1/publish \
  -H "Authorization: Bearer <your-token>"
```

## Multi-Tenant Support

The API supports multi-tenancy through the `TenantId` field. When registering a user, you can optionally provide a `tenantId`:

```json
{
  "username": "user",
  "email": "user@example.com",
  "password": "password",
  "tenantId": "tenant-123"
}
```

## Project Structure

```
SocialMediaAPI/
├── Controllers/          # API Controllers
├── Data/                 # Database context
├── DTOs/                 # Data Transfer Objects
├── Interfaces/           # Service interfaces
├── Models/               # Domain models
├── Services/             # Business logic
│   └── Platforms/        # Social media platform implementations
├── Middleware/           # Custom middleware
├── Program.cs            # Application entry point
└── appsettings.json      # Configuration
```

## Configuration

Key configuration settings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your SQL Server connection string"
  },
  "Jwt": {
    "Key": "Your secret key (min 32 characters)",
    "Issuer": "SocialMediaAPI",
    "Audience": "SocialMediaAPIUsers"
  },
  "MediaStorage": {
    "Path": "uploads"
  }
}
```

## Development

### Adding a New Social Media Platform

1. Create a new class implementing `ISocialMediaPlatform` in `Services/Platforms/`
2. Register it in `Program.cs`:
```csharp
builder.Services.AddScoped<ISocialMediaPlatform, YourNewPlatform>();
```

### Database Migrations

After modifying models:
```bash
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

## Security Considerations

- JWT tokens expire after 24 hours
- Passwords are hashed using SHA256 (in production, use bcrypt or Argon2)
- Social media tokens are stored encrypted in the database
- CORS is configured to allow all origins (restrict in production)

## License

This project is licensed under the MIT License.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

For issues and questions, please open an issue on the GitHub repository.