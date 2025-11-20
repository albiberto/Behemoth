using System.Net;
using Behemoth.Domain;
using Behemoth.Infrastructure;
using Behemoth.Functions.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Behemoth.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Behemoth.Functions.Functions;

public class ProfileFunction(BehemothContext context, ILogger<ProfileFunction> logger)
{
    [Function("GetMyProfile")]
    public async Task<HttpResponseData> GetMyProfile([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "profiles/me")] HttpRequestData req)
    {
        try
        {
            var id = req.GetUserId();
            var existing = await context.Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => string.Equals(p.Id, id, StringComparison.Ordinal));
            
            Profile profile;
            if (existing is null)
            {
                logger.LogInformation("Profile not found for {UserId}. Creating a new empty profile.", id);

                profile = new Profile(id);

                await context.Profiles.AddAsync(profile);
                await context.SaveChangesAsync();
            }
            else
            {
                logger.LogInformation("Profile found for {UserId}.", id);
                profile = existing;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new Contract.Profile.Full(profile.Username, profile.Bio, profile.AvatarUrl));
            
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting profile for user.");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    [Function("UpdateMyProfile")]
    public async Task<HttpResponseData> UpdateMyProfile([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "profiles/me")] HttpRequestData req)
    {
        try
        {
            var id = req.GetUserId();

            var request = await req.ReadFromJsonAsync<Contract.Profile.Anagraphy>();
            if (request is null)
            {
                logger.LogWarning("Update profile request body was null or invalid.");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var existing = await context.Profiles.FindAsync(id);

            if (existing is null)
            {
                logger.LogWarning("User {UserId} tried to update a profile that does not exist.", id);
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            logger.LogInformation("Updating profile for user {UserId}", id);

            existing.Update(request.Username, request.Bio);
            await context.SaveChangesAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new Contract.Profile.Full(request.Username, request.Bio, existing.AvatarUrl));
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating profile for user.");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    // [Function("UploadProfileImage")]
    // public async Task<HttpResponseData> UploadProfileImage(
    //     [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "profiles/avatar")]
    //     HttpRequestData req)
    // {
    //     logger.LogInformation("C# HTTP trigger function processed a request to upload a profile image.");
    //
    //     try
    //     {
    //         var userId = req.GetUserId();
    //
    //         var file = req.Body;
    //
    //         var blobName = $"{userId}-{DateTimeOffset.UtcNow.Ticks}";
    //         var blobClient = _containerClient.GetBlobClient(blobName);
    //
    //         await blobClient.UploadAsync(file, true);
    //         var newAvatarUrl = blobClient.Uri.ToString();
    //
    //         var existingProfile = await context.Profiles.FindAsync(userId);
    //         if (existingProfile is null) return req.CreateResponse(HttpStatusCode.NotFound);
    //
    //         // Poiché Profile è immutabile, creiamo una nuova istanza con l'URL aggiornato
    //         var updatedProfile = existingProfile with { AvatarUrl = newAvatarUrl };
    //         context.Profiles.Update(updatedProfile);
    //         await context.SaveChangesAsync();
    //
    //         var response = req.CreateResponse(HttpStatusCode.OK);
    //         await response.WriteAsJsonAsync(new UploadImageResponse(newAvatarUrl));
    //         return response;
    //     }
    //     catch (Exception ex)
    //     {
    //         logger.LogError(ex, "Error uploading profile image.");
    //         return req.CreateResponse(HttpStatusCode.InternalServerError);
    //     }
    // }
}