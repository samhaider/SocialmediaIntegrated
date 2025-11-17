# Reddit API OAuth Integration

## Overview

This document describes the comprehensive Reddit API integration with OAuth 2.0 support for the Social Media Management API. The integration allows users to:

- Authenticate via Reddit OAuth 2.0 with proper token management
- Create posts (text, link, image, video)
- Submit comments and reply to comments
- Upload media (images)
- Edit and delete content
- Manage tokens (refresh, validate, revoke)

## Architecture

### Components

1. **Models**
   - `RedditConfiguration` - Configuration settings for Reddit API credentials and endpoints
   - `RedditDTOs.cs` - Data transfer objects for all Reddit operations

2. **Services**
   - `RedditOAuthService` - Handles OAuth 2.0 authentication flow and token management
   - `RedditApiService` - Handles Reddit API operations (posting, commenting, media upload)
   - `RedditPlatform` - Platform implementation integrating with the social media framework

3. **Controllers**
   - `RedditController` - REST API endpoints for Reddit operations

## Setup Instructions

### 1. Register a Reddit Application

1. Go to https://www.reddit.com/prefs/apps
2. Click "create another app..." or "create an app"
3. Fill in the details:
   - **Name**: Your application name
   - **App type**: Select "web app"
   - **Description**: Brief description of your app
   - **About URL**: Your website or GitHub repository
   - **Redirect URI**: `https://yourdomain.com/api/reddit/oauth/callback` (or `http://localhost:5000/api/reddit/oauth/callback` for development)
4. Click "create app"
5. Note down your **Client ID** (shown under the app name) and **Client Secret**

### 2. Configure Application Settings

Update your `appsettings.json` or `appsettings.Development.json` with Reddit credentials:

```json
{
  "Reddit": {
    "ClientId": "YOUR_REDDIT_CLIENT_ID",
    "ClientSecret": "YOUR_REDDIT_CLIENT_SECRET",
    "UserAgent": "YourApp/1.0 (by /u/yourusername)",
    "RedirectUri": "https://yourdomain.com/api/reddit/oauth/callback",
    "Scopes": "identity,read,submit,edit",
    "AuthorizationEndpoint": "https://www.reddit.com/api/v1/authorize",
    "TokenEndpoint": "https://www.reddit.com/api/v1/access_token",
    "ApiBaseUrl": "https://oauth.reddit.com",
    "RateLimitPerMinute": 60
  }
}
```

**Important Notes:**
- **UserAgent**: Reddit requires a unique User-Agent. Format: `platform:app_id:version (by /u/your_reddit_username)`
- **Scopes**: Required scopes are:
  - `identity` - Get user account information
  - `read` - Read posts and comments
  - `submit` - Submit posts and comments
  - `edit` - Edit and delete posts/comments

### 3. Build and Run

```bash
cd SocialMediaAPI
dotnet restore
dotnet build
dotnet run
```

The API will be available at `https://localhost:5001` (or `http://localhost:5000`)

## Usage Guide

### Authentication Flow

#### Step 1: Initiate OAuth Flow

**Endpoint**: `POST /api/reddit/oauth/init`

**Request**:
```json
{
  "redirectUri": "https://yourdomain.com/api/reddit/oauth/callback",
  "state": "random_string_for_csrf_protection"
}
```

**Response**:
```json
{
  "authorizationUrl": "https://www.reddit.com/api/v1/authorize?client_id=...&state=...",
  "state": "random_string_for_csrf_protection"
}
```

Redirect the user to the `authorizationUrl`. They will log in to Reddit and authorize your app.

#### Step 2: Handle OAuth Callback

After authorization, Reddit redirects to your `redirect_uri` with a `code` and `state` parameter.

**Endpoint**: `POST /api/reddit/oauth/callback`

**Request**:
```json
{
  "code": "authorization_code_from_reddit",
  "state": "same_state_from_step_1",
  "redirectUri": "https://yourdomain.com/api/reddit/oauth/callback"
}
```

**Response**:
```json
{
  "accessToken": "ACCESS_TOKEN",
  "tokenType": "bearer",
  "expiresIn": 3600,
  "refreshToken": "REFRESH_TOKEN",
  "scope": "identity read submit edit"
}
```

The access token and refresh token are automatically saved to the database for the authenticated user.

