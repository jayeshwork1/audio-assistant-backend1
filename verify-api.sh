#!/bin/bash

# Script to verify the Audio Assistant API is working correctly

BASE_URL="http://localhost:5096"
EMAIL="test@example.com"
PASSWORD="TestPassword123!"

echo "=== Audio Assistant API Verification ==="
echo ""

# Test 1: Health Check
echo "1. Testing Health Check..."
HEALTH_RESPONSE=$(curl -s "$BASE_URL/health")
echo "Response: $HEALTH_RESPONSE"
echo ""

# Test 2: User Registration
echo "2. Testing User Registration..."
REGISTER_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}")
echo "Response: $REGISTER_RESPONSE"
TOKEN=$(echo $REGISTER_RESPONSE | grep -o '"token":"[^"]*' | cut -d'"' -f4)
echo "Token: ${TOKEN:0:50}..."
echo ""

# Test 3: User Login
echo "3. Testing User Login..."
LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}")
echo "Response: $LOGIN_RESPONSE"
echo ""

# Test 4: Store API Key (Protected Route)
echo "4. Testing Store API Key (Protected)..."
API_KEY_RESPONSE=$(curl -s -X POST "$BASE_URL/api/apikey/store" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d "{\"provider\":\"groq\",\"apiKey\":\"test-api-key-12345\"}")
echo "Response: $API_KEY_RESPONSE"
echo ""

# Test 5: Get Providers
echo "5. Testing Get Providers..."
PROVIDERS_RESPONSE=$(curl -s -X GET "$BASE_URL/api/apikey/providers" \
  -H "Authorization: Bearer $TOKEN")
echo "Response: $PROVIDERS_RESPONSE"
echo ""

echo "=== Verification Complete ==="
