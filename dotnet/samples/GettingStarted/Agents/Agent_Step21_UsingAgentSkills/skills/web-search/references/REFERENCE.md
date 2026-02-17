# Web Search API Reference

## Overview

This reference document provides additional technical details about the web search capabilities.

## API Endpoints

The web search functionality simulates access to major search engines and provides:

- General web search
- News search
- Academic paper search
- Image search (metadata only)

## Search Parameters

- **query** (required): The search query string
- **max_results** (optional): Maximum number of results to return (default: 10)
- **date_range** (optional): Filter by date (e.g., "past_week", "past_month", "past_year")
- **language** (optional): Preferred language for results (default: "en")

## Rate Limits

- Standard: 100 queries per hour
- Premium: 1000 queries per hour

## Response Format

```json
{
  "results": [
    {
      "title": "Example Title",
      "snippet": "Example snippet...",
      "url": "https://example.com",
      "date": "2026-02-17"
    }
  ],
  "total_results": 42,
  "search_time": "0.45s"
}
```

## Error Codes

- **400**: Invalid query format
- **429**: Rate limit exceeded
- **503**: Service temporarily unavailable

## Tips for Better Results

1. Use specific keywords rather than full sentences
2. Combine terms with AND, OR, NOT operators
3. Use site: operator to search specific domains
4. Use filetype: operator to find specific document types
