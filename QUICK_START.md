# Quick Start Guide - Task 3 STT Implementation

## For Backend Developers

### Prerequisites
- .NET 8.0 SDK
- Groq API key (free at https://console.groq.com/)

### Setup Steps

1. **Configure API Keys:**
   ```bash
   # Edit appsettings.Development.json
   # Add your Groq API key
   "GroqSettings": {
     "ApiKey": "gsk_your_api_key_here"
   }
   ```

2. **Run Application:**
   ```bash
   dotnet restore
   dotnet run
   ```

3. **Access Swagger UI:**
   ```
   https://localhost:5001/swagger
   ```

### Testing Transcription

**Option 1: Using Swagger UI**
1. Register: POST `/api/auth/register`
2. Login: POST `/api/auth/login` (copy the token)
3. Click "Authorize" in Swagger and paste your token
4. Test: POST `/api/transcribe` with audio data

**Option 2: Using HTTP File**
1. Open `TranscriptionApi.http`
2. Replace `YOUR_JWT_TOKEN_HERE`
3. Execute requests (each request has tests)

**Option 3: Using curl**
```bash
# Register
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"TestPassword123!"}'

# Login
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"TestPassword123!"}' \
  | jq -r '.token'

# Transcribe (replace TOKEN and audioData)
curl -X POST https://localhost:5001/api/transcribe \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer TOKEN" \
  -d '{
    "audioData": [137, 80, 78, 71],
    "language": "en",
    "provider": "GroqWhisper"
  }'
```

### Testing Fallback Behavior

1. Stop Whisper.cpp (if running)
2. Don't provide OpenAI API key
3. Set Groq to fail (use invalid API key)
4. Try transcription - should fail gracefully with error

## For Extension Developers

### Prerequisites
- Chrome/Edge/Firefox/Safari browser
- JavaScript/HTML/CSS knowledge
- Audio Assistant backend URL

### Quick Integration

**Step 1: Create Transcription Client**
```javascript
class TranscriptionClient {
  constructor(baseUrl, authToken) {
    this.baseUrl = baseUrl;
    this.authToken = authToken;
  }

  async transcribe(audioData, language = 'en') {
    const response = await fetch(`${this.baseUrl}/api/transcribe`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.authToken}`
      },
      body: JSON.stringify({
        audioData: Array.from(audioData),
        language,
        streaming: false
      })
    });

    if (!response.ok) {
      throw new Error('Transcription failed');
    }

    return await response.json();
  }
}
```

**Step 2: Record Audio**
```javascript
async function recordAudio() {
  const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
  const mediaRecorder = new MediaRecorder(stream);
  const chunks = [];

  mediaRecorder.ondataavailable = (e) => chunks.push(e.data);
  
  return new Promise((resolve) => {
    mediaRecorder.onstop = () => {
      const blob = new Blob(chunks, { type: 'audio/webm' });
      stream.getTracks().forEach(t => t.stop());
      resolve(blob);
    };
    
    mediaRecorder.start();
    setTimeout(() => mediaRecorder.stop(), 5000); // 5 seconds
  });
}
```

**Step 3: Test Integration**
```javascript
async function testTranscription() {
  const audioBlob = await recordAudio();
  const audioData = await blobToArrayBuffer(audioBlob);
  
  const client = new TranscriptionClient('https://your-backend-url.com', 'your-jwt-token');
  
  const result = await client.transcribe(audioData, 'en');
  console.log('Transcript:', result.text);
  console.log('Provider:', result.provider);
  console.log('Confidence:', result.confidence);
}

function blobToArrayBuffer(blob) {
  return new Promise((resolve) => {
    const reader = new FileReader();
    reader.onloadend = () => resolve(reader.result);
    reader.readAsArrayBuffer(blob);
  });
}

testTranscription();
```

### Testing Different Providers

**Test Groq (Primary):**
```javascript
const result = await client.transcribe(audioData, 'en', 'GroqWhisper');
console.log('Used:', result.provider); // Should be "GroqWhisper"
```

**Test OpenAI (with API key):**
```javascript
// First, store your OpenAI API key
await fetch('https://your-backend-url.com/api/apikey/store', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer ' + token
  },
  body: JSON.stringify({
    provider: 'OpenAIWhisper',
    apiKey: 'sk-your-openai-key'
  })
});

// Then use OpenAI for transcription
const result = await client.transcribe(audioData, 'en', 'OpenAIWhisper');
```

**Test Fallback:**
```javascript
// Don't specify provider - uses user preference or fallback
const result = await client.transcribe(audioData, 'en');

if (result.usedFallback) {
  console.log(`Used fallback: ${result.provider}`);
}
```

## Common Issues & Solutions

### Issue: "API key not provided"
**Solution:** For OpenAI provider, store your API key first:
```javascript
await fetch('/api/apikey/store', {
  method: 'POST',
  headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
  body: JSON.stringify({ provider: 'OpenAIWhisper', apiKey: 'sk-...' })
});
```

### Issue: "All transcription providers failed"
**Solutions:**
1. Check Groq API key is valid in appsettings.json
2. For OpenAI: Store API key via `/api/apikey/store`
3. Check Whisper.cpp is running at `http://localhost:8080`
4. Check internet connection

### Issue: "Audio file too large"
**Solution:** Maximum is 25MB. Compress audio or record shorter clips.

### Issue: CORS errors in extension
**Solution:** Add extension origin to CORS settings:
```json
{
  "CorsSettings": {
    "AllowedOrigins": ["chrome-extension://YOUR_EXTENSION_ID"]
  }
}
```

## Debug Tips

### Enable Debug Logging
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

### Check Available Providers
```bash
curl -H "Authorization: Bearer YOUR_TOKEN" \
  https://localhost:5001/api/transcribe/providers
```

### View Transaction Logs
```bash
# SQLite database location
ls *.db

# View logs
sqlite3 audioassistant.dev.db "SELECT * FROM TransactionLogs ORDER BY CreatedAt DESC LIMIT 10"
```

## Next Steps

### For Backend:
1. Run integration tests: `dotnet test`
2. Test with real audio files
3. Configure production settings
4. Set up monitoring for transcription failures

### For Extension:
1. Implement full UI (see EXTENSION_INTEGRATION.md)
2. Add transcript history
3. Add provider selection UI
4. Add language selection dropdown
5. Implement error handling

## Support

- **Documentation:** See `EXTENSION_INTEGRATION.md` for full guide
- **API Reference:** Swagger UI at `/swagger`
- **Tests:** See `Tests/` directory
- **Examples:** See `TranscriptionApi.http`

## Provider Comparison

| Provider | Type | Cost | Speed | Accuracy | Setup |
|-----------|------|-------|--------|--------|
| GroqWhisper | Cloud | Fast | High | Server API key |
| WhisperCpp | Local | Varies | Medium- High | Install whisper.cpp |
| OpenAIWhisper | Cloud | Fast | Very High | User API key |

## Quick Reference

### Endpoints
- `POST /api/transcribe` - Transcribe audio
- `GET /api/transcribe/providers` - List providers
- `POST /api/transcribe/preferences/provider` - Set preference

### Language Codes
`en`, `es`, `fr`, `de`, `it`, `pt`, `nl`, `ru`, `ja`, `ko`, `zh`, `ar`, `hi`, `tr`, `pl`, `sv`, `fi`, `da`, `no`, `uk`, `cs`, `el`, `he`, `th`, `vi`

### Provider Names
- `GroqWhisper` - Primary
- `WhisperCpp` - Local fallback
- `OpenAIWhisper` - User-provided
- `ClaudeHaiku` - Not yet supported
