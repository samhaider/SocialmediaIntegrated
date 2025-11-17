# LinkedIn Integration Examples

This document provides complete, working examples for integrating with LinkedIn Company Pages using the Social Media Management API.

## Table of Contents

1. [Complete OAuth Flow Example](#complete-oauth-flow-example)
2. [Personal Account Setup](#personal-account-setup)
3. [Organization Account Setup](#organization-account-setup)
4. [Posting Examples](#posting-examples)
5. [Analytics Examples](#analytics-examples)
6. [Error Handling Examples](#error-handling-examples)
7. [cURL Examples](#curl-examples)
8. [Postman Collection](#postman-collection)

---

## Complete OAuth Flow Example

### JavaScript/TypeScript Example

```typescript
// Step 1: Register and login to get JWT token
async function authenticateUser() {
  const response = await fetch('https://api.yourdomain.com/api/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      username: 'johndoe',
      email: 'john@example.com',
      password: 'SecurePassword123!',
      firstName: 'John',
      lastName: 'Doe'
    })
  });

  const data = await response.json();
  return data.token; // Save this JWT token
}

// Step 2: Get LinkedIn authorization URL
async function getLinkedInAuthUrl(jwtToken) {
  const response = await fetch('https://api.yourdomain.com/api/linkedin/auth/url', {
    headers: {
      'Authorization': `Bearer ${jwtToken}`
    }
  });

  const data = await response.json();
  console.log('Visit this URL to authorize:', data.authorizationUrl);
  return data;
}

// Step 3: Handle the callback (after user authorizes)
async function handleCallback(jwtToken, authorizationCode) {
  const response = await fetch('https://api.yourdomain.com/api/linkedin/auth/callback', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${jwtToken}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      code: authorizationCode
    })
  });

  const data = await response.json();
  return data; // Contains accessToken, refreshToken, etc.
}

// Step 4: Get user's organizations
async function getUserOrganizations(jwtToken, linkedInAccessToken) {
  const response = await fetch(
    `https://api.yourdomain.com/api/linkedin/organizations?accessToken=${linkedInAccessToken}`,
    {
      headers: {
        'Authorization': `Bearer ${jwtToken}`
      }
    }
  );

  const data = await response.json();
  return data.organizations;
}

// Step 5: Connect organization account
async function connectOrganizationAccount(jwtToken, linkedInTokenData, organization) {
  const response = await fetch('https://api.yourdomain.com/api/socialaccounts', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${jwtToken}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      platform: 'LinkedIn',
      accessToken: linkedInTokenData.accessToken,
      refreshToken: linkedInTokenData.refreshToken,
      tokenExpiresAt: new Date(Date.now() + linkedInTokenData.expiresIn * 1000).toISOString(),
      accountType: 'Organization',
      organizationId: organization.organizationUrn,
      organizationName: organization.organizationName,
      scopes: linkedInTokenData.scope
    })
  });

  const data = await response.json();
  return data;
}

// Complete flow
async function completeLinkedInSetup() {
  // 1. Authenticate
  const jwtToken = await authenticateUser();
  console.log('Authenticated. JWT Token:', jwtToken);

  // 2. Get auth URL
  const authData = await getLinkedInAuthUrl(jwtToken);
  console.log('Authorization URL:', authData.authorizationUrl);

  // 3. User visits the URL and authorizes
  // You'll receive the code via redirect or webhook
  const authCode = 'AQT...'; // From LinkedIn redirect

  // 4. Exchange code for token
  const tokenData = await handleCallback(jwtToken, authCode);
  console.log('LinkedIn Access Token:', tokenData.accessToken);

  // 5. Get organizations
  const organizations = await getUserOrganizations(jwtToken, tokenData.accessToken);
  console.log('Organizations:', organizations);

  // 6. Connect the first organization
  if (organizations.length > 0) {
    const connectedAccount = await connectOrganizationAccount(
      jwtToken,
      tokenData,
      organizations[0]
    );
    console.log('Connected Account:', connectedAccount);
  }
}
```

---

## Personal Account Setup

### Python Example

```python
import requests
from datetime import datetime, timedelta

BASE_URL = "https://api.yourdomain.com"

def connect_personal_linkedin_account():
    # 1. Login to get JWT
    login_response = requests.post(
        f"{BASE_URL}/api/auth/login",
        json={
            "username": "johndoe",
            "password": "SecurePassword123!"
        }
    )
    jwt_token = login_response.json()["token"]
    headers = {"Authorization": f"Bearer {jwt_token}"}

    # 2. Get LinkedIn auth URL
    auth_url_response = requests.get(
        f"{BASE_URL}/api/linkedin/auth/url",
        headers=headers
    )
    auth_data = auth_url_response.json()
    print(f"Visit: {auth_data['authorizationUrl']}")

    # 3. After user authorizes, get the code
    auth_code = input("Enter the authorization code: ")

    # 4. Exchange code for token
    token_response = requests.post(
        f"{BASE_URL}/api/linkedin/auth/callback",
        headers=headers,
        json={"code": auth_code}
    )
    token_data = token_response.json()

    # 5. Connect personal account
    expires_at = (datetime.utcnow() + timedelta(seconds=token_data["expiresIn"])).isoformat() + "Z"

    connect_response = requests.post(
        f"{BASE_URL}/api/socialaccounts",
        headers={**headers, "Content-Type": "application/json"},
        json={
            "platform": "LinkedIn",
            "accessToken": token_data["accessToken"],
            "refreshToken": token_data.get("refreshToken"),
            "tokenExpiresAt": expires_at,
            "accountType": "Personal",
            "scopes": token_data.get("scope")
        }
    )

    account = connect_response.json()
    print(f"Connected Account: {account}")
    return account

if __name__ == "__main__":
    connect_personal_linkedin_account()
```

---

## Organization Account Setup

### C# Example

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

public class LinkedInIntegration
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://api.yourdomain.com";
    private string _jwtToken;

    public LinkedInIntegration()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> AuthenticateAsync(string username, string password)
    {
        var loginRequest = new
        {
            username = username,
            password = password
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/auth/login",
            loginRequest
        );

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        _jwtToken = result.Token;
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_jwtToken}");

        return _jwtToken;
    }

    public async Task<string> GetAuthorizationUrlAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/linkedin/auth/url");
        var data = await response.Content.ReadFromJsonAsync<AuthUrlResponse>();
        return data.AuthorizationUrl;
    }

    public async Task<TokenResponse> ExchangeCodeAsync(string code)
    {
        var request = new { code = code };
        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/linkedin/auth/callback",
            request
        );

        return await response.Content.ReadFromJsonAsync<TokenResponse>();
    }

    public async Task<OrganizationsResponse> GetOrganizationsAsync(string accessToken)
    {
        var response = await _httpClient.GetAsync(
            $"{_baseUrl}/api/linkedin/organizations?accessToken={accessToken}"
        );

        return await response.Content.ReadFromJsonAsync<OrganizationsResponse>();
    }

    public async Task<SocialAccountResponse> ConnectOrganizationAsync(
        string accessToken,
        string refreshToken,
        DateTime expiresAt,
        string organizationUrn,
        string organizationName,
        string scopes)
    {
        var request = new
        {
            platform = "LinkedIn",
            accessToken = accessToken,
            refreshToken = refreshToken,
            tokenExpiresAt = expiresAt,
            accountType = "Organization",
            organizationId = organizationUrn,
            organizationName = organizationName,
            scopes = scopes
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/socialaccounts",
            request
        );

        return await response.Content.ReadFromJsonAsync<SocialAccountResponse>();
    }

    public async Task<SocialAccountResponse> SetupOrganizationAccountAsync()
    {
        // 1. Authenticate
        await AuthenticateAsync("johndoe", "SecurePassword123!");

        // 2. Get auth URL
        var authUrl = await GetAuthorizationUrlAsync();
        Console.WriteLine($"Visit this URL: {authUrl}");

        Console.Write("Enter authorization code: ");
        var code = Console.ReadLine();

        // 3. Exchange code
        var tokenData = await ExchangeCodeAsync(code);

        // 4. Get organizations
        var orgs = await GetOrganizationsAsync(tokenData.AccessToken);

        if (orgs.Organizations.Count == 0)
        {
            throw new Exception("No organizations found");
        }

        // 5. Connect first organization
        var org = orgs.Organizations[0];
        var expiresAt = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);

        return await ConnectOrganizationAsync(
            tokenData.AccessToken,
            tokenData.RefreshToken,
            expiresAt,
            org.OrganizationUrn,
            org.OrganizationName,
            tokenData.Scope
        );
    }
}

// DTOs
public class LoginResponse { public string Token { get; set; } }
public class AuthUrlResponse { public string AuthorizationUrl { get; set; } }
public class TokenResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public string Scope { get; set; }
}
public class Organization
{
    public string OrganizationUrn { get; set; }
    public string OrganizationName { get; set; }
}
public class OrganizationsResponse
{
    public List<Organization> Organizations { get; set; }
}
public class SocialAccountResponse
{
    public int Id { get; set; }
    public string Platform { get; set; }
}
```

---

## Posting Examples

### Example 1: Simple Text Post

```bash
curl -X POST https://api.yourdomain.com/api/posts \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Excited to share our latest product innovation! 🚀 #innovation #tech",
    "platforms": ["LinkedIn"]
  }'
```

### Example 2: Post with Image

```javascript
async function postWithImage(jwtToken) {
  // First, upload the media
  const formData = new FormData();
  formData.append('file', imageFile);

  const mediaResponse = await fetch('https://api.yourdomain.com/api/media/upload', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${jwtToken}`
    },
    body: formData
  });

  const mediaData = await mediaResponse.json();

  // Then create the post
  const postResponse = await fetch('https://api.yourdomain.com/api/posts', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${jwtToken}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      content: 'Check out our latest infographic! 📊',
      mediaUrls: [mediaData.url],
      platforms: ['LinkedIn']
    })
  });

  return await postResponse.json();
}
```

### Example 3: Scheduled Post

```python
import requests
from datetime import datetime, timedelta

