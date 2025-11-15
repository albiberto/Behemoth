using System.Net;
using Azure.Storage.Blobs;
using Behemoth.Domain;
using Behemoth.Infrastructure;
using Behemoth.Functions.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Behemoth.Functions.Functions;

public class ProfileFunction(ILogger<ProfileFunction> logger, BehemothContext context, BlobServiceClient containerClient)
{
    private readonly BlobContainerClient containerClient = containerClient.GetBlobContainerClient("avatars");

    [Function("UploadProfileImage")]
    public async Task<HttpResponseData> UploadProfileImage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "profiles/image")] HttpRequestData req)
    {
        logger.LogInformation("C# HTTP trigger function processed a request to upload a profile image.");

        try
        {
            var userId = req.GetUserId();

            var file = req.Body;

            var blobName = $"avatars/{userId}?t={DateTimeOffset.UtcNow.Ticks}";
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.UploadAsync(file, overwrite: true);
            var newAvatarUrl = blobClient.Uri.ToString();

            var profile = await context.Profiles.FindAsync(userId);
            if (profile is null) return req.CreateResponse(HttpStatusCode.NotFound);

            var updatedProfile = profile with { AvatarUrl = newAvatarUrl };
            context.Profiles.Update(updatedProfile);
            await context.SaveChangesAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { url = newAvatarUrl });
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading profile image.");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

[Function("GetMyProfile")]
public async Task<HttpResponseData> GetMyProfile(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "profiles/me")] HttpRequestData req)
{
    logger.LogInformation("Request to get current user's profile.");

    try
    {
        var userId = req.GetUserId();
        var profile = await context.Profiles.FindAsync(userId);

        if (profile == null)
        {
            logger.LogInformation("No profile found for user {UserId}. Creating a new one.", userId);
            profile = new Profile(userId);
            context.Profiles.Add(profile);
            await context.SaveChangesAsync();
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(profile);
        return response;
    }
    catch (Exception ex)
    {
         logger.LogError(ex, "Error getting or creating profile.");
        return req.CreateResponse(HttpStatusCode.InternalServerError);
    }
}

    [Function("CreateOrUpdateMyProfile")]
    public async Task<HttpResponseData> CreateOrUpdateMyProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "profiles")] HttpRequestData req)
    {
        logger.LogInformation("Request to create or update a profile.");

        try
        {
            var userId = req.GetUserId();

            var request = await req.ReadFromJsonAsync<UpdateProfileRequest>();
            if (request == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var existingProfile = await context.Profiles.FindAsync(userId);
            Profile profileToSave;

            if (existingProfile == null)
            {
                logger.LogInformation("Creating new profile for user {UserId}", userId);
                profileToSave = new Profile(
                    Id: userId,
                    Username: request.Username,
                    Bio: request.Bio,
                    AvatarUrl: null
                );
                context.Profiles.Add(profileToSave);
            }
            else
            {
                logger.LogInformation("Updating profile for user {UserId}", userId);
                profileToSave = existingProfile with
                {
                    Username = request.Username,
                    Bio = request.Bio
                };
                context.Profiles.Update(profileToSave);
            }

            await context.SaveChangesAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(profileToSave);
            return response;
        }
        catch (Exception ex)
        {
             logger.LogError(ex, "Error creating/updating profile.");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }
}