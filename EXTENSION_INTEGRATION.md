# Audio Assistant Extension - STT Integration

This document describes how the browser extension should integrate with the Audio Assistant backend for Speech-to-Text (STT) functionality.

## Backend API Endpoints

### 1. Transcribe Audio
**Endpoint:** `POST /api/transcribe`

**Authentication:** Required (Bearer token in Authorization header)

**Request Body:**
```json
{
  "audioData": [base64 encoded byte array or raw bytes],
  "language": "en",
  "provider": "GroqWhisper",
  "streaming": false
}
```

**Parameters:**
- `audioData` (required): Audio data as byte array
- `language` (optional): Language code (default: "en")
  - Supported: "en", "es", "fr", "de", "it", "pt", "nl", "ru", "ja", "ko", "zh", "ar", etc.
- `provider` (optional): Specific provider to use (overrides user preference)
  - Options: "GroqWhisper", "WhisperCpp", "OpenAIWhisper"
- `streaming` (optional): Whether to use streaming (default: false)

**Response:**
```json
{
  "id": "guid",
  "text": "Transcribed text",
  "language": "en",
  "confidence": 0.95,
  "duration": "00:00:01.234",
  "provider": "GroqWhisper",
  "tokens": 100,
  "timestamp": "2024-01-15T10:30:00Z",
  "usedFallback": false
}
```

**Error Response:**
```json
{
  "error": "Error message describing the issue"
}
```

### 2. Get Available Providers
**Endpoint:** `GET /api/transcribe/providers`

**Authentication:** Required

**Response:**
```json
["GroqWhisper", "WhisperCpp", "OpenAIWhisper"]
```

### 3. Set Preferred Provider
**Endpoint:** `POST /api/transcribe/preferences/provider`

**Authentication:** Required

**Request Body:**
```json
{
  "provider": "GroqWhisper"
}
```

**Response:**
```json
{
  "success": true
}
```

## Extension Implementation Guide

### 1. TranscriptionClient Class

Create a client class to handle transcription API calls:

