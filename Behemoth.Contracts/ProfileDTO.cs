namespace Behemoth.Contracts;

public class ProfileDto
{
    public string Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}

public class UpdateProfileRequest
{
    public string Username { get; set; } = string.Empty;
    public string? Bio { get; set; }
}

public class UploadImageResponse
{
    public string? Url { get; set; }
}