#### Step 3: Refresh Token

When the access token expires (typically after 1 hour), refresh it:

**Endpoint**: `POST /api/reddit/oauth/refresh`

**Request Body**: `"REFRESH_TOKEN"`

**Response**:
```json
{
  "accessToken": "NEW_ACCESS_TOKEN",
  "tokenType": "bearer",
  "expiresIn": 3600,
  "refreshToken": "REFRESH_TOKEN",
  "scope": "identity read submit edit"
}
```

### Creating Posts

#### Text Post (Self Post)

**Endpoint**: `POST /api/reddit/post`

**Request**:
```json
{
  "subreddit": "test",
  "title": "My First Reddit Post via API",
  "kind": "self",
  "text": "This is the body of my text post. It supports **markdown**!",
  "nsfw": false,
  "spoiler": false,
  "sendReplies": true
}
```

**Query Parameter**: `accessToken=YOUR_ACCESS_TOKEN`

#### Link Post

**Request**:
```json
{
  "subreddit": "test",
  "title": "Check out this cool website",
  "kind": "link",
  "url": "https://example.com",
  "nsfw": false,
  "spoiler": false
}
```

#### Image Post

**Endpoint**: `POST /api/reddit/post/media`

**Form Data**:
- `subreddit`: "test"
- `title`: "Check out this image"
- `file`: (image file upload)

**Query Parameter**: `accessToken=YOUR_ACCESS_TOKEN`

### Creating Comments

**Endpoint**: `POST /api/reddit/comment`

**Request**:
```json
{
  "thingId": "t3_abc123",
  "text": "This is my comment!"
}
```

**Query Parameter**: `accessToken=YOUR_ACCESS_TOKEN`

**Thing ID Formats**:
- Posts: `t3_postid`
- Comments: `t1_commentid`

### Editing Content

**Endpoint**: `PUT /api/reddit/edit`

**Query Parameters**:
- `accessToken`: Your access token
- `thingId`: Full name of thing to edit (e.g., `t3_abc123`)

**Request Body**: `"New edited text content"`

### Deleting Content

**Endpoint**: `DELETE /api/reddit/delete`

**Query Parameters**:
- `accessToken`: Your access token
- `thingId`: Full name of thing to delete

### Get User Information

**Endpoint**: `GET /api/reddit/user`

**Query Parameter**: `accessToken=YOUR_ACCESS_TOKEN`

**Response**:
```json
{
  "id": "user_id",
  "name": "username",
  "linkKarma": 1234,
  "commentKarma": 5678,
  "isGold": false,
  "isMod": false
}
```

### Get Subreddit Submit Requirements

**Endpoint**: `GET /api/reddit/subreddit/{subreddit}/requirements`

**Query Parameter**: `accessToken=YOUR_ACCESS_TOKEN`

Returns posting requirements for a specific subreddit (title length, allowed post types, etc.)

## Using Through the Social Media Platform Interface

The Reddit integration also works through the generic `ISocialMediaPlatform` interface:

```csharp
// Injected via DI
IEnumerable<ISocialMediaPlatform> platforms;

// Get Reddit platform
var reddit = platforms.FirstOrDefault(p => p.PlatformName == "Reddit");

// Publish a post
var postContent = JsonSerializer.Serialize(new {
    subreddit = "test",
    title = "My Post",
    kind = "self",
    text = "Post content"
});

var postId = await reddit.PublishPostAsync(accessToken, postContent, null);

// Reply to a comment
await reddit.ReplyToCommentAsync(accessToken, "t3_abc123", "My reply");
```

## Error Handling

### Common Errors

1. **403 Forbidden**
   - **Cause**: Missing required scope (e.g., `submit` scope)
   - **Solution**: Re-authenticate with correct scopes

2. **401 Unauthorized**
   - **Cause**: Expired or invalid access token
   - **Solution**: Refresh the access token using refresh token

3. **Rate Limit Exceeded**
   - **Cause**: Too many requests in short time
   - **Solution**: Implement rate limiting (max 60 requests/minute recommended)

4. **Subreddit Restrictions**
   - **Cause**: Subreddit has posting requirements (karma, account age, etc.)
   - **Solution**: Check submit requirements endpoint first

## Security Considerations

