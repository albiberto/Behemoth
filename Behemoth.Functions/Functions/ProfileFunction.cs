using System.Net;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Behemoth.Domain;
using Behemoth.Infrastructure;
using Behemoth.Functions.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Behemoth.Contracts;
using Behemoth.Functions.Options;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Behemoth.Functions.Functions;

public class ProfileFunction(
    BehemothContext context,
    IDistributedCache cache,
    IValidator<Contract.Profile.Anagraphy> validator,
    IOptions<CacheOptions> options,
    BlobServiceClient blobs,
    ILogger<ProfileFunction> logger)
{
    [Function("GetMyProfile")]
    public async Task<HttpResponseData> GetMyProfile([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "profiles/me")] HttpRequestData req)
    {
        var id = req.GetUserId();
        var cacheKey = CacheOptions.ProfileKey(id);

        try
        {
            var cachedProfileJson = await cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedProfileJson))
            {
                logger.LogInformation("Profile found in Redis cache for {UserId}.", id);

                var cachedProfile = JsonSerializer.Deserialize<Contract.Profile.Full>(cachedProfileJson);

                var responseCached = req.CreateResponse(HttpStatusCode.OK);
                await responseCached.WriteAsJsonAsync(cachedProfile);

                return responseCached;
            }

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

            var contract = new Contract.Profile.Full(profile.Username, profile.Bio, profile.AvatarUrl);

            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(contract), options.Value.ProfileOptions);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(contract);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting profile for user.");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    [Function("UpdateMyProfile")]
    public async Task<HttpResponseData> UpdateMyProfile([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "profiles/me")] HttpRequestData req)
    {
        var id = req.GetUserId();
        var cacheKey = CacheOptions.ProfileKey(id);

        try
        {
            var request = await req.ReadFromJsonAsync<Contract.Profile.Anagraphy>();
            if (request is null)
            {
                logger.LogWarning("Update profile request body was null or invalid for user {UserId}.", id);
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                logger.LogWarning("Validation failed for profile update for user {UserId}.", id);

                var errorMessages = validationResult.Errors
                    .Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage })
                    .ToList();

                var responseValidation = req.CreateResponse(HttpStatusCode.BadRequest);
                await responseValidation.WriteAsJsonAsync(new { Errors = errorMessages });

                return responseValidation;
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

            try
            {
                await cache.RemoveAsync(cacheKey);
                logger.LogDebug("Cache invalidated for profile {UserId}.", id);
            }
            catch (Exception cacheEx)
            {
                logger.LogError(cacheEx, "WARNING: Failed to invalidate Redis cache for profile {UserId}. Data will be stale until TTL expires.", id);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new Contract.Profile.Full(request.Username, request.Bio, existing.AvatarUrl));

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating profile for user {UserId}.", id);

            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    [Function("UploadProfileImage")]
    public async Task<HttpResponseData> UploadProfileImage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "profiles/avatar")]
        HttpRequestData req)
    {
        const string ContainerName = "behemoth-container";
        var userId = req.GetUserId();

        try
        {
            var containerClient = blobs.GetBlobContainerClient(ContainerName);

            var fileName = $"{userId}-{DateTimeOffset.UtcNow.Ticks}.jpg";
            var blobClient = containerClient.GetBlobClient(fileName);

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "image/jpeg",
                    CacheControl = "public, max-age=31536000, immutable"
                }
            };

            await blobClient.UploadAsync(req.Body, uploadOptions);

            var publicUrl = GetPublicUrl(blobClient.Uri, ContainerName, fileName);

            logger.LogInformation("Image uploaded. Internal: {Internal} -> Public: {Public}", blobClient.Uri, publicUrl);

            var existingProfile = await context.Profiles.FindAsync(userId);
            if (existingProfile is null) return req.CreateResponse(HttpStatusCode.NotFound);

            existingProfile.UpdateAvatar(publicUrl);
            await context.SaveChangesAsync();

            await cache.RemoveAsync(CacheOptions.ProfileKey(userId));

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new Contract.Profile.Avatar(publicUrl));

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading profile image.");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    private string GetPublicUrl(Uri physicalUri, string container, string fileName)
    {
        var cdnHost = Environment.GetEnvironmentVariable("StoragePublicHost");

        return string.IsNullOrEmpty(cdnHost)
            ? physicalUri.ToString()
            : $"{cdnHost.TrimEnd('/')}/{container}/{fileName}";
    }
}