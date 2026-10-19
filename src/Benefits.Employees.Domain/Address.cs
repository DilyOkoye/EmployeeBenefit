namespace Benefits.Employees.Domain;

/// <summary>A UK postal address. <see cref="Postcode"/> is stored in canonical form, e.g. <c>SW1A 1AA</c>.</summary>
public sealed record Address(string Line1, string? Line2, string Town, string? County, string Postcode);
