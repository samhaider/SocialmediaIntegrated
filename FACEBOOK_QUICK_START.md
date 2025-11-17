# Facebook Page Posting - Quick Start Guide

## 🚀 Quick Setup (5 Minutes)

### Step 1: Facebook App Configuration

1. Create a Facebook App at [developers.facebook.com](https://developers.facebook.com/)
2. Copy your **App ID** and **App Secret**
3. Add OAuth Redirect URI: `https://yourdomain.com/api/facebook/oauth/callback`

### Step 2: Update Configuration

Edit `appsettings.json`:

```json
{
  "Facebook": {
    "AppId": "YOUR_APP_ID_HERE",
    "AppSecret": "YOUR_APP_SECRET_HERE",
    "RedirectUri": "https://yourdomain.com/api/facebook/oauth/callback"
  }
}
```

### Step 3: Run Database Migration

```bash
cd SocialMediaAPI
dotnet ef database update
```

This will add the new Facebook Page fields to the `SocialAccounts` table.

### Step 4: Start the API

```bash
dotnet run
```

API will be available at: `https://localhost:7000` (or your configured port)

---

## 🔐 OAuth Flow (3 Steps)

### 1. Get Authorization URL

```bash
curl -X POST https://localhost:7000/api/facebook/oauth/authorize \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}'
```

Response:
```json
{
  "authorizationUrl": "https://www.facebook.com/v19.0/dialog/oauth?...",
  "state": "abc123..."
}
```

### 2. Visit URL & Grant Permissions

- Open the `authorizationUrl` in a browser
- Log in to Facebook
- Select the Page you want to manage
- Grant the requested permissions

### 3. Exchange Code for Tokens

After Facebook redirects back, use the `code` parameter:

```bash
curl -X POST https://localhost:7000/api/facebook/oauth/callback \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "CODE_FROM_FACEBOOK"
  }'
```

Response includes your pages:
```json
{
  "accessToken": "user_token...",
  "pages": [
    {
      "id": "123456789",
      "name": "My Company Page",
      "accessToken": "page_token..."
    }
  ]
}
```

### 4. Connect the Page

```bash
curl -X POST https://localhost:7000/api/facebook/pages/connect \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "pageId": "123456789",
    "pageAccessToken": "page_token...",
    "pageName": "My Company Page"
  }'
```

✅ **Done!** Your Facebook Page is now connected.

---

## 📝 Posting to Facebook

### Create & Publish a Text Post

```bash
# 1. Create the post
curl -X POST https://localhost:7000/api/posts \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Hello from our Social Media API! 🎉",
    "platforms": ["Facebook"]
  }'

# Response: { "id": 1, ... }

# 2. Publish it
curl -X POST https://localhost:7000/api/posts/1/publish \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Post with Image

```bash
curl -X POST https://localhost:7000/api/posts \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Check out our new product! 🚀",
    "platforms": ["Facebook"],
    "mediaUrls": ["https://example.com/product-image.jpg"]
  }'
```

### Schedule a Post

```bash
curl -X POST https://localhost:7000/api/posts \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Scheduled post for later",
    "platforms": ["Facebook"],
    "scheduledAt": "2025-11-18T10:00:00Z"
  }'
```

The background service will automatically publish it at the scheduled time.

---

## 📊 Get Analytics

```bash
# Get post analytics
curl -X GET https://localhost:7000/api/analytics/posts/1 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Refresh analytics from Facebook
curl -X POST https://localhost:7000/api/analytics/posts/1/refresh \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

Response:
```json
{
  "totalLikes": 42,
  "totalComments": 8,
  "totalShares": 15,
  "byPlatform": [
    {
      "platform": "Facebook",
      "likes": 42,
      "comments": 8,
      "shares": 15
    }
  ]
}
```

---

## 🧪 Testing with Swagger

1. Navigate to: `https://localhost:7000/swagger`
2. Click **"Authorize"** and enter your JWT token
3. Test the Facebook endpoints:
   - `POST /api/facebook/oauth/authorize`
   - `POST /api/facebook/oauth/callback`
   - `POST /api/facebook/pages/connect`
   - `POST /api/posts`
   - `POST /api/posts/{id}/publish`

