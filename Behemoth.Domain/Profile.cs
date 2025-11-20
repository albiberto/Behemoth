namespace Behemoth.Domain;

public record Profile(string Id, string Username = "", string? Bio = null, string? AvatarUrl = null)
{
    public void Update(string username, string? bio)
    {
        Username = username;
        Bio = bio;
    }
    
    public void UpdateAvatar(string? avatarUrl) => AvatarUrl = avatarUrl;

    public string Username { get; private set; } = Username;
    public string? Bio { get; private set; } = Bio;
    public string? AvatarUrl { get; private set; } = AvatarUrl;
};