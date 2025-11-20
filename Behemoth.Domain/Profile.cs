namespace Behemoth.Domain;

public record Profile(string Id, string Username = "", string? Bio = null, string? AvatarUrl = null)
{
    public void Update(string username, string? bio)
    {
        Username = username;
        Bio = bio;
    }

    public string Username { get; private set; } = Username;
    public string? Bio { get; private set; } = Bio;
};