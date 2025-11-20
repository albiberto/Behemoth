namespace Behemoth.Contracts;

public abstract record Contract
{
    public abstract record Profile
    {
        public record Full(string Username, string? Bio = null, string? AvatarUrl = null) : Profile;

        public record Anagraphy(string Username = "", string? Bio = null) : Profile;

        public record Avatar(string? Url = null) : Profile;
    }
}