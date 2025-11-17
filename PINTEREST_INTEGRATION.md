# Pinterest API v5 Integration

## Overview

This document describes the complete Pinterest API v5 integration implemented for the Social Media Management API platform. The integration supports OAuth 2.0 authentication, pin creation, board management, and analytics.

## Features Implemented

### ✅ Core Features

1. **OAuth 2.0 Authentication**
   - Access token validation
   - Refresh token support with automatic token renewal
   - Secure token storage

2. **Pin Management**
   - Create pins with images (URL or Base64)
   - Get pin details
   - List user pins with pagination
   - List board pins with pagination
   - Delete pins

3. **Board Management**
   - List all user boards
   - Support for board sections
   - Pagination support

4. **Analytics**
   - Pin impressions
   - Saves (likes)
   - Pin clicks (shares/repins)
   - Outbound clicks
   - Video views

5. **Error Handling**
   - Comprehensive error logging
   - API error response parsing
   - Rate limit handling
   - Token expiration handling

## Architecture

### Files Created/Modified

```
SocialMediaAPI/
├── Controllers/
│   └── PinterestController.cs          (NEW) - Pinterest-specific endpoints
├── DTOs/
│   └── PinterestDTOs.cs                (NEW) - Pinterest data transfer objects
├── Interfaces/
│   └── IPinterestService.cs            (NEW) - Pinterest service interface
├── Services/
│   ├── PinterestService.cs             (NEW) - Pinterest API v5 implementation
│   └── Platforms/
│       └── PinterestPlatform.cs        (UPDATED) - Platform adapter
├── Program.cs                          (UPDATED) - Added service registration
└── appsettings.json                    (UPDATED) - Added Pinterest config
```

## Configuration

### appsettings.json

Add the following configuration section:

```json
{
  "Pinterest": {
    "ClientId": "your-pinterest-app-id",
    "ClientSecret": "your-pinterest-app-secret",
    "RedirectUri": "https://yourdomain.com/api/pinterest/oauth/callback",
    "UseSandbox": false,
    "Scopes": "pins:read,pins:write,boards:read,boards:write,user_accounts:read"
  }
}
```

### Environment Variables (Production)

For production, use environment variables instead of appsettings.json:

```bash
export Pinterest__ClientId="your-app-id"
export Pinterest__ClientSecret="your-app-secret"
export Pinterest__RedirectUri="https://yourdomain.com/api/pinterest/oauth/callback"
export Pinterest__UseSandbox="false"
```

## API Endpoints

### Pinterest-Specific Endpoints

All endpoints require JWT authentication via `Authorization: Bearer {token}` header.

#### 1. Get Account Information

```http
GET /api/pinterest/account
```

**Response:**
```json
{
  "username": "user123",
  "account_type": "BUSINESS",
  "profile_image": "https://...",
  "website_url": "https://...",
  "monthly_views": 10000
}
```

#### 2. List Boards

```http
GET /api/pinterest/boards?pageSize=25&bookmark=string
```

**Response:**
```json
{
  "items": [
    {
      "id": "board-id",
      "name": "My Board",
      "description": "Board description",
      "privacy": "PUBLIC",
      "pin_count": 42,
      "follower_count": 100
    }
  ],
  "bookmark": "next-page-token"
}
```

#### 3. Create Pin

```http
POST /api/pinterest/pins
Content-Type: application/json

{
  "boardId": "board-id",
  "boardSectionId": "section-id",  // optional
  "title": "Pin Title",
  "description": "Pin description",
  "link": "https://example.com",
  "altText": "Image alt text",
  "mediaUrl": "https://example.com/image.jpg",
  "mediaType": "image_url"
}
```

**Response:**
```json
{
  "id": "pin-id",
  "created_at": "2025-11-17T10:00:00Z",
  "title": "Pin Title",
  "description": "Pin description",
  "board_id": "board-id"
}
```

#### 4. Get Pin Details

```http
GET /api/pinterest/pins/{pinId}
```

#### 5. List User Pins

```http
GET /api/pinterest/pins?pageSize=25&bookmark=string
```

#### 6. List Board Pins

```http
GET /api/pinterest/boards/{boardId}/pins?pageSize=25&bookmark=string
```

#### 7. Get Pin Analytics

```http
GET /api/pinterest/pins/{pinId}/analytics?startDate=2025-01-01&endDate=2025-01-31
```

**Response:**
```json
{
  "impressions": 1000,
  "saves": 50,
  "pinClicks": 30,
  "outboundClicks": 20,
  "videoViews": 0
}
```

#### 8. Delete Pin

```http
DELETE /api/pinterest/pins/{pinId}
```

### Generic Multi-Platform Endpoints

Pinterest is also accessible through generic endpoints:

#### Connect Pinterest Account

```http
POST /api/socialaccounts
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "platform": "Pinterest",
  "accessToken": "pinterest-access-token",
  "refreshToken": "pinterest-refresh-token",
  "tokenExpiresAt": "2025-12-17T10:00:00Z"
}
```

#### Publish Post to Pinterest

```http
POST /api/posts
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "content": "Check out this amazing image!",
  "mediaUrls": ["https://example.com/image.jpg"],
  "platforms": ["Pinterest"],
  "scheduledAt": "2025-11-18T10:00:00Z"
}
```

Then publish:

```http
POST /api/posts/{postId}/publish
```

## OAuth 2.0 Flow

### Step 1: Redirect User to Pinterest Authorization

