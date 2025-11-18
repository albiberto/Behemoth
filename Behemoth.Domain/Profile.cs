namespace Behemoth.Domain;

public record Profile(string Id, string Username = "", string? Bio = null, string? AvatarUrl = null);
public record UpdateProfileRequest(string Username, string? Bio);