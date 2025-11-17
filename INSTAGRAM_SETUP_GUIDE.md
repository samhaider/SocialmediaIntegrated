# Instagram Graph API Integration - Setup Guide

This guide explains how to set up and use the Instagram Graph API integration for publishing content to Instagram Business/Creator accounts.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Facebook App Setup](#facebook-app-setup)
- [Configuration](#configuration)
- [Authentication Flow](#authentication-flow)
- [Publishing Content](#publishing-content)
- [API Endpoints](#api-endpoints)
- [Error Handling](#error-handling)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Instagram Requirements

1. **Instagram Business or Creator Account**
   - Personal Instagram accounts cannot use the publishing API
   - Convert your account: Settings → Account → Switch to Professional Account

2. **Connected Facebook Page**
   - Your Instagram account must be connected to a Facebook Page
   - Go to Instagram Settings → Business → Linked Accounts → Facebook → Connect Page

### Platform Requirements

1. **Facebook Developer Account**
   - Create at https://developers.facebook.com

2. **Facebook App**
   - Create a new app in the Facebook Developer Dashboard
   - Choose "Business" as the app type

3. **Admin Access**
   - You must be an admin of the Facebook Page linked to your Instagram account

---

## Facebook App Setup

### Step 1: Create a Facebook App

1. Go to https://developers.facebook.com
2. Click "My Apps" → "Create App"
3. Choose "Business" as the app type
4. Fill in the app details:
   - **App Name**: Your application name
   - **App Contact Email**: Your email
   - **Business Account**: Select or create one

### Step 2: Add Instagram Graph API Product

1. In your app dashboard, click "Add Product"
2. Find "Instagram Graph API" and click "Set Up"
3. Complete the setup wizard

### Step 3: Configure OAuth Settings

1. Go to "Facebook Login" → "Settings"
2. Add your OAuth Redirect URI:
   ```
   https://yourdomain.com/api/instagram/auth/callback
   ```
   For local development:
   ```
   https://localhost:7000/api/instagram/auth/callback
   ```

### Step 4: Get App Credentials

1. Go to Settings → Basic
2. Copy your:
   - **App ID**
   - **App Secret** (click "Show")

### Step 5: Request Permissions

For production apps, you need to request:
- `instagram_basic`
- `instagram_content_publish`
- `pages_show_list`
- `pages_read_engagement`
- `business_management`

For development/testing, these permissions are available by default.

---

## Configuration

### Update appsettings.json

Replace the placeholder values in `appsettings.json`:

```json
{
  "Instagram": {
    "AppId": "YOUR_FACEBOOK_APP_ID",
    "AppSecret": "YOUR_FACEBOOK_APP_SECRET",
    "RedirectUri": "https://yourdomain.com/api/instagram/auth/callback",
    "GraphApiBaseUrl": "https://graph.facebook.com/v21.0",
    "OAuthBaseUrl": "https://www.facebook.com/v21.0/dialog/oauth",
    "RequiredPermissions": [
      "instagram_basic",
      "instagram_content_publish",
      "pages_show_list",
      "pages_read_engagement",
      "business_management"
    ],
    "MaxRetryAttempts": 3,
    "RetryDelayMs": 1000,
    "TokenRefreshThresholdDays": 7
  }
}
```

### Environment-Specific Configuration

For production, use environment variables or Azure Key Vault:

```bash
export Instagram__AppId="your_app_id"
export Instagram__AppSecret="your_app_secret"
export Instagram__RedirectUri="https://yourdomain.com/api/instagram/auth/callback"
```

---

## Authentication Flow

### Step 1: Get Authorization URL

**Request:**
```http
GET /api/instagram/auth/url
Authorization: Bearer {jwt_token}
```

**Response:**
```json
{
  "authorizationUrl": "https://www.facebook.com/v21.0/dialog/oauth?client_id=...",
  "state": "guid-for-csrf-protection",
  "redirectUri": "https://yourdomain.com/api/instagram/auth/callback"
}
```

### Step 2: Redirect User to Facebook

Direct the user to the `authorizationUrl` from Step 1. They will:
1. Log in to Facebook (if not already logged in)
2. Grant permissions to your app
3. Be redirected back to your `redirectUri` with a `code` parameter

### Step 3: Handle OAuth Callback

**Request:**
```http
GET /api/instagram/auth/callback?code={auth_code}&state={state}
Authorization: Bearer {jwt_token}
```

**Response:**
```json
{
  "success": true,
  "message": "Successfully connected 1 Instagram account(s)",
  "accounts": [
    {
      "id": 123,
      "instagramAccountId": "17841400001234567",
      "username": "your_instagram_username",
      "name": "Your Name",
      "profilePictureUrl": "https://...",
      "followersCount": 1000,
      "followsCount": 500,
      "mediaCount": 50,
      "facebookPageId": "123456789",
      "facebookPageName": "Your Page Name",
      "tokenExpiresAt": "2025-01-15T12:00:00Z"
    }
  ]
}
```

The access token is automatically stored in the database and will be used for publishing.

---

## Publishing Content

### Single Image Post

**Request:**
```http
POST /api/posts
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "content": "Check out this amazing photo! #nature #photography",
  "mediaUrls": [
    "https://yourdomain.com/uploads/image.jpg"
  ],
  "platforms": ["Instagram"]
}
```

**Important:** Media URLs must be:
- Publicly accessible (not behind authentication)
- HTTPS URLs
- Meet Instagram's requirements:
  - Images: JPG or PNG, max 8MB
  - Aspect ratio: 0.8 (4:5) to 1.91 (1.91:1)
  - Minimum width: 320px

### Video/Reels Post

**Request:**
```http
POST /api/posts
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "content": "Amazing video content! #reels #viral",
  "mediaUrls": [
    "https://yourdomain.com/uploads/video.mp4"
  ],
  "platforms": ["Instagram"]
}
```

**Video Requirements:**
- Format: MP4 or MOV
- Codec: H.264 or VP8
- Audio: AAC or Vorbis
- Duration: 3-60 seconds (up to 90 seconds for Reels)
- Max size: 100MB
- Aspect ratio: 0.8 to 1.91

### Carousel Post (Multiple Images)

**Request:**
```http
POST /api/posts
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "content": "Swipe to see more! 👉",
  "mediaUrls": [
    "https://yourdomain.com/uploads/image1.jpg",
    "https://yourdomain.com/uploads/image2.jpg",
    "https://yourdomain.com/uploads/image3.jpg"
  ],
  "platforms": ["Instagram"]
}
```

**Carousel Requirements:**
- 2-10 items
- Can mix images and videos
- All items must be publicly accessible

### Publish Existing Post

**Request:**
```http
POST /api/posts/{postId}/publish
Authorization: Bearer {jwt_token}
```

This will publish the post to all configured platforms.

---

## API Endpoints

### Instagram OAuth Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/instagram/auth/url` | Get Facebook OAuth authorization URL |
| GET | `/api/instagram/auth/callback` | OAuth callback handler |
| POST | `/api/instagram/accounts/{accountId}/refresh` | Refresh access token |
| GET | `/api/instagram/accounts/{accountId}` | Get account details |
| DELETE | `/api/instagram/accounts/{accountId}` | Disconnect Instagram account |

### General Endpoints (Multi-Platform)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/posts` | Create a new post |
| GET | `/api/posts` | List all posts |
| GET | `/api/posts/{id}` | Get specific post |
| POST | `/api/posts/{id}/publish` | Publish post to platforms |
| GET | `/api/analytics/posts/{id}` | Get post analytics |
| POST | `/api/analytics/posts/{id}/refresh` | Refresh analytics |

---

## Error Handling

### Common Errors

#### Invalid Access Token (Code: 190)
```json
{
  "error": "Instagram API error",
  "message": "Invalid OAuth access token",
  "code": 190
}
```
**Solution:** Reconnect the Instagram account or refresh the token.

#### Permission Denied (Code: 10)
```json
{
  "error": "Instagram API error",
  "message": "Permission denied",
  "code": 10
}
```
**Solution:** Ensure all required permissions are granted during OAuth.

#### Rate Limit Exceeded (Code: 4)
```json
{
  "error": "Instagram API error",
  "message": "Rate limit exceeded",
  "code": 4
}
```
**Solution:** Wait before retrying. The API automatically retries with exponential backoff.

#### Video Processing Error
```json
{
  "error": "Video processing failed for container {containerId}. Status code: ERROR"
}
```
**Solution:** Ensure video meets all requirements (format, size, duration, codec).

### Automatic Retry Logic

The implementation includes automatic retry for:
- Rate limiting errors (codes: 4, 17, 32)
- Network failures
- Temporary Instagram API issues

Retries use exponential backoff: 1s, 2s, 3s

---

## Testing

### 1. Test with Postman or curl

**Get Authorization URL:**
```bash
curl -X GET "https://localhost:7000/api/instagram/auth/url" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Publish a Post:**
```bash
curl -X POST "https://localhost:7000/api/posts" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Test post from API",
    "mediaUrls": ["https://example.com/image.jpg"],
    "platforms": ["Instagram"]
  }'
```

### 2. Using Swagger UI

1. Navigate to `https://localhost:7000/swagger`
2. Click "Authorize" and enter your JWT token
3. Try the Instagram endpoints

### 3. Test Token Refresh

```bash
curl -X POST "https://localhost:7000/api/instagram/accounts/1/refresh" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## Troubleshooting

### Issue: "No Instagram Business Accounts found"

**Causes:**
- Instagram account is personal (not Business/Creator)
- Instagram not connected to a Facebook Page
- Facebook Page not visible to the app

**Solutions:**
1. Convert Instagram to Business/Creator account
2. Link Instagram to Facebook Page in Instagram settings
3. Ensure you're admin of the Facebook Page

### Issue: Media URL not accessible

**Causes:**
- URL requires authentication
- URL is not HTTPS
- URL is not publicly accessible

**Solutions:**
1. Ensure media is publicly accessible
2. Use HTTPS URLs only
3. Test URL in browser (incognito mode)

### Issue: Token expires too quickly

**Causes:**
- Using short-lived token instead of long-lived token

**Solutions:**
1. The implementation automatically exchanges for long-lived tokens (60 days)
2. Set up automatic token refresh (implemented in the code)
3. Tokens refresh 7 days before expiration by default

### Issue: Container status stays "IN_PROGRESS"

**Causes:**
- Video is still processing
- Video processing failed

**Solutions:**
1. Wait up to 60 seconds for video processing
2. Check video meets all requirements
3. Review error logs for details

### Issue: Caption too long

**Error:** "Caption exceeds maximum length of 2200 characters"

**Solution:** Trim caption to 2200 characters or less.

---

## Best Practices

### 1. Token Management

- Store tokens securely (encrypted in database)
- Implement automatic token refresh (already included)
- Monitor token expiration dates
- Handle token errors gracefully

### 2. Media Hosting

- Host media on a CDN with HTTPS
- Ensure media is publicly accessible
- Validate media before uploading to Instagram
- Compress images/videos for faster uploads

### 3. Error Handling

- Implement proper error handling in your UI
- Show user-friendly error messages
- Log all API errors for debugging
- Implement retry logic for transient failures

### 4. Rate Limiting

- Respect Instagram's rate limits
- Implement request queuing if needed
- Use the built-in retry logic
- Monitor API usage in Facebook App Dashboard

### 5. Content Validation

- Validate media dimensions and aspect ratios
- Check file sizes before uploading
- Verify caption length
- Test with different content types

---

## Additional Resources

- [Instagram Graph API Documentation](https://developers.facebook.com/docs/instagram-api)
- [Content Publishing Guide](https://developers.facebook.com/docs/instagram-api/guides/content-publishing)
- [Facebook Login Documentation](https://developers.facebook.com/docs/facebook-login)
- [Instagram Media Requirements](https://developers.facebook.com/docs/instagram-api/reference/ig-user/media)

---

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review the API logs in your application
3. Check Facebook App Dashboard for errors
4. Use Graph API Explorer for testing: https://developers.facebook.com/tools/explorer/

---

## License

This implementation is part of the Social Media Management API Platform.
