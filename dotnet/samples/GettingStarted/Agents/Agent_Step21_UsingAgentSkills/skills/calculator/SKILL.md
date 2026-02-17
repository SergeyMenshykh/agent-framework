---
name: calculator
description: Perform mathematical calculations and conversions. Supports basic arithmetic, advanced math functions, and unit conversions. Use when you need precise calculations or unit conversions.
compatibility: Requires PowerShell (Windows) or bash (Linux/Mac)
---

# Calculator Skill

This skill provides mathematical calculation capabilities including basic arithmetic, advanced functions, and unit conversions.

## When to Use This Skill

Use this skill when you need to:
- Perform precise calculations
- Evaluate mathematical expressions
- Convert between units (length, weight, temperature, etc.)
- Calculate percentages, ratios, or statistics

## Available Scripts

This skill includes the `calculate.ps1` script for performing calculations. Execute it using:

```
run_skill_script("calculator", "calculate.ps1", ["expression"])
```

## Available Operations

### Basic Arithmetic
- Addition (+)
- Subtraction (-)
- Multiplication (*)
- Division (/)
- Exponentiation (**)
- Modulo (%)

### Advanced Functions
- Square root (sqrt)
- Logarithms (log, ln)
- Trigonometric functions (sin, cos, tan)
- Rounding (round, ceil, floor)

### Unit Conversions
- Length: meters, feet, miles, kilometers
- Weight: grams, kilograms, pounds, ounces
- Temperature: Celsius, Fahrenheit, Kelvin
- Volume: liters, gallons, milliliters

## Examples

Calculate a mathematical expression:
```
run_skill_script("calculator", "calculate.ps1", ["(10 + 5) * 2"])
```

Convert units:
```
run_skill_script("calculator", "calculate.ps1", ["100 miles to km"])
```

Calculate percentage:
```
run_skill_script("calculator", "calculate.ps1", ["15% of 200"])
```

## Script: calculate.ps1

The `calculate` script accepts a mathematical expression as its first argument.

**Arguments:**
- `expression` (required): The mathematical expression to evaluate

**Returns:**
- The calculated result as a string

**Note:** This is a mock implementation that returns simulated results. In a real scenario, this would execute actual calculations.
