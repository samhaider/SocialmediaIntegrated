# Facebook Page Posting Integration Guide

## Overview

This guide explains how to integrate Facebook Page posting into the Social Media Management API. The implementation follows Facebook's official OAuth 2.0 flow and Graph API v19.0 specifications.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Facebook App Setup](#facebook-app-setup)
3. [Configuration](#configuration)
4. [OAuth Flow](#oauth-flow)
5. [API Endpoints](#api-endpoints)
6. [Usage Examples](#usage-examples)
7. [Error Handling](#error-handling)
8. [Security Considerations](#security-considerations)
9. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Facebook Permissions

The following permissions are required for full functionality:

- **`pages_show_list`** - Required to retrieve the list of Pages the user manages
- **`pages_manage_posts`** - Required to create and publish posts on behalf of the Page
- **`pages_read_engagement`** - Required to access Page analytics and insights
- **`public_profile`** - Required for basic user authentication
- **`email`** - Optional, for user identification

### Development Requirements

- **Facebook Developer Account**: [Create one here](https://developers.facebook.com/)
- **Facebook App**: Register an app in the Facebook Developer Console
- **Company Facebook Page**: Must have admin rights to the Page
- **.NET 8.0 SDK**: Latest version installed
- **SQL Server**: For database storage

---

## Facebook App Setup

### Step 1: Create a Facebook App

1. Go to [Facebook Developers](https://developers.facebook.com/)
2. Click **"My Apps"** → **"Create App"**
3. Select **"Business"** as the app type
4. Fill in the app details:
   - **App Name**: Your application name
   - **App Contact Email**: Your email address
   - **Business Account**: Select or create a business account

### Step 2: Configure App Settings

1. In the app dashboard, go to **Settings** → **Basic**
2. Note down your:
   - **App ID**
   - **App Secret** (click "Show" to reveal)
3. Add your **App Domains** (e.g., `yourdomain.com`)

### Step 3: Add Facebook Login Product

1. In the left sidebar, click **"Add Product"**
2. Find **"Facebook Login"** and click **"Set Up"**
3. Select **"Web"** platform
4. Enter your **Site URL**: `https://yourdomain.com`

### Step 4: Configure OAuth Redirect URIs

1. Go to **Facebook Login** → **Settings**
2. Add your OAuth redirect URI:
   ```
   https://yourdomain.com/api/facebook/oauth/callback
   ```
3. Save changes

### Step 5: Request Advanced Access

1. Go to **App Review** → **Permissions and Features**
2. Request advanced access for:
   - `pages_show_list`
   - `pages_manage_posts`
   - `pages_read_engagement`
3. Follow Facebook's review process (may require business verification)

---

## Configuration

### Update appsettings.json

Add the following configuration to your `appsettings.json`:

```json
{
  "Facebook": {
    "AppId": "YOUR_FACEBOOK_APP_ID",
    "AppSecret": "YOUR_FACEBOOK_APP_SECRET",
    "ApiVersion": "v19.0",
    "GraphApiBaseUrl": "https://graph.facebook.com",
    "RedirectUri": "https://yourdomain.com/api/facebook/oauth/callback",
    "Scopes": [
      "pages_show_list",
      "pages_manage_posts",
      "pages_read_engagement",
      "public_profile",
      "email"
    ],
    "RateLimitPerHour": 200,
    "TokenExchangeUrl": "https://graph.facebook.com/v19.0/oauth/access_token",
    "LongLivedTokenUrl": "https://graph.facebook.com/v19.0/oauth/access_token"
  }
}
```

### Environment Variables (Production)

For production, use environment variables or Azure Key Vault:

```bash
Facebook__AppId=your_app_id
Facebook__AppSecret=your_app_secret
```

---

## OAuth Flow

### Complete OAuth Flow Diagram

```
┌─────────┐                ┌──────────┐                ┌──────────┐
│ Client  │                │   API    │                │ Facebook │
└────┬────┘                └─────┬────┘                └─────┬────┘
     │                           │                           │
     │ 1. Request Auth URL       │                           │
     ├──────────────────────────>│                           │
     │                           │                           │
     │ 2. Return Auth URL        │                           │
     │<──────────────────────────┤                           │
     │                           │                           │
     │ 3. Redirect to Facebook   │                           │
     ├───────────────────────────────────────────────────────>│
     │                           │                           │
     │ 4. User Grants Permissions│                           │
     │<───────────────────────────────────────────────────────┤
     │                           │                           │
     │ 5. Callback with Code     │                           │
     ├──────────────────────────>│                           │
     │                           │                           │
     │                           │ 6. Exchange Code for Token│
     │                           ├──────────────────────────>│
     │                           │                           │
     │                           │ 7. Access Token + Pages   │
     │                           │<──────────────────────────┤
     │                           │                           │
     │ 8. Return Pages List      │                           │
     │<──────────────────────────┤                           │
     │                           │                           │
     │ 9. Select & Connect Page  │                           │
     ├──────────────────────────>│                           │
     │                           │                           │
     │ 10. Page Connected        │                           │
     │<──────────────────────────┤                           │
```

---

## API Endpoints

### 1. Get Authorization URL

**Endpoint:** `POST /api/facebook/oauth/authorize`

**Authentication:** Required (JWT Bearer Token)

**Request Body:**
```json
{
  "state": "optional_csrf_token",
  "redirectUri": "https://yourdomain.com/custom/callback"
}
```

**Response:**
```json
{
  "authorizationUrl": "https://www.facebook.com/v19.0/dialog/oauth?client_id=...",
  "state": "csrf_token_value"
}
```

### 2. Handle OAuth Callback

**Endpoint:** `POST /api/facebook/oauth/callback`

**Authentication:** Required (JWT Bearer Token)

**Request Body:**
```json
{
  "code": "AUTH_CODE_FROM_FACEBOOK",
  "state": "csrf_token_value",
  "redirectUri": "https://yourdomain.com/api/facebook/oauth/callback"
}
```

**Response:**
```json
{
  "accessToken": "short_lived_user_token",
  "tokenType": "bearer",
  "expiresIn": 3600,
  "pages": [
    {
      "id": "PAGE_ID",
      "name": "My Company Page",
      "accessToken": "LONG_LIVED_PAGE_TOKEN",
      "category": "Business",
      "tasks": ["ANALYZE", "ADVERTISE", "MODERATE", "CREATE_CONTENT"]
    }
  ]
}
```

### 3. Get User's Pages

**Endpoint:** `GET /api/facebook/pages?accessToken=USER_ACCESS_TOKEN`

**Authentication:** Required (JWT Bearer Token)

**Response:**
```json
[
  {
    "id": "PAGE_ID",
    "name": "My Company Page",
    "accessToken": "LONG_LIVED_PAGE_TOKEN",
    "category": "Business",
    "tasks": ["ANALYZE", "ADVERTISE", "MODERATE", "CREATE_CONTENT"]
  }
]
```

### 4. Connect a Facebook Page

**Endpoint:** `POST /api/facebook/pages/connect`

**Authentication:** Required (JWT Bearer Token)

**Request Body:**
```json
{
  "pageId": "PAGE_ID",
  "pageAccessToken": "LONG_LIVED_PAGE_TOKEN",
  "pageName": "My Company Page",
  "metadata": "{\"category\":\"Business\",\"followers\":5000}"
}
```

**Response:**
```json
{
  "id": 1,
  "platform": "Facebook",
  "accountId": "PAGE_ID",
  "accountName": "My Company Page",
  "isActive": true,
  "connectedAt": "2025-11-17T12:00:00Z"
}
```

### 5. Extend Token (Short-lived to Long-lived)

**Endpoint:** `POST /api/facebook/token/extend`

**Authentication:** Required (JWT Bearer Token)

**Request Body:**
```json
"SHORT_LIVED_TOKEN"
```

**Response:**
```json
{
  "accessToken": "LONG_LIVED_TOKEN",
  "tokenType": "bearer",
  "expiresIn": 5184000
}
```

### 6. Create a Post

**Endpoint:** `POST /api/posts`

**Authentication:** Required (JWT Bearer Token)

**Request Body:**
```json
{
  "content": "Check out our new product launch! 🚀",
  "platforms": ["Facebook"],
  "mediaUrls": ["https://example.com/image.jpg"],
  "scheduledAt": null
}
```

**Response:**
```json
{
  "id": 1,
  "content": "Check out our new product launch! 🚀",
  "platforms": ["Facebook"],
  "status": "draft",
  "createdAt": "2025-11-17T12:00:00Z"
}
```

### 7. Publish a Post

**Endpoint:** `POST /api/posts/{id}/publish`

**Authentication:** Required (JWT Bearer Token)

**Response:**
```json
{
  "id": 1,
  "content": "Check out our new product launch! 🚀",
  "platforms": ["Facebook"],
  "status": "published",
  "publishedAt": "2025-11-17T12:05:00Z",
  "results": [
    {
      "platform": "Facebook",
      "platformPostId": "PAGE_ID_POST_ID",
      "status": "success"
    }
  ]
}
```

---

## Usage Examples

### Example 1: Complete OAuth Flow (C# Client)

```csharp
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

public class FacebookAuthExample
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl = "https://yourdomain.com";
    private readonly string _jwtToken = "YOUR_JWT_TOKEN";

    public async Task ConnectFacebookPageAsync()
    {
        // Step 1: Get authorization URL
        var authUrlRequest = new
        {
            state = Guid.NewGuid().ToString()
        };

        var authUrlResponse = await _httpClient.PostAsJsonAsync(
            $"{_apiBaseUrl}/api/facebook/oauth/authorize",
            authUrlRequest);

        var authData = await authUrlResponse.Content.ReadFromJsonAsync<JsonElement>();
        var authUrl = authData.GetProperty("authorizationUrl").GetString();

        Console.WriteLine($"Visit this URL: {authUrl}");

        // Step 2: User visits URL and grants permissions
        // Facebook redirects back with code parameter

        Console.Write("Enter the code from the redirect URL: ");
        var code = Console.ReadLine();

        // Step 3: Exchange code for tokens
        var callbackRequest = new
        {
            code = code,
            state = authUrlRequest.state,
            redirectUri = "https://yourdomain.com/api/facebook/oauth/callback"
        };

        var callbackResponse = await _httpClient.PostAsJsonAsync(
            $"{_apiBaseUrl}/api/facebook/oauth/callback",
            callbackRequest);

        var callbackData = await callbackResponse.Content.ReadFromJsonAsync<JsonElement>();
        var pages = callbackData.GetProperty("pages").EnumerateArray().ToList();

        Console.WriteLine($"Found {pages.Count} pages:");
        foreach (var page in pages)
        {
            Console.WriteLine($"- {page.GetProperty("name").GetString()}");
        }

        // Step 4: Connect the first page
        var firstPage = pages.First();
        var connectRequest = new
        {
            pageId = firstPage.GetProperty("id").GetString(),
            pageAccessToken = firstPage.GetProperty("accessToken").GetString(),
            pageName = firstPage.GetProperty("name").GetString()
        };

        var connectResponse = await _httpClient.PostAsJsonAsync(
            $"{_apiBaseUrl}/api/facebook/pages/connect",
            connectRequest);

        Console.WriteLine("Facebook Page connected successfully!");
    }
}
```

### Example 2: Posting to Facebook (JavaScript/Fetch)

```javascript
async function postToFacebook(content, mediaUrl = null) {
  const jwtToken = localStorage.getItem('jwtToken');

  // Create post
  const createResponse = await fetch('https://yourdomain.com/api/posts', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${jwtToken}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      content: content,
      platforms: ['Facebook'],
      mediaUrls: mediaUrl ? [mediaUrl] : null
    })
  });

  const post = await createResponse.json();
  console.log('Post created:', post.id);

  // Publish post
  const publishResponse = await fetch(
    `https://yourdomain.com/api/posts/${post.id}/publish`,
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${jwtToken}`
      }
    }
  );

  const result = await publishResponse.json();
  console.log('Published:', result);
  return result;
}

// Usage
postToFacebook('Hello from our Social Media API! 🎉', 'https://example.com/image.jpg');
```

### Example 3: Schedule a Post (Python)

```python
import requests
from datetime import datetime, timedelta

API_BASE_URL = "https://yourdomain.com"
JWT_TOKEN = "YOUR_JWT_TOKEN"

headers = {
    "Authorization": f"Bearer {JWT_TOKEN}",
    "Content-Type": "application/json"
}

# Schedule post for 2 hours from now
scheduled_time = datetime.utcnow() + timedelta(hours=2)

post_data = {
    "content": "Exciting announcement coming soon! Stay tuned 👀",
    "platforms": ["Facebook"],
    "scheduledAt": scheduled_time.isoformat() + "Z"
}

response = requests.post(
    f"{API_BASE_URL}/api/posts",
    json=post_data,
    headers=headers
)

post = response.json()
print(f"Post scheduled for {scheduled_time}: {post['id']}")
```

---

## Error Handling

### Common Error Codes

| Error Code | Description | Solution |
|-----------|-------------|----------|
| `400` | Invalid request parameters | Check request body format |
| `401` | Invalid or expired access token | Re-authenticate the user |
| `403` | Insufficient permissions | Request additional permissions |
| `404` | Post or page not found | Verify the ID exists |
| `429` | Rate limit exceeded | Implement backoff strategy |
| `500` | Internal server error | Check logs, contact support |

### Facebook-Specific Errors

| Error Type | Error Code | Description |
|-----------|-----------|-------------|
| `OAuthException` | 190 | Access token expired or invalid |
| `OAuthException` | 200 | Missing permissions |
| `GraphMethodException` | 100 | Invalid parameter |
| `GraphMethodException` | 368 | Page request limit reached |

### Error Response Format

```json
{
  "message": "Failed to publish post",
  "error": "Invalid OAuth access token - Cannot parse access token"
}
```

### Retry Logic Example

```csharp
public async Task<string> PublishWithRetryAsync(int postId, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/posts/{postId}/publish", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                var delay = Math.Pow(2, attempt) * 1000; // Exponential backoff
                await Task.Delay((int)delay);
                continue;
            }

            throw new Exception($"Failed with status: {response.StatusCode}");
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            Console.WriteLine($"Attempt {attempt} failed: {ex.Message}");
        }
    }

    throw new Exception("Max retries exceeded");
}
```

---

## Security Considerations

### Access Token Security

1. **Never expose tokens in client-side code**
   - Store tokens server-side only
   - Use secure database storage
   - Encrypt tokens at rest

2. **Use HTTPS for all API calls**
   - Facebook requires HTTPS for OAuth callbacks
   - Enforce HTTPS in production

3. **Implement CSRF protection**
   - Use the `state` parameter in OAuth flow
   - Validate state on callback

4. **Token expiration handling**
   - User access tokens expire in ~2 hours
   - Page access tokens don't expire (unless password changed)
   - Implement token refresh logic

### Configuration Security

```csharp
// Use Azure Key Vault or similar in production
builder.Configuration.AddAzureKeyVault(
    $"https://{keyVaultName}.vault.azure.net/",
    new DefaultAzureCredential());
```

### Rate Limiting

Implement rate limiting to avoid hitting Facebook's limits:

```csharp
// Add to Program.cs
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IRateLimitService, RateLimitService>();
```

---

## Troubleshooting

### Issue 1: "OAuth Redirect URI Mismatch"

**Symptom:** Error during OAuth flow

**Solution:**
- Verify redirect URI in Facebook App Settings matches exactly
- Check for trailing slashes
- Ensure HTTPS in production

### Issue 2: "Insufficient Permissions"

**Symptom:** Cannot post or access pages

**Solution:**
- Request advanced access for required permissions
- Complete Facebook's business verification
- Ensure user is admin of the page

### Issue 3: "Access Token Expired"

**Symptom:** API calls fail with 401 error

**Solution:**
- Implement token refresh logic
- Use page access tokens (long-lived)
- Store token expiration time and check before use

### Issue 4: "Post Not Appearing on Page"

**Symptom:** Post API succeeds but post doesn't show

**Solution:**
- Verify you're using PAGE access token, not user token
- Check page settings (post approval requirements)
- Ensure content doesn't violate Facebook policies

### Debug Mode

Enable debug logging in appsettings.json:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SocialMediaAPI.Services.Platforms.FacebookPlatform": "Debug",
      "SocialMediaAPI.Controllers.FacebookAuthController": "Debug"
    }
  }
}
```

---

## Additional Resources

- [Facebook Graph API Documentation](https://developers.facebook.com/docs/graph-api)
- [Facebook Login Documentation](https://developers.facebook.com/docs/facebook-login/)
- [Page Publishing Best Practices](https://developers.facebook.com/docs/pages/publishing)
- [Facebook Platform Policy](https://developers.facebook.com/policy/)

---

## Support

For issues or questions:
1. Check the [troubleshooting section](#troubleshooting)
2. Review Facebook's developer documentation
3. Contact your development team
4. Submit an issue to the project repository

---

**Last Updated:** November 17, 2025
**API Version:** v19.0
**Implementation Version:** 1.0.0