```javascript
class TranscriptionClient {
  constructor(baseUrl, authToken) {
    this.baseUrl = baseUrl;
    this.authToken = authToken;
  }

  async transcribe(audioData, language = 'en', provider = null) {
    try {
      const response = await fetch(`${this.baseUrl}/api/transcribe`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.authToken}`
        },
        body: JSON.stringify({
          audioData: Array.from(audioData),
          language,
          provider,
          streaming: false
        })
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || 'Transcription failed');
      }

      return await response.json();
    } catch (error) {
      console.error('Transcription error:', error);
      throw error;
    }
  }

  async getAvailableProviders() {
    const response = await fetch(`${this.baseUrl}/api/transcribe/providers`, {
      headers: {
        'Authorization': `Bearer ${this.authToken}`
      }
    });
    return await response.json();
  }

  async setPreferredProvider(provider) {
    const response = await fetch(`${this.baseUrl}/api/transcribe/preferences/provider`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.authToken}`
      },
      body: JSON.stringify({ provider })
    });
    return await response.json();
  }
}
```

### 2. Real-time Transcript Display

**UI Component:**
```html
<div id="transcript-popup" class="transcript-popup" style="display: none;">
  <div class="transcript-header">
    <span class="provider-badge" id="provider-badge">GroqWhisper</span>
    <button class="close-btn" onclick="closeTranscript()">&times;</button>
  </div>
  <div class="transcript-content" id="transcript-content">
    <div class="loading-indicator">Transcribing...</div>
  </div>
  <div class="transcript-footer">
    <span class="confidence">Confidence: <span id="confidence-score">-</span></span>
    <span class="duration">Duration: <span id="duration">-</span></span>
  </div>
</div>
```

**JavaScript:**
```javascript
async function transcribeAudio(audioBlob, language) {
  showTranscriptPopup('Transcribing...');
  
  try {
    const audioData = await blobToArrayBuffer(audioBlob);
    const client = new TranscriptionClient(API_BASE_URL, authToken);
    
    const result = await client.transcribe(audioData, language);
    
    updateTranscriptDisplay(result);
    
    if (result.usedFallback) {
      showFallbackNotification(`Fallback to ${result.provider}`);
    }
  } catch (error) {
    showTranscriptError(error.message);
  }
}

function updateTranscriptDisplay(result) {
  document.getElementById('provider-badge').textContent = result.provider;
  document.getElementById('transcript-content').textContent = result.text;
  document.getElementById('confidence-score').textContent = 
    (result.confidence * 100).toFixed(1) + '%';
  document.getElementById('duration').textContent = 
    formatDuration(result.duration);
}

function formatDuration(timespan) {
  // Parse "00:00:01.234" format and display as human-readable
  const match = timespan.match(/(\d+):(\d+):([\d.]+)/);
  if (match) {
    const seconds = parseFloat(match[3]);
    return `${seconds.toFixed(2)}s`;
  }
  return timespan;
}

function showFallbackNotification(message) {
  const notification = document.createElement('div');
  notification.className = 'fallback-notification';
  notification.textContent = message;
  document.body.appendChild(notification);
  
  setTimeout(() => {
    notification.remove();
  }, 5000);
}
```

### 3. Transcript History Sidebar

**UI Component:**
```html
<div id="transcript-sidebar" class="sidebar">
  <div class="sidebar-header">
    <h3>Transcript History</h3>
    <button class="clear-btn" onclick="clearHistory()">Clear</button>
  </div>
  <div id="transcript-list" class="transcript-list">
    <!-- Transcript items will be added here -->
  </div>
</div>
```

**JavaScript:**
```javascript
class TranscriptHistory {
  constructor() {
    this.transcripts = [];
    this.loadFromStorage();
  }

  addTranscript(transcript) {
    const item = {
      id: Date.now(),
      text: transcript.text,
      provider: transcript.provider,
      confidence: transcript.confidence,
      timestamp: new Date().toISOString()
    };
    
    this.transcripts.unshift(item);
    this.saveToStorage();
    this.render();
  }

  loadFromStorage() {
    const stored = localStorage.getItem('transcriptHistory');
    if (stored) {
      this.transcripts = JSON.parse(stored);
    }
  }

  saveToStorage() {
    localStorage.setItem('transcriptHistory', 
      JSON.stringify(this.transcripts.slice(0, 50))); // Keep last 50
  }

  render() {
    const list = document.getElementById('transcript-list');
    list.innerHTML = this.transcripts.map(t => `
      <div class="transcript-item" data-id="${t.id}">
        <div class="item-header">
          <span class="provider-tag">${t.provider}</span>
          <span class="timestamp">${formatTime(t.timestamp)}</span>
        </div>
        <div class="item-text">${escapeHtml(t.text)}</div>
        <div class="item-actions">
          <button onclick="copyTranscript('${t.id}')">Copy</button>
          <button onclick="deleteTranscript('${t.id}')">Delete</button>
        </div>
      </div>
    `).join('');
  }
}

function formatTime(isoString) {
  const date = new Date(isoString);
  return date.toLocaleTimeString();
}

function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}
```

### 4. Provider Indicator

**CSS Styling:**
```css
.provider-badge {
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
}

.provider-badge.groq {
  background-color: #f97316;
  color: white;
}

.provider-badge.whispercpp {
  background-color: #10b981;
  color: white;
}

.provider-badge.openai {
  background-color: #3b82f6;
  color: white;
}

.fallback-notification {
  position: fixed;
  bottom: 20px;
  right: 20px;
  background-color: #f59e0b;
  color: white;
  padding: 12px 16px;
  border-radius: 8px;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  animation: slideIn 0.3s ease-out;
}

@keyframes slideIn {
  from {
    transform: translateX(100%);
    opacity: 0;
  }
  to {
    transform: translateX(0);
    opacity: 1;
  }
}
```

### 5. Language Selection

**UI Component:**
```html
<div class="settings-section">
  <label for="language-select">Transcription Language:</label>
  <select id="language-select" onchange="saveLanguagePreference(this.value)">
    <option value="en">English</option>
    <option value="es">Spanish</option>
    <option value="fr">French</option>
    <option value="de">German</option>
    <option value="it">Italian</option>
    <option value="pt">Portuguese</option>
    <option value="nl">Dutch</option>
    <option value="ru">Russian</option>
    <option value="ja">Japanese</option>
    <option value="ko">Korean</option>
    <option value="zh">Chinese</option>
    <option value="ar">Arabic</option>
  </select>
</div>
```

**JavaScript:**
```javascript
function saveLanguagePreference(language) {
  localStorage.setItem('transcriptionLanguage', language);
}

function loadLanguagePreference() {
  const saved = localStorage.getItem('transcriptionLanguage') || 'en';
  document.getElementById('language-select').value = saved;
  return saved;
}

function getSelectedLanguage() {
  return document.getElementById('language-select').value || 'en';
}
```

### 6. Error Handling

```javascript
class TranscriptionErrorHandler {
  static handle(error) {
    if (error.message.includes('API key not provided')) {
      this.showApiKeyError();
    } else if (error.message.includes('Audio file too large')) {
      this.showFileSizeError();
    } else if (error.message.includes('All transcription providers failed')) {
      this.showProviderError();
    } else {
      this.showGenericError(error.message);
    }
  }

  static showApiKeyError() {
    alert('API key required for this provider. Please configure your API keys in settings.');
  }

  static showFileSizeError() {
    alert('Audio file is too large. Maximum size is 25MB.');
  }

  static showProviderError() {
    alert('Unable to transcribe audio. Please check your internet connection and try again.');
  }

  static showGenericError(message) {
    console.error('Transcription error:', message);
    alert(`Transcription failed: ${message}`);
  }
}

// Usage
try {
  await transcribeAudio(audioBlob, language);
} catch (error) {
  TranscriptionErrorHandler.handle(error);
}
```

### 7. Audio Recording & Encoding

```javascript
async function recordAudio(maxDuration = 30000) { // 30 seconds default
  const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
  const mediaRecorder = new MediaRecorder(stream);
  const chunks = [];

  mediaRecorder.ondataavailable = (event) => {
    if (event.data.size > 0) {
      chunks.push(event.data);
    }
  };

  return new Promise((resolve, reject) => {
    const timeout = setTimeout(() => {
      mediaRecorder.stop();
    }, maxDuration);

    mediaRecorder.onstop = () => {
      clearTimeout(timeout);
      const blob = new Blob(chunks, { type: 'audio/webm' });
      stream.getTracks().forEach(track => track.stop());
      resolve(blob);
    };

    mediaRecorder.onerror = (error) => {
      clearTimeout(timeout);
      reject(error);
    };

    mediaRecorder.start();
  });
}

async function convertToMp3(webmBlob) {
  // Note: Browsers typically output WebM format
  // The backend handles various audio formats
  // If conversion is needed, use a library like lamejs
  
  // For now, send the blob directly - backend can handle WebM
  return webmBlob;
}

async function blobToArrayBuffer(blob) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onloadend = () => resolve(reader.result);
    reader.onerror = reject;
    reader.readAsArrayBuffer(blob);
  });
}
```

## Integration Flow

1. **User initiates recording:**
   - Click microphone button in extension popup
   - Browser prompts for microphone permission
   - Audio recording begins

2. **Recording complete:**
   - User stops recording or timeout reached
   - Audio blob is created
   - Language preference is retrieved from settings

3. **Transcription request:**
   - Audio blob converted to ArrayBuffer
   - API request sent to `/api/transcribe`
   - Loading indicator shown in UI

4. **Processing:**
   - Backend processes audio with preferred provider
   - Fallback chain executes if primary fails
   - Result is returned to extension

5. **Display result:**
   - Transcript text displayed in popup
   - Provider badge shows which provider was used
   - Fallback notification shown if applicable
   - Transcript added to history

6. **Error handling:**
   - User-friendly error messages displayed
   - Provider availability checked
   - Retry options provided

## Testing Checklist

- [ ] Test transcription with different audio lengths (5s, 15s, 30s)
- [ ] Test language selection for multiple languages
- [ ] Test provider switching (Groq → WhisperCpp → OpenAI)
- [ ] Test fallback notifications
- [ ] Test error handling (network errors, API failures)
- [ ] Test transcript history (save, load, delete)
- [ ] Test copy transcript to clipboard
- [ ] Test concurrent transcription requests
- [ ] Test with poor audio quality
- [ ] Test with different audio formats

## Browser Compatibility

- **Chrome/Edge:** Full support (MediaRecorder, ArrayBuffer)
- **Firefox:** Full support
- **Safari:** Full support (iOS 14.5+, macOS 11+)
- **Opera:** Full support

Minimum browser versions:
- Chrome 51+
- Firefox 30+
- Safari 14.1+
- Edge 79+
