using System.Net.Http.Headers;
using System.Net.Http.Json;
using Behemoth.Contracts;
using Microsoft.AspNetCore.Components.Forms;

namespace Behemoth.Web.Handlers;

public class BehemothHttpClient(HttpClient http)
{
    public async Task<Contract.Profile.Full?> GetMyProfileAsync()
    {
        var response = await http.GetAsync("api/profiles/me");

        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.OK => await response.Content.ReadFromJsonAsync<Contract.Profile.Full>(),
            System.Net.HttpStatusCode.NotFound => new Contract.Profile.Full(string.Empty),
            _ => null
        };
    }

    public async Task<bool> SaveProfileAsync(string username, string? bio)
    {
        var request = new Contract.Profile.Anagraphy(username, bio);

        var response = await http.PatchAsJsonAsync("api/profiles/me", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<Contract.Profile.Avatar?> UploadProfileImageAsync(IBrowserFile file, long maxFileSize)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream(maxFileSize))
        {
            Headers =
            {
                ContentType = new MediaTypeHeaderValue(file.ContentType)
            }
        };
        content.Add(fileContent, "file", file.Name);

        var response = await http.PostAsync("api/profiles/avatar", content);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<Contract.Profile.Avatar>()
            : null;
    }
}