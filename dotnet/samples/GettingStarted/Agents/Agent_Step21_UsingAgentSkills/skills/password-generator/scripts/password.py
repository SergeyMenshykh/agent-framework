#!/usr/bin/env python3
"""Generate a random password with configurable length and character sets."""

import secrets
import string

LOWERCASE = string.ascii_lowercase
UPPERCASE = string.ascii_uppercase
DIGITS = string.digits
SYMBOLS = "!@#$%^&*()-_=+[]{}"

# Configuration — adjust these values before running
length = 16
use_upper = True
use_digits = True
use_symbols = True

charset = LOWERCASE
required = [secrets.choice(LOWERCASE)]

if use_upper:
    charset += UPPERCASE
    required.append(secrets.choice(UPPERCASE))
if use_digits:
    charset += DIGITS
    required.append(secrets.choice(DIGITS))
if use_symbols:
    charset += SYMBOLS
    required.append(secrets.choice(SYMBOLS))

remaining = [secrets.choice(charset) for _ in range(length - len(required))]
password_chars = required + remaining
secrets.SystemRandom().shuffle(password_chars)
print("".join(password_chars))