def schedule_linkedin_post(jwt_token, content, schedule_time):
    response = requests.post(
        "https://api.yourdomain.com/api/posts",
        headers={
            "Authorization": f"Bearer {jwt_token}",
            "Content-Type": "application/json"
        },
        json={
            "content": content,
            "platforms": ["LinkedIn"],
            "scheduledAt": schedule_time.isoformat() + "Z"
        }
    )
    return response.json()

# Schedule a post for tomorrow at 9 AM
tomorrow_9am = datetime.now() + timedelta(days=1)
tomorrow_9am = tomorrow_9am.replace(hour=9, minute=0, second=0, microsecond=0)

post = schedule_linkedin_post(
    jwt_token="YOUR_JWT_TOKEN",
    content="Good morning! Here's our weekly industry update 📰",
    schedule_time=tomorrow_9am
)
print(f"Post scheduled: {post}")
```

### Example 4: Multi-Platform Post

```json
{
  "content": "Big announcement coming tomorrow! Stay tuned 👀 #comingsoon",
  "platforms": ["LinkedIn", "Twitter", "Facebook"],
  "scheduledAt": "2025-11-18T10:00:00Z"
}
```

---

## Analytics Examples

### Example 1: Get Post Analytics

```javascript
async function getPostAnalytics(jwtToken, postId) {
  const response = await fetch(
    `https://api.yourdomain.com/api/analytics/posts/${postId}`,
    {
      headers: {
        'Authorization': `Bearer ${jwtToken}`
      }
    }
  );

  const data = await response.json();

  // Display analytics
  data.analytics.forEach(platform => {
    console.log(`${platform.platform} Analytics:`);
    console.log(`  Likes: ${platform.likes}`);
    console.log(`  Comments: ${platform.comments}`);
    console.log(`  Shares: ${platform.shares}`);
    console.log(`  Views: ${platform.views}`);
    console.log(`  Engagement Rate: ${(platform.engagement * 100).toFixed(2)}%`);
  });

  return data;
}
```

### Example 2: Refresh and Compare Analytics

```python
import requests
import time

