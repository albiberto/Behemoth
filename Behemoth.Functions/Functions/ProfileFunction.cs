using System.Net;
using Behemoth.Domain;
using Behemoth.Infrastructure;
using Behemoth.Functions.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Behemoth.Contracts;
using FluentValidation;
// Assicurati che il tuo UpdateProfileRequest sia in Behemoth.Contracts
using UpdateProfileRequest = Behemoth.Contracts.UpdateProfileRequest;

namespace Behemoth.Functions.Functions;

public class ProfileFunction(BehemothContext context, ILogger<ProfileFunction> logger)
{
    /// <summary>
    /// FUNZIONE 1: Ottiene il profilo dell'utente corrente.
    /// Se il profilo non esiste, ne crea uno nuovo e vuoto.
    /// </summary>
    [Function("GetMyProfile")]
    public async Task<HttpResponseData> GetMyProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "profiles/me")]
        HttpRequestData req)
    {
        logger.LogInformation("Request to get 'me' profile.");

        try
        {
            var userId = req.GetUserId();

            var existingProfile = await context.Profiles.FindAsync(userId);
            Profile profileToReturn;

            if (existingProfile is null)
            {
                logger.LogInformation("Profile not found for {UserId}. Creating a new empty profile.", userId);

                profileToReturn = new Profile(userId);

                await context.Profiles.AddAsync(profileToReturn);
                await context.SaveChangesAsync();
            }
            else
            {
                logger.LogInformation("Profile found for {UserId}.", userId);
                profileToReturn = existingProfile;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            var profileDto = new ProfileDto
            {
                Id = profileToReturn.Id,
                Username = profileToReturn.Username,
                Bio = profileToReturn.Bio,
                AvatarUrl = profileToReturn.AvatarUrl
            };
            await response.WriteAsJsonAsync(profileDto);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting profile for user.");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// FUNZIONE 2: Aggiorna il profilo dell'utente corrente (Username e Bio).
    /// Questa funzione è solo un UPDATE (PUT).
    /// </summary>
    [Function("UpdateMyProfile")]
    public async Task<HttpResponseData> UpdateMyProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "profiles/me")]
        HttpRequestData req)
    {
        logger.LogInformation("Request to update 'me' profile.");

        try
        {
            var userId = req.GetUserId();

            var request = await req.ReadFromJsonAsync<UpdateProfileRequest>();
            if (request is null)
            {
                logger.LogWarning("Update profile request body was null or invalid.");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var existingProfile = await context.Profiles.FindAsync(userId);

            if (existingProfile is null)
            {
                // Non dovrebbe succedere se il GET viene chiamato prima, 
                // ma è una buona protezione.
                logger.LogWarning("User {UserId} tried to update a profile that does not exist.", userId);
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            logger.LogInformation("Updating profile for user {UserId}", userId);

            // Applica l'aggiornamento
            var updatedProfile = existingProfile with
            {
                Username = request.Username,
                Bio = request.Bio
            };
            context.Profiles.Update(updatedProfile);

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
            logger.LogError(ex, "Error updating profile for user.");
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