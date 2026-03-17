// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.Agents.AI;

namespace Agent_Step04_ClassBasedSkills;

/// <summary>
/// A unit-converter skill defined as a reusable C# class.
/// </summary>
/// <remarks>
/// Class-based skills bundle all components (name, description, body, resources, scripts)
/// into a single class that can be reused across agents and shared as a library.
/// </remarks>
public sealed class UnitConverterSkill : AgentClassSkill
{
    /// <inheritdoc/>
    public override string Name => "unit-converter";

    /// <inheritdoc/>
    public override string Description =>
        "Convert between common units using a multiplication factor. Use when asked to convert miles, kilometers, pounds, or kilograms.";

    /// <inheritdoc/>
    public override string Instructions => """
        Use this skill when the user asks to convert between units.

        1. Review the conversion-tables resource to find the factor for the requested conversion.
        2. Use the convert script, passing the value and factor from the table.
        3. Present the result clearly with both units.
        """;

    /// <inheritdoc/>
    public override IReadOnlyList<AgentSkillResource>? Resources { get; } =
    [
        new AgentCodeSkillResource(
            "conversion-tables",
            """
            # Conversion Tables

            Formula: **result = value × factor**

            | From        | To          | Factor   |
            |-------------|-------------|----------|
            | miles       | kilometers  | 1.60934  |
            | kilometers  | miles       | 0.621371 |
            | pounds      | kilograms   | 0.453592 |
            | kilograms   | pounds      | 2.20462  |
            """),
    ];

    /// <inheritdoc/>
    public override IReadOnlyList<AgentSkillScript>? Scripts { get; } =
    [
        new AgentCodeSkillScript(ConvertUnits, "convert"),
    ];

    private static string ConvertUnits(double value, double factor)
    {
        double result = Math.Round(value * factor, 4);
        return JsonSerializer.Serialize(new { value, factor, result });
    }
}