def track_post_performance(jwt_token, post_id, duration_hours=24):
    base_url = "https://api.yourdomain.com"
    headers = {"Authorization": f"Bearer {jwt_token}"}

    analytics_history = []

    for hour in range(duration_hours):
        # Refresh analytics
        requests.post(
            f"{base_url}/api/analytics/posts/{post_id}/refresh",
            headers=headers
        )

        # Get updated analytics
        response = requests.get(
            f"{base_url}/api/analytics/posts/{post_id}",
            headers=headers
        )

        analytics = response.json()
        analytics_history.append({
            "hour": hour,
            "data": analytics
        })

        # Wait 1 hour
        if hour < duration_hours - 1:
            time.sleep(3600)

    return analytics_history

# Track performance for 24 hours
history = track_post_performance("YOUR_JWT_TOKEN", 123, 24)
```

---

## Error Handling Examples

### Example 1: Token Refresh

```javascript
async function makeAuthenticatedRequest(url, options = {}) {
  let accessToken = localStorage.getItem('linkedinAccessToken');

  try {
    const response = await fetch(url, {
      ...options,
      headers: {
        ...options.headers,
        'Authorization': `Bearer ${accessToken}`
      }
    });

    if (response.status === 401) {
      // Token expired, refresh it
      const refreshToken = localStorage.getItem('linkedinRefreshToken');
      const refreshResponse = await fetch(
        'https://api.yourdomain.com/api/linkedin/auth/refresh',
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ refreshToken })
        }
      );

      const newTokenData = await refreshResponse.json();
      localStorage.setItem('linkedinAccessToken', newTokenData.accessToken);

      // Retry original request
      return await fetch(url, {
        ...options,
        headers: {
          ...options.headers,
          'Authorization': `Bearer ${newTokenData.accessToken}`
        }
      });
    }

    return response;
  } catch (error) {
    console.error('Request failed:', error);
    throw error;
  }
}
```

### Example 2: Retry Logic

```python
import requests
from time import sleep