---

## 🔍 Verify Everything Works

### Check Connected Accounts

```bash
curl -X GET https://localhost:7000/api/socialaccounts \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

Should show your Facebook Page:
```json
[
  {
    "id": 1,
    "platform": "Facebook",
    "accountName": "My Company Page",
    "isActive": true
  }
]
```

### Check Post Status

```bash
curl -X GET https://localhost:7000/api/posts/1 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

Should show:
```json
{
  "id": 1,
  "status": "published",
  "results": [
    {
      "platform": "Facebook",
      "platformPostId": "123456789_987654321",
      "status": "success"
    }
  ]
}
```

---

## ⚠️ Common Issues

### "OAuth redirect URI mismatch"
**Fix:** Ensure the redirect URI in your Facebook App Settings exactly matches the one in `appsettings.json`

### "Insufficient permissions"
**Fix:**
1. Go to Facebook App Review
2. Request advanced access for: `pages_show_list`, `pages_manage_posts`, `pages_read_engagement`

### "Access token expired"
**Fix:** Page access tokens are long-lived. If expired, reconnect the page using the OAuth flow.

### "Post not appearing on Facebook"
**Fix:**
- Verify you're using the PAGE access token (not user token)
- Check if the page requires post approval
- Ensure content doesn't violate Facebook policies

---

## 📚 Next Steps

1. **Production Setup:**
   - Move App ID/Secret to environment variables or Azure Key Vault
   - Enable HTTPS
   - Restrict CORS to your domain
   - Implement rate limiting

2. **Advanced Features:**
   - Handle comments and replies
   - Implement scheduled posts with specific times
   - Add video posting support
   - Monitor post performance

3. **Read the Full Guide:**
   - See `FACEBOOK_INTEGRATION_GUIDE.md` for complete documentation
   - Review security considerations
   - Learn about error handling

---

## 🎯 Full Example (Node.js)

```javascript
const axios = require('axios');

const API_URL = 'https://localhost:7000';
let jwtToken = 'YOUR_JWT_TOKEN';

async function connectFacebookAndPost() {
  try {
    // 1. Get auth URL
    const authRes = await axios.post(
      `${API_URL}/api/facebook/oauth/authorize`,
      {},
      { headers: { Authorization: `Bearer ${jwtToken}` } }
    );

    console.log('Visit:', authRes.data.authorizationUrl);

    // 2. After user authorizes, get the code and exchange it
    const code = 'CODE_FROM_REDIRECT'; // Get from user

    const callbackRes = await axios.post(
      `${API_URL}/api/facebook/oauth/callback`,
      { code },
      { headers: { Authorization: `Bearer ${jwtToken}` } }
    );

    const firstPage = callbackRes.data.pages[0];

    // 3. Connect the page
    await axios.post(
      `${API_URL}/api/facebook/pages/connect`,
      {
        pageId: firstPage.id,
        pageAccessToken: firstPage.accessToken,
        pageName: firstPage.name
      },
      { headers: { Authorization: `Bearer ${jwtToken}` } }
    );

    console.log('✅ Page connected!');

    // 4. Create and publish a post
    const postRes = await axios.post(
      `${API_URL}/api/posts`,
      {
        content: 'Hello from our automated posting system! 🚀',
        platforms: ['Facebook']
      },
      { headers: { Authorization: `Bearer ${jwtToken}` } }
    );

    const postId = postRes.data.id;

    // 5. Publish it
    await axios.post(
      `${API_URL}/api/posts/${postId}/publish`,
      {},
      { headers: { Authorization: `Bearer ${jwtToken}` } }
    );

    console.log('✅ Post published to Facebook!');

  } catch (error) {
    console.error('Error:', error.response?.data || error.message);
  }
}

connectFacebookAndPost();
```

---

**Ready to go!** 🎉

For detailed documentation, see `FACEBOOK_INTEGRATION_GUIDE.md`
