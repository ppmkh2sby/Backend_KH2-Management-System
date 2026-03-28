namespace KH2.ManagementSystem.Domain.Kafarahs;

public sealed record KafarahDefinition(
    string Code,
    string Label,
    string Description,
    int DefaultAmount);
