using System.Net;
using Behemoth.Domain;
using Behemoth.Infrastructure;
using Behemoth.Functions.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Behemoth.Contracts;
using FluentValidation;
using UpdateProfileRequest = Behemoth.Contracts.UpdateProfileRequest;

namespace Behemoth.Functions.Functions;

public class ProfileFunction(BehemothContext context, ILogger<ProfileFunction> logger)
{
    [Function("CreateOrUpdateMyProfile")]
    public async Task<HttpResponseData> CreateOrUpdateMyProfile([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "profiles/me")] HttpRequestData req)
    {
        logger.LogInformation("Request to create or update a profile.");

        try
        {
            var userId = req.GetUserId();

            var request = await req.ReadFromJsonAsync<UpdateProfileRequest>();
            if (request is null) return req.CreateResponse(HttpStatusCode.BadRequest);

            var existingProfile = await context.Profiles.FindAsync(userId);
            Profile updatedProfile;

            if (existingProfile is null)
            {
                logger.LogInformation("Creating new profile for user {UserId}", userId);
                
                updatedProfile = new Profile(userId, request.Username, request.Bio, null);
                await context.Profiles.AddAsync(updatedProfile);
            }
            else
            {
                logger.LogInformation("Updating profile for user {UserId}", userId);
                
                updatedProfile = existingProfile with
                {
                    Username = request.Username,
                    Bio = request.Bio
                };
                context.Profiles.Update(updatedProfile);
            }

            await context.SaveChangesAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            var profileDto = new ProfileDto
            {
                Id = updatedProfile.Id,
                Username = updatedProfile.Username,
                Bio = updatedProfile.Bio,
                AvatarUrl = updatedProfile.AvatarUrl
            };
            await response.WriteAsJsonAsync(profileDto);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating/updating profile.");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    //
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