```
https://www.pinterest.com/oauth/
  ?client_id=YOUR_CLIENT_ID
  &redirect_uri=YOUR_REDIRECT_URI
  &response_type=code
  &scope=pins:read,pins:write,boards:read,boards:write,user_accounts:read
  &state=RANDOM_STATE_STRING
```

### Step 2: Handle Callback

Pinterest redirects to: `YOUR_REDIRECT_URI?code=AUTH_CODE&state=STATE`

### Step 3: Exchange Code for Token

```bash
curl -X POST https://api.pinterest.com/v5/oauth/token \
  -H "Authorization: Basic BASE64(client_id:client_secret)" \
  -d "grant_type=authorization_code" \
  -d "code=AUTH_CODE" \
  -d "redirect_uri=YOUR_REDIRECT_URI"
```

**Response:**
```json
{
  "access_token": "...",
  "refresh_token": "...",
  "token_type": "bearer",
  "expires_in": 2592000,
  "scope": "pins:read,pins:write,..."
}
```

### Step 4: Store Tokens

Connect the account using the generic endpoint:

```http
POST /api/socialaccounts
```

## Required Scopes

| Scope | Purpose |
|-------|---------|
| `user_accounts:read` | Read user account information |
| `pins:read` | Read user pins |
| `pins:write` | Create and delete pins |
| `boards:read` | List boards |
| `boards:write` | Create and modify boards |

## Security Best Practices

### ✅ Implemented

1. **Token Storage**: Access and refresh tokens stored in database
2. **Automatic Refresh**: Tokens automatically refreshed when expired
3. **HTTPS Only**: All API calls use HTTPS
4. **Input Validation**: Request data validated
5. **Error Logging**: Comprehensive logging without exposing secrets
6. **JWT Authentication**: All endpoints require authentication

### 🔒 Production Recommendations

1. **Encrypt Tokens**: Implement encryption for stored tokens
2. **Rate Limiting**: Add rate limiting middleware
3. **Token Rotation**: Periodically rotate refresh tokens
4. **Audit Logging**: Log all Pinterest API operations
5. **Webhook Validation**: Validate Pinterest webhooks (if implemented)
6. **Environment Variables**: Use environment variables for secrets

## Testing

### Prerequisites

1. Create a Pinterest Developer App at [developers.pinterest.com](https://developers.pinterest.com/)
2. Configure OAuth redirect URI
3. Obtain Client ID and Client Secret
4. Have a Pinterest Business or Creator account

### Test Flow

1. **Register/Login**
   ```bash
   POST /api/auth/register
   POST /api/auth/login
   ```

2. **Connect Pinterest Account**
   - Complete OAuth flow
   - Store tokens via `/api/socialaccounts`

3. **List Boards**
   ```bash
   GET /api/pinterest/boards
   ```

4. **Create Pin**
   ```bash
   POST /api/pinterest/pins
   ```

5. **Get Analytics**
   ```bash
   GET /api/pinterest/pins/{pinId}/analytics
   ```

### Sandbox Testing

To use Pinterest's sandbox environment:

```json
{
  "Pinterest": {
    "UseSandbox": true
  }
}
```

API base URL will change to: `https://api-sandbox.pinterest.com/v5`

## Error Handling

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| 401 Unauthorized | Invalid/expired token | Refresh token or re-authenticate |
| 403 Forbidden | Insufficient scopes | Request proper scopes during OAuth |
| 404 Not Found | Invalid pin/board ID | Verify ID exists |
| 429 Too Many Requests | Rate limit exceeded | Implement backoff/retry |
| 400 Bad Request | Invalid request data | Check request format |

### Error Response Format

```json
{
  "code": 400,
  "message": "Invalid board_id"
}
```

## Analytics Mapping

Pinterest metrics mapped to generic `PostAnalytics`:

| Pinterest Metric | Generic Field |
|-----------------|---------------|
| Impressions | Views |
| Saves | Likes |
| Pin Clicks | Shares |
| Outbound Clicks | Clicks |
| Video Views | - |

## Limitations

### Pinterest API v5 Limitations

1. **No Comment API**: Pinterest v5 doesn't support fetching/replying to comments
2. **No DM API**: No direct messaging functionality
3. **Analytics Delay**: Analytics data may have 24-48 hour delay
4. **Rate Limits**: Subject to Pinterest's rate limiting
5. **Business Account**: Some features require Business account

### Implementation Notes

- Default board used for generic post publishing
- Title truncated to 100 characters (Pinterest limit)
- Only first media URL used when publishing via generic endpoint

## Dependencies

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="System.Net.Http" Version="8.0.0" />
```

## Maintenance

### Monitoring

Monitor these metrics:

- Token refresh success rate
- API error rates
- Response times
- Rate limit hits

### Updates

- Review [Pinterest API Changelog](https://developers.pinterest.com/docs/api/v5/)
- Update scopes as needed
- Test new API versions in sandbox

## Support

### Documentation

- [Pinterest API v5 Docs](https://developers.pinterest.com/docs/api/v5/)
- [OAuth 2.0 Guide](https://developers.pinterest.com/docs/api/v5/#tag/Authentication)
- [API Reference](https://developers.pinterest.com/docs/api/v5/)

### Troubleshooting

1. **Token Issues**: Check expiration, scopes, and refresh logic
2. **API Errors**: Enable detailed logging in `appsettings.json`
3. **Connection Issues**: Verify firewall/network access to `api.pinterest.com`

## License

This implementation follows Pinterest's [Developer Terms of Service](https://policy.pinterest.com/en/developer-guidelines).

---

**Last Updated**: 2025-11-17
**API Version**: Pinterest API v5
**.NET Version**: 8.0