def post_with_retry(jwt_token, content, max_retries=3):
    url = "https://api.yourdomain.com/api/posts"
    headers = {
        "Authorization": f"Bearer {jwt_token}",
        "Content-Type": "application/json"
    }
    data = {
        "content": content,
        "platforms": ["LinkedIn"]
    }

    for attempt in range(max_retries):
        try:
            response = requests.post(url, headers=headers, json=data)

            if response.status_code == 429:  # Rate limit
                wait_time = 2 ** attempt  # Exponential backoff
                print(f"Rate limited. Waiting {wait_time} seconds...")
                sleep(wait_time)
                continue

            response.raise_for_status()
            return response.json()

        except requests.exceptions.RequestException as e:
            if attempt == max_retries - 1:
                raise
            wait_time = 2 ** attempt
            print(f"Request failed. Retrying in {wait_time} seconds...")
            sleep(wait_time)

    raise Exception("Max retries exceeded")
```

---

## cURL Examples

### Complete Workflow

```bash
#!/bin/bash

# 1. Register
curl -X POST https://api.yourdomain.com/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "johndoe",
    "email": "john@example.com",
    "password": "SecurePassword123!",
    "firstName": "John",
    "lastName": "Doe"
  }'

# 2. Login
JWT_TOKEN=$(curl -X POST https://api.yourdomain.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "johndoe",
    "password": "SecurePassword123!"
  }' | jq -r '.token')

# 3. Get LinkedIn auth URL
curl -X GET https://api.yourdomain.com/api/linkedin/auth/url \
  -H "Authorization: Bearer $JWT_TOKEN"

# 4. Exchange code (after user authorizes)
LINKEDIN_TOKEN=$(curl -X POST https://api.yourdomain.com/api/linkedin/auth/callback \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "AQT..."
  }' | jq -r '.accessToken')

# 5. Get organizations
curl -X GET "https://api.yourdomain.com/api/linkedin/organizations?accessToken=$LINKEDIN_TOKEN" \
  -H "Authorization: Bearer $JWT_TOKEN"

# 6. Connect organization account
curl -X POST https://api.yourdomain.com/api/socialaccounts \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "platform": "LinkedIn",
    "accessToken": "'$LINKEDIN_TOKEN'",
    "accountType": "Organization",
    "organizationId": "urn:li:organization:123456",
    "organizationName": "Acme Corporation",
    "scopes": "openid,profile,email,w_organization_social"
  }'

# 7. Create a post
curl -X POST https://api.yourdomain.com/api/posts \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Hello LinkedIn! 👋",
    "platforms": ["LinkedIn"]
  }'
