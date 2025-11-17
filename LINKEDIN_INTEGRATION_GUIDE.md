# LinkedIn Company Page Integration Guide

## Overview

This guide provides comprehensive instructions for integrating LinkedIn Company Page posting functionality into your application using the Social Media Management API.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Configuration](#configuration)
3. [OAuth Flow](#oauth-flow)
4. [Connecting Accounts](#connecting-accounts)
5. [Posting to LinkedIn](#posting-to-linkedin)
6. [Analytics and Engagement](#analytics-and-engagement)
7. [Error Handling](#error-handling)
8. [Best Practices](#best-practices)
9. [API Reference](#api-reference)

---

## Prerequisites

### LinkedIn Developer App Setup

1. **Create a LinkedIn App**
   - Visit [LinkedIn Developer Portal](https://www.linkedin.com/developers)
   - Create a new app or select an existing one
   - Note your `Client ID` and `Client Secret`

2. **Request Access to Products**
   - Apply for **Marketing Developer Platform** access
   - Apply for **Community Management API** access
   - Wait for approval (this may take several days)

3. **Configure OAuth 2.0 Settings**
   - Add redirect URLs in your app settings
   - Example: `https://your-domain.com/api/auth/linkedin/callback`

4. **Required Scopes**
   - `openid` - Basic authentication
   - `profile` - User profile information
   - `email` - User email address
   - `w_organization_social` - Post on behalf of organizations
   - `r_organization_social` - Read organization posts
   - `rw_organization_admin` - Manage organization settings (optional)

### Company Page Requirements

- You must be an **ADMINISTRATOR** or **CONTENT_ADMIN** of the LinkedIn Company Page
- The LinkedIn Company Page must be active and in good standing
- The authorized user must have accepted the page's terms

---

## Configuration

### 1. Update appsettings.json

```json
{
  "LinkedIn": {
    "ClientId": "YOUR_LINKEDIN_CLIENT_ID",
    "ClientSecret": "YOUR_LINKEDIN_CLIENT_SECRET",
    "RedirectUri": "https://your-domain.com/api/auth/linkedin/callback",
    "AuthorizationEndpoint": "https://www.linkedin.com/oauth/v2/authorization",
    "TokenEndpoint": "https://www.linkedin.com/oauth/v2/accessToken",
    "ApiVersion": "202408",
    "ApiBaseUrl": "https://api.linkedin.com/rest",
    "Scopes": "openid,profile,email,w_organization_social,r_organization_social,rw_organization_admin",
    "RateLimiting": {
      "MaxRequestsPerMinute": 100,
      "MaxRequestsPerDay": 10000
    }
  }
}
```

### 2. Run Database Migration

```bash
dotnet ef database update
```

This will add the necessary columns to support LinkedIn organization accounts.

---

## OAuth Flow

### Step 1: Get Authorization URL

**Endpoint:** `GET /api/linkedin/auth/url`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
{
  "authorizationUrl": "https://www.linkedin.com/oauth/v2/authorization?...",
  "state": "user123_abc-def-ghi",
  "instructions": {
    "step1": "Visit the authorization URL",
    "step2": "Grant permissions to your LinkedIn account or organization",
    "step3": "You will be redirected back with an authorization code",
    "step4": "Use the /api/linkedin/auth/callback endpoint with the code"
  }
}
```

### Step 2: User Authorizes Application

Direct the user to the `authorizationUrl`. After granting permissions, they will be redirected to your callback URL with an authorization code.

Example redirect:
```
https://your-domain.com/api/auth/linkedin/callback?code=AQT...&state=user123_abc-def-ghi
```

### Step 3: Exchange Code for Token

**Endpoint:** `POST /api/linkedin/auth/callback`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

**Request Body:**
```json
{
  "code": "AQT...",
  "state": "user123_abc-def-ghi"
}
```

**Response:**
```json
{
  "accessToken": "AQV...",
  "expiresIn": 5184000,
  "refreshToken": "AQX...",
  "scope": "openid,profile,email,w_organization_social,r_organization_social",
  "nextSteps": {
    "step1": "Use the access token to get your organizations (optional)",
    "step2": "Connect the account using /api/socialaccounts endpoint"
  }
}
```

### Step 4: Get User Organizations (Optional)

**Endpoint:** `GET /api/linkedin/organizations?accessToken=YOUR_ACCESS_TOKEN`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
{
  "count": 2,
  "organizations": [
    {
      "organizationUrn": "urn:li:organization:123456",
      "organizationName": "Acme Corporation",
      "role": "ADMINISTRATOR",
      "state": "APPROVED",
      "canPost": true
    },
    {
      "organizationUrn": "urn:li:organization:789012",
      "organizationName": "Tech Startup Inc",
      "role": "CONTENT_ADMIN",
      "state": "APPROVED",
      "canPost": true
    }
  ],
  "instructions": {
    "message": "To post as an organization, use the organizationUrn when connecting",
    "example": "Set AccountType='Organization' and OrganizationId to the organizationUrn"
  }
}
```

---

## Connecting Accounts

### Connect Personal Account

**Endpoint:** `POST /api/socialaccounts`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

**Request Body:**
```json
{
  "platform": "LinkedIn",
  "accessToken": "AQV...",
  "refreshToken": "AQX...",
  "tokenExpiresAt": "2025-02-15T10:30:00Z",
  "accountType": "Personal",
  "scopes": "openid,profile,email"
}
```

### Connect Organization Account (Company Page)

**Endpoint:** `POST /api/socialaccounts`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

**Request Body:**
```json
{
  "platform": "LinkedIn",
  "accessToken": "AQV...",
  "refreshToken": "AQX...",
  "tokenExpiresAt": "2025-02-15T10:30:00Z",
  "accountType": "Organization",
  "organizationId": "urn:li:organization:123456",
  "organizationName": "Acme Corporation",
  "scopes": "openid,profile,email,w_organization_social,r_organization_social"
}
```

**Response:**
```json
{
  "id": 42,
  "platform": "LinkedIn",
  "accountId": "abc123xyz",
  "accountName": "John Doe",
  "isActive": true,
  "connectedAt": "2025-11-17T12:00:00Z",
  "organizationId": "urn:li:organization:123456",
  "organizationName": "Acme Corporation",
  "accountType": "Organization",
  "scopes": "openid,profile,email,w_organization_social,r_organization_social"
}
```

---

## Posting to LinkedIn

### Create a Post

**Endpoint:** `POST /api/posts`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

**Request Body (Text Only):**
```json
{
  "content": "Excited to announce our new product launch! #innovation #technology",
  "platforms": ["LinkedIn"]
}
```

**Request Body (With Image):**
```json
{
  "content": "Check out our latest infographic on market trends!",
  "mediaUrls": [
    "https://your-cdn.com/images/infographic.jpg"
  ],
  "platforms": ["LinkedIn"]
}
```

**Request Body (Scheduled Post):**
```json
{
  "content": "Join us for our webinar tomorrow at 2 PM EST!",
  "platforms": ["LinkedIn"],
  "scheduledAt": "2025-11-18T14:00:00Z"
}
```

**Response:**
```json
{
  "id": 100,
  "content": "Excited to announce our new product launch! #innovation #technology",
  "platforms": ["LinkedIn"],
  "status": "published",
  "createdAt": "2025-11-17T12:00:00Z",
  "publishedAt": "2025-11-17T12:00:05Z",
  "results": [
    {
      "platform": "LinkedIn",
      "platformPostId": "urn:li:share:7890123456789012345",
      "status": "success",
      "publishedAt": "2025-11-17T12:00:05Z"
    }
  ]
}
```

### Publish an Existing Draft

**Endpoint:** `POST /api/posts/{postId}/publish`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
{
  "id": 100,
  "status": "published",
  "results": [
    {
      "platform": "LinkedIn",
      "platformPostId": "urn:li:share:7890123456789012345",
      "status": "success"
    }
  ]
}
```

---

## Analytics and Engagement

### Get Post Analytics

**Endpoint:** `GET /api/analytics/posts/{postId}`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
{
  "postId": 100,
  "analytics": [
    {
      "platform": "LinkedIn",
      "platformPostId": "urn:li:share:7890123456789012345",
      "likes": 156,
      "comments": 23,
      "shares": 12,
      "views": 3420,
      "clicks": 245,
      "engagement": 0.114,
      "lastUpdated": "2025-11-17T15:30:00Z"
    }
  ]
}
```

### Refresh Analytics

**Endpoint:** `POST /api/analytics/posts/{postId}/refresh`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

This will fetch the latest analytics data from LinkedIn.

---

## Error Handling

### Common Errors

#### 1. Invalid Access Token
```json
{
  "error": "Invalid access token"
}
```
**Solution:** Refresh the token using the refresh token endpoint.

#### 2. Insufficient Permissions
```json
{
  "status": 403,
  "code": "ACCESS_DENIED",
  "message": "The token does not have the required scope"
}
```
**Solution:** Re-authorize with the correct scopes (w_organization_social).

#### 3. Organization Not Found
```json
{
  "error": "Social account not found"
}
```
**Solution:** Ensure the organization account is properly connected.

#### 4. Rate Limit Exceeded
```json
{
  "status": 429,
  "message": "Rate limit exceeded"
}
```
**Solution:** Implement exponential backoff. The API automatically retries up to 4 times.

#### 5. Media Upload Failed
```json
{
  "error": "Failed to upload media"
}
```
**Solution:** Check media file size and format. LinkedIn supports:
- Images: JPG, PNG, GIF (max 10MB)
- Videos: MP4 (max 200MB, 10 minutes)

---

## Best Practices

### 1. Token Management

- **Store tokens securely** in your database (encrypted)
- **Refresh tokens** before they expire (LinkedIn tokens typically expire after 60 days)
- **Handle token refresh** automatically in your application

### 2. Content Guidelines

- Keep posts under 3,000 characters (LinkedIn limit)
- Use relevant hashtags (maximum 3-5)
- Include engaging visuals when possible
- Schedule posts during business hours for maximum engagement

### 3. Rate Limiting

- Respect LinkedIn's rate limits:
  - 100 requests per minute per user
  - 10,000 requests per day per app
- Implement exponential backoff for retries
- Cache analytics data to reduce API calls

### 4. Error Recovery

- Implement retry logic for transient errors
- Log all API errors for debugging
- Provide meaningful error messages to users
- Monitor token expiration proactively

### 5. Organization Posting

- Verify user has ADMINISTRATOR or CONTENT_ADMIN role
- Always test with a test organization page first
- Monitor organization page compliance
- Keep organization information up to date

---

## API Reference

### LinkedIn Controller Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/linkedin/auth/url` | Get OAuth authorization URL |
| POST | `/api/linkedin/auth/callback` | Exchange code for token |
| GET | `/api/linkedin/user/info` | Get user information |
| GET | `/api/linkedin/organizations` | Get user's organizations |
| POST | `/api/linkedin/auth/refresh` | Refresh access token |

### Social Accounts Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/socialaccounts` | Connect LinkedIn account |
| GET | `/api/socialaccounts` | List connected accounts |
| DELETE | `/api/socialaccounts/{id}` | Disconnect account |

### Posts Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/posts` | Create a new post |
| GET | `/api/posts` | List user posts |
| GET | `/api/posts/{id}` | Get specific post |
| POST | `/api/posts/{id}/publish` | Publish a draft post |
| DELETE | `/api/posts/{id}` | Delete a post |

### Analytics Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/analytics/posts/{id}` | Get post analytics |
| POST | `/api/analytics/posts/{id}/refresh` | Refresh analytics data |

---

## Testing

### Test Checklist

- [ ] Successfully complete OAuth flow
- [ ] Connect personal LinkedIn account
- [ ] Retrieve list of organizations
- [ ] Connect organization account
- [ ] Post text-only content
- [ ] Post content with image
- [ ] Post content with video
- [ ] Retrieve post analytics
- [ ] Handle token refresh
- [ ] Test error scenarios
- [ ] Verify rate limiting works
- [ ] Test scheduled posts

### Sample Test Scenarios

See `LINKEDIN_EXAMPLES.md` for detailed test scenarios and example requests.

---

## Security Considerations

1. **Never expose** Client Secret in client-side code
2. **Always use HTTPS** for all API communications
3. **Validate** all user inputs before sending to LinkedIn
4. **Store tokens encrypted** in your database
5. **Implement** proper authentication and authorization
6. **Monitor** for suspicious activity
7. **Comply** with LinkedIn's API Terms of Service
8. **Review** permissions regularly

---

## Support and Resources

### Official Documentation
- [LinkedIn Marketing API](https://learn.microsoft.com/en-us/linkedin/marketing/)
- [LinkedIn Posts API](https://learn.microsoft.com/en-us/linkedin/marketing/community-management/shares/posts-api)
- [LinkedIn Assets API](https://learn.microsoft.com/en-us/linkedin/marketing/integrations/community-management/shares/images-videos-articles-api)

### Developer Portal
- [LinkedIn Developers](https://www.linkedin.com/developers)
- [API Console](https://www.linkedin.com/developers/apps)
- [Support](https://www.linkedin.com/help/linkedin/ask/api)

### Community
- [LinkedIn Developer Forum](https://www.linkedin.com/help/linkedin/forum/api)
- Stack Overflow: Tag `linkedin-api`

---

## Troubleshooting

### Issue: "App not approved for required products"

**Solution:** Your LinkedIn app needs to be reviewed and approved by LinkedIn for Marketing Developer Platform access. This process can take 3-7 business days.

### Issue: "No organizations found"

**Possible causes:**
1. User doesn't have admin/content admin role on any company page
2. Scopes don't include `w_organization_social`
3. App doesn't have Marketing Developer Platform access

**Solution:** Verify all prerequisites are met.

### Issue: "Media upload fails"

**Possible causes:**
1. File too large
2. Unsupported format
3. Network timeout

**Solution:**
- Compress images/videos
- Use supported formats (JPG, PNG, MP4)
- Implement retry logic

---

## Changelog

### Version 1.0.0 (2025-11-17)
- Initial implementation
- Support for personal and organization posting
- OAuth 2.0 flow
- Media upload (images and videos)
- Analytics and engagement tracking
- Rate limiting and retry logic
- Comprehensive error handling

---

## License

This integration is part of the Social Media Management API and follows the same license terms.
