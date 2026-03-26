// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Agent_Step07_ClassBasedSkillsWithDI;

/// <summary>
/// A unit-converter skill defined as a reusable C# class that uses Dependency Injection.
/// </summary>
/// <remarks>
/// This skill resolves <see cref="ConversionRateService"/> from the DI container
/// in both its resource and script functions. This enables clean separation of
/// concerns and testability while retaining the reusable class-based skill pattern.
/// </remarks>
public sealed class UnitConverterSkill : AgentClassSkill
{
    /// <inheritdoc/>
    public override AgentSkillFrontmatter Frontmatter { get; } = new(
        "unit-converter",
        "Convert between common units using a multiplication factor. Use when asked to convert miles, kilometers, pounds, or kilograms.");

    /// <inheritdoc/>
    public override string Instructions => """
        Use this skill when the user asks to convert between units.

        1. Review the conversion-table resource to find the factor for the requested conversion.
        2. Use the convert script, passing the value and factor from the table.
        3. Present the result clearly with both units.
        """;

    /// <inheritdoc/>
    public override IReadOnlyList<AgentSkillResource>? Resources { get; } =
    [
        // Dynamic resource with DI: resolves ConversionRateService to build conversion table
        new AgentInlineSkillResource((IServiceProvider serviceProvider) =>
        {
            var rateService = serviceProvider.GetRequiredService<ConversionRateService>();
            return rateService.GetConversionTable();
        }, "conversion-table"),
    ];

    /// <inheritdoc/>
    public override IReadOnlyList<AgentSkillScript>? Scripts { get; } =
    [
        // Script with DI: resolves ConversionRateService to perform the conversion
        new AgentInlineSkillScript((double value, double factor, IServiceProvider serviceProvider) =>
        {
            var rateService = serviceProvider.GetRequiredService<ConversionRateService>();
            return rateService.Convert(value, factor);
        }, "convert"),
    ];
}

/// <summary>
/// Provides conversion rates between units.
/// In a real application this could call an external API, read from a database,
/// or apply time-varying exchange rates.
/// </summary>
internal sealed class ConversionRateService
{
    /// <summary>
    /// Returns a static markdown table of all supported conversions with factors.
    /// </summary>
    public string GetConversionTable() =>
        """
        # Conversion Tables

        Formula: **result = value × factor**

        | From        | To          | Factor   |
        |-------------|-------------|----------|
        | miles       | kilometers  | 1.60934  |
        | kilometers  | miles       | 0.621371 |
        | pounds      | kilograms   | 0.453592 |
        | kilograms   | pounds      | 2.20462  |
        """;

    /// <summary>
    /// Converts a value by the given factor and returns a JSON result.
    /// </summary>
    public string Convert(double value, double factor)
    {
        double result = Math.Round(value * factor, 4);
        return JsonSerializer.Serialize(new { value, factor, result });
    }
}