```

---

## Postman Collection

### Import this JSON into Postman

```json
{
  "info": {
    "name": "LinkedIn Integration",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "1. Register",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"username\": \"johndoe\",\n  \"email\": \"john@example.com\",\n  \"password\": \"SecurePassword123!\",\n  \"firstName\": \"John\",\n  \"lastName\": \"Doe\"\n}"
        },
        "url": {
          "raw": "{{baseUrl}}/api/auth/register",
          "host": ["{{baseUrl}}"],
          "path": ["api", "auth", "register"]
        }
      }
    },
    {
      "name": "2. Login",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"username\": \"johndoe\",\n  \"password\": \"SecurePassword123!\"\n}"
        },
        "url": {
          "raw": "{{baseUrl}}/api/auth/login",
          "host": ["{{baseUrl}}"],
          "path": ["api", "auth", "login"]
        }
      }
    },
    {
      "name": "3. Get LinkedIn Auth URL",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwtToken}}"
          }
        ],
        "url": {
          "raw": "{{baseUrl}}/api/linkedin/auth/url",
          "host": ["{{baseUrl}}"],
          "path": ["api", "linkedin", "auth", "url"]
        }
      }
    },
    {
      "name": "4. Exchange Code for Token",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwtToken}}"
          },
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"code\": \"{{linkedinAuthCode}}\"\n}"
        },
        "url": {
          "raw": "{{baseUrl}}/api/linkedin/auth/callback",
          "host": ["{{baseUrl}}"],
          "path": ["api", "linkedin", "auth", "callback"]
        }
      }
    },
    {
      "name": "5. Get Organizations",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwtToken}}"
          }
        ],
        "url": {
          "raw": "{{baseUrl}}/api/linkedin/organizations?accessToken={{linkedinAccessToken}}",
          "host": ["{{baseUrl}}"],
          "path": ["api", "linkedin", "organizations"],
          "query": [
            {
              "key": "accessToken",
              "value": "{{linkedinAccessToken}}"
            }
          ]
        }
      }
    },
    {
      "name": "6. Connect Organization Account",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwtToken}}"
          },
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"platform\": \"LinkedIn\",\n  \"accessToken\": \"{{linkedinAccessToken}}\",\n  \"refreshToken\": \"{{linkedinRefreshToken}}\",\n  \"accountType\": \"Organization\",\n  \"organizationId\": \"{{organizationUrn}}\",\n  \"organizationName\": \"{{organizationName}}\",\n  \"scopes\": \"openid,profile,email,w_organization_social,r_organization_social\"\n}"
        },
        "url": {
          "raw": "{{baseUrl}}/api/socialaccounts",
          "host": ["{{baseUrl}}"],
          "path": ["api", "socialaccounts"]
        }
      }
    },
    {
      "name": "7. Create Post",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwtToken}}"
          },
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"content\": \"Hello LinkedIn! 👋 #test\",\n  \"platforms\": [\"LinkedIn\"]\n}"
        },
        "url": {
          "raw": "{{baseUrl}}/api/posts",
          "host": ["{{baseUrl}}"],
          "path": ["api", "posts"]
        }
      }
    },
    {
      "name": "8. Get Post Analytics",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwtToken}}"
          }
        ],
        "url": {
          "raw": "{{baseUrl}}/api/analytics/posts/{{postId}}",
          "host": ["{{baseUrl}}"],
          "path": ["api", "analytics", "posts", "{{postId}}"]
        }
      }
    }
  ],
  "variable": [
    {
      "key": "baseUrl",
      "value": "https://api.yourdomain.com"
    },
    {
      "key": "jwtToken",
      "value": ""
    },
    {
      "key": "linkedinAccessToken",
      "value": ""
    },
    {
      "key": "linkedinRefreshToken",
      "value": ""
    },
    {
      "key": "linkedinAuthCode",
      "value": ""
    },
    {
      "key": "organizationUrn",
      "value": ""
    },
    {
      "key": "organizationName",
      "value": ""
    },
    {
      "key": "postId",
      "value": ""
    }
  ]
}
```

---

## Testing Checklist

Use this checklist to verify your integration:

- [ ] Successfully register a user
- [ ] Successfully login and receive JWT token
- [ ] Get LinkedIn authorization URL
- [ ] Complete OAuth flow and receive access token
- [ ] Retrieve user information
- [ ] Retrieve list of organizations
- [ ] Connect personal LinkedIn account
- [ ] Connect organization (company page) account
- [ ] Create text-only post
- [ ] Create post with image
- [ ] Create post with video
- [ ] Create scheduled post
- [ ] Retrieve post analytics
- [ ] Refresh post analytics
- [ ] Handle expired token (refresh)
- [ ] Handle rate limiting
- [ ] Handle various error scenarios
- [ ] Verify posts appear on LinkedIn
- [ ] Verify analytics match LinkedIn's dashboard

---

## Next Steps

1. Review the [LinkedIn Integration Guide](LINKEDIN_INTEGRATION_GUIDE.md) for detailed documentation
2. Test all endpoints using the examples above
3. Implement error handling in your production code
4. Set up monitoring and logging
5. Apply for LinkedIn Marketing Developer Platform access
6. Configure your production environment

## Support

If you encounter issues:
1. Check the error messages in the API response
2. Review the [Troubleshooting section](LINKEDIN_INTEGRATION_GUIDE.md#troubleshooting) in the guide
3. Verify your LinkedIn app configuration
4. Ensure you have the correct permissions and scopes
5. Check LinkedIn's API status page

