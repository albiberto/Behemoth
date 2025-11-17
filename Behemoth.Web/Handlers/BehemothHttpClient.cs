using System.Net.Http.Headers;
using System.Net.Http.Json;
using Behemoth.Contracts;
using Microsoft.AspNetCore.Components.Forms;

namespace Behemoth.Web.Handlers;

public class BehemothHttpClient(HttpClient http)
{
    public async Task<ProfileDto?> GetMyProfileAsync()
    {
        var response = await http.GetAsync("api/profiles/me");

        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.OK => await response.Content.ReadFromJsonAsync<ProfileDto>(),
            System.Net.HttpStatusCode.NotFound => new ProfileDto { Username = string.Empty, Bio = string.Empty },
            _ => null
        };
    }

    public async Task<UploadImageResponse?> UploadProfileImageAsync(IBrowserFile file, long maxFileSize)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream(maxFileSize))
        {
            Headers =
            {
                ContentType = new MediaTypeHeaderValue(file.ContentType),
            }
        };
        content.Add(fileContent, "file", file.Name);
        
        var response = await http.PostAsync("api/profiles/image", content);
        return response.IsSuccessStatusCode 
            ? await response.Content.ReadFromJsonAsync<UploadImageResponse>() 
            : null;
    }

    public async Task<bool> SaveProfileAsync(UpdateProfileRequest request)
    {
        var response = await http.PostAsJsonAsync("api/profiles", request);
        
        return response.IsSuccessStatusCode;
    }
}