namespace Behemoth.Contracts;

public record ProfileDto
{
    public Guid? Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }

    public ProfileDto() { }
}

public record UpdateProfileRequest(string Username = "", string? Bio = null);

public record UploadImageResponse(string? Url = null);