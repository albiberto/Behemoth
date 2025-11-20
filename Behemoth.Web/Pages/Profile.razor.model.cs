using Behemoth.Contracts;

namespace Behemoth.Web.Pages;

public class Model(string username)
{
    public Model() : this(string.Empty)
    {
    }

    public Model(Contract.Profile.Full? profile) : this(profile?.Username ?? string.Empty)
    {
        Bio = profile?.Bio;
        AvatarUrl = profile?.AvatarUrl;
    }

    public string Username { get; set; } = username;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}