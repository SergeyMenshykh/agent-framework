// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Agents.AI;

namespace Agent_Step05_MixedSkills;

/// <summary>
/// A temperature-converter skill defined as a reusable C# class.
/// </summary>
public sealed class TemperatureConverterSkill : AgentClassSkill
{
    /// <inheritdoc/>
    public override string Name => "temperature-converter";

    /// <inheritdoc/>
    public override string Description =>
        "Convert between temperature scales (Fahrenheit, Celsius, Kelvin).";

    /// <inheritdoc/>
    public override string Body => """
        Use this skill when the user asks to convert temperatures.

        1. Review the conversion-formulas resource for the correct formula.
        2. Use the convert script, passing the value, source scale, and target scale.
        3. Present the result clearly with both temperature scales.
        """;

    /// <inheritdoc/>
    public override IReadOnlyList<AgentSkillResource>? Resources { get; } =
    [
        new AgentCodeSkillResource(
            "conversion-formulas",
            """
            # Temperature Conversion Formulas

            | From        | To          | Formula                   |
            |-------------|-------------|---------------------------|
            | Fahrenheit  | Celsius     | °C = (°F − 32) × 5/9     |
            | Celsius     | Fahrenheit  | °F = (°C × 9/5) + 32     |
            | Celsius     | Kelvin      | K = °C + 273.15           |
            | Kelvin      | Celsius     | °C = K − 273.15           |
            """),
    ];

    /// <inheritdoc/>
    public override IReadOnlyList<AgentSkillScript>? Scripts { get; } =
    [
        new AgentCodeSkillScript(ConvertTemperature),
    ];

    private static string ConvertTemperature(double value, string from, string to)
    {
        double result = (from.ToUpperInvariant(), to.ToUpperInvariant()) switch
        {
            ("FAHRENHEIT", "CELSIUS") => Math.Round((value - 32) * 5.0 / 9.0, 2),
            ("CELSIUS", "FAHRENHEIT") => Math.Round(value * 9.0 / 5.0 + 32, 2),
            ("CELSIUS", "KELVIN") => Math.Round(value + 273.15, 2),
            ("KELVIN", "CELSIUS") => Math.Round(value - 273.15, 2),
            _ => throw new ArgumentException($"Unsupported conversion: {from} → {to}")
        };

        return JsonSerializer.Serialize(new { value, from, to, result });
    }
}