1. **Store Credentials Securely**
   - Never commit `appsettings.json` with real credentials
   - Use environment variables or Azure Key Vault in production
   - Store access/refresh tokens encrypted in database

2. **User Agent Requirements**
   - Reddit requires a unique User-Agent for your app
   - Follow format: `platform:app_id:version (by /u/username)`
   - Failure to use proper User-Agent may result in rate limiting

3. **State Parameter**
   - Always use a random state parameter in OAuth flow
   - Validate state in callback to prevent CSRF attacks

4. **HTTPS Only**
   - Use HTTPS for all OAuth redirects in production
   - Reddit requires HTTPS for production redirect URIs

5. **Rate Limiting**
   - Respect Reddit's rate limits (60 requests/minute)
   - Implement client-side rate limiting
   - Use exponential backoff for retries

## Reddit API Policies

Follow Reddit's **Responsible Builder Policy**:

1. **No Spam**: Don't automate excessive posting
2. **Respect User Privacy**: Don't collect user data without consent
3. **Rate Limits**: Stay within API rate limits
4. **User Agent**: Use descriptive and unique User-Agent
5. **OAuth**: Use OAuth for user authentication, not password flow
6. **Content Policy**: Ensure posted content follows Reddit's content policy

**Resources**:
- Reddit API Documentation: https://www.reddit.com/dev/api
- Responsible Builder Policy: https://support.reddithelp.com/hc/en-us/articles/16160319875092
- Reddit API Rules: https://github.com/reddit-archive/reddit/wiki/API

## Testing

### Test Subreddit

Use `/r/test` for testing post submissions. This is Reddit's official test subreddit.

### Testing Checklist

- [ ] OAuth flow works (init, callback, token exchange)
- [ ] Token refresh works
- [ ] Can create text posts
- [ ] Can create link posts
- [ ] Can upload and post images
- [ ] Can create comments
- [ ] Can edit posts/comments
- [ ] Can delete posts/comments
- [ ] Rate limiting is respected
- [ ] Error handling works correctly

## API Reference Summary

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/reddit/oauth/init` | POST | Start OAuth flow |
| `/api/reddit/oauth/callback` | POST | Handle OAuth callback |
| `/api/reddit/oauth/refresh` | POST | Refresh access token |
| `/api/reddit/user` | GET | Get user information |
| `/api/reddit/post` | POST | Submit text/link post |
| `/api/reddit/post/media` | POST | Submit image/video post |
| `/api/reddit/comment` | POST | Create comment |
| `/api/reddit/edit` | PUT | Edit post/comment |
| `/api/reddit/delete` | DELETE | Delete post/comment |
| `/api/reddit/subreddit/{name}/requirements` | GET | Get posting requirements |

## File Structure

```
SocialMediaAPI/
├── Controllers/
│   └── RedditController.cs          # Reddit API endpoints
├── DTOs/
│   └── RedditDTOs.cs                 # Request/response DTOs
├── Interfaces/
│   ├── IRedditOAuthService.cs        # OAuth service interface
│   └── IRedditApiService.cs          # API service interface
├── Models/
│   └── RedditConfiguration.cs        # Configuration model
├── Services/
│   ├── RedditOAuthService.cs         # OAuth implementation
│   ├── RedditApiService.cs           # API implementation
│   └── Platforms/
│       └── RedditPlatform.cs         # Platform integration
└── appsettings.json                  # Configuration
```

## Future Enhancements

Potential improvements for production use:

1. **Analytics**: Implement full post analytics retrieval
2. **Comments**: Full comment tree parsing and retrieval
3. **Direct Messages**: Reddit DM support
4. **Webhooks**: Real-time notifications for comments/replies
5. **Moderation**: Moderator actions (remove, approve, etc.)
6. **Search**: Search posts and comments
7. **Video Upload**: Full video upload support (currently limited)
8. **Flair Management**: Auto-select flair based on subreddit requirements
9. **Rate Limit Handling**: Automatic retry with exponential backoff
10. **Token Auto-Refresh**: Background service to auto-refresh expiring tokens

## Support

For issues or questions:
- Check Reddit API documentation: https://www.reddit.com/dev/api
- Review error logs in application
- Verify Reddit app credentials and configuration
- Test with `/r/test` subreddit first

## License

This integration follows Reddit's API Terms of Service and must comply with Reddit's Responsible Builder Policy.
