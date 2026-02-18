"""Generate a numeric PIN with configurable length."""

import secrets

DIGITS = "0123456789"

# Configuration — change this value to set the PIN length (minimum 4)
length = 6

print("".join(secrets.choice(DIGITS) for _ in range(length)))
