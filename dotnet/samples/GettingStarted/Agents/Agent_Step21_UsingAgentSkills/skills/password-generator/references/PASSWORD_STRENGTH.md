# Password Strength Guide

## Requirements

| Use Case | Min Length | Character Types | Example |
|----------|-----------|-----------------|---------|
| Standard accounts | 12 | Upper + lower + digits | `Tg7kLm9xPq2w` |
| Admin / root access | 20 | All four types | `K#9mTv!2qLx&8Wp$Rn3z` |

Password characters: lowercase `a-z`, uppercase `A-Z`, digits `0-9`, symbols `!@#$%^&*()-_=+[]{}`.

## Strength Tiers

- **Weak**: < 8 characters or single character type. Reject.
- **Fair**: 8–11 characters with 2+ types. Low-risk accounts only.
- **Strong**: 12–19 characters with 3+ types. Recommended default.
- **Very Strong**: 20+ characters with all four types. Privileged access.

## Common Pitfalls

- Avoid dictionary words, sequential characters (`abc`, `123`), and personal information.
- Do not reuse passwords across services.
- Rotate service account passwords every 90 days.
