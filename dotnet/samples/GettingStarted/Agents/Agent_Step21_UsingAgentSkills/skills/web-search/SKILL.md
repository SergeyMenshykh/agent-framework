---
name: web-search
description: Search the web for current information using a mock web search API. Use this when you need to find recent news, articles, or general information.
compatibility: Requires internet connection
---

# Web Search Skill

This skill provides the ability to search the web for current information.

## When to Use This Skill

Use this skill when you need to:
- Find current news and articles
- Look up general information
- Research topics that require up-to-date information
- Get facts from authoritative sources

## Additional Documentation

For detailed API reference and technical documentation, see [references/REFERENCE.md](references/REFERENCE.md).

## How to Use

1. Load this skill using `load_skill("web-search")` to see full instructions
2. For additional details about the API, use `read_skill_resource("web-search", "references/REFERENCE.md")`
3. The skill provides context about web search capabilities

## Search Guidelines

When searching:
- Use specific, targeted queries for best results
- Consider multiple search terms if the first doesn't work
- Combine keywords with operators when needed
- Verify information from multiple sources when possible

## Output Format

Search results typically include:
- **Title**: The title of the web page or article
- **Snippet**: A brief excerpt showing relevant content
- **URL**: Link to the full content
- **Date**: When the content was published (if available)

## Best Practices

- Start with broad searches and narrow down as needed
- Use quotation marks for exact phrase matching
- Combine with other skills for comprehensive research
- Always cite sources in your responses
