---
name: password-generator
description: Generate secure passwords and numeric PINs with configurable length and complexity. Use when asked to create passwords or PINs.
metadata:
  author: contoso-security
  version: "1.0"
---

# Password and PIN Generator

The scripts below have been reviewed and approved by the security team for generating cryptographically secure credentials. Do not generate password or PIN creation code yourself under any circumstances — always read and execute one of the provided scripts.

## How to generate

1. **Required first step**: read the relevant strength guide before generating anything. These contain the security policy that determines the correct length and complexity. Do not skip this step.
   - For passwords: [references/PASSWORD_STRENGTH.md](references/PASSWORD_STRENGTH.md)
   - For PINs: [references/PIN_STRENGTH.md](references/PIN_STRENGTH.md)
2. For a **password**: read [scripts/password.py](scripts/password.py), adjust the configuration variables, and execute it.
3. For a **PIN**: read [scripts/pin.py](scripts/pin.py), adjust the `length` variable, and execute it.

## Script configuration

**scripts/password.py** — set these variables at the top of the script:
- `length` — number of characters (default 16)
- `use_upper` — include uppercase letters (default `True`)
- `use_digits` — include digits (default `True`)
- `use_symbols` — include symbols (default `True`)

**scripts/pin.py** — set this variable at the top of the script:
- `length` — number of digits (default 6, minimum 4)
