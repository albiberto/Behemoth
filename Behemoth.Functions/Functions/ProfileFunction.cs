using System.Net;
using System.Security.Claims;
using Azure.Storage.Blobs;
using Behemoth.Domain;
using Behemoth.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Behemoth.Functions.Functions;

public class ProfileFunction(ILogger<ProfileFunction> logger, BehemothContext context, BlobContainerClient containerClient)
{
    [Function("UploadProfileImage")]
    public async Task<HttpResponseData> UploadProfileImage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "profiles/image")] HttpRequestData req) // Metti "User" per sicurezza
    {
        logger.LogInformation("C# HTTP trigger function processed a request to upload a profile image.");

        try
        {
            var userEmail = GetUserEmail(req);
            
            // Non serve più creare il container, è già iniettato
            // var containerClient = blobServiceClient.GetBlobContainerClient("profile-images");

            // Leggi il file dal body (questo presume un file binario, non multipart)
            var file = req.Body; 
            
            // Usa un nome prevedibile legato all'utente. Es: "avatars/utente@email.com.jpg"
            // Aggiungi un timestamp per evitare caching
            var blobName = $"avatars/{userEmail}?t={DateTimeOffset.UtcNow.Ticks}"; 
            var blobClient = containerClient.GetBlobClient(blobName);

            // Carica l'immagine
            await blobClient.UploadAsync(file, overwrite: true);
            var newAvatarUrl = blobClient.Uri.ToString();

            // *** CRUCIALE: Aggiorna il profilo in Cosmos con la nuova URL ***
            var profile = await context.Profiles.FirstOrDefaultAsync(p => p.Email == userEmail);
            if (profile == null)
            {
                // Se non esiste, crea un profilo base. 
                // Assicurati che l'utente abbia un username prima di fare questo.
                // Meglio sarebbe che il profilo esista già.
                // Per ora, assumiamo che esista o che venga creato da CreateOrUpdateProfile.
                 return req.CreateResponse(HttpStatusCode.NotFound);
            }

            // Aggiorna il record (i record sono immutabili, ne crei uno nuovo)
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "profiles/me")] HttpRequestData req) // Metti "User" per sicurezza
    {
        logger.LogInformation("Request to get current user's profile.");
        
        try
        {
            var userEmail = GetUserEmail(req);
            var profile = await context.Profiles.FirstOrDefaultAsync(p => p.Email == userEmail);

            if (profile == null)
            {
                // Non trovato. La UI dovrà gestire questo caso
                // e richiedere la creazione di un profilo.
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(profile);
            return response;
        }
        catch (Exception ex)
        {
             logger.LogError(ex, "Error getting profile.");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    [Function("CreateOrUpdateMyProfile")]
    public async Task<HttpResponseData> CreateOrUpdateMyProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "profiles")] HttpRequestData req) // Metti "User" per sicurezza
    {
        logger.LogInformation("Request to create or update a profile.");

        try
        {
            // 1. Prendi l'email dai claims, NON dal body
            var userEmail = GetUserEmail(req);

            // 2. Leggi il DTO dal body (che non contiene l'email)
            var request = await req.ReadFromJsonAsync<UpdateProfileRequest>();
            if (request == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var existingProfile = await context.Profiles.FirstOrDefaultAsync(p => p.Email == userEmail);
            Profile profileToSave;

            if (existingProfile == null)
            {
                // 3. PRIMA CREAZIONE: usa l'email dei claims
                logger.LogInformation($"Creating new profile for {userEmail}");
                profileToSave = new Profile(
                    Email: userEmail, 
                    Username: request.Username, 
                    Bio: request.Bio,
                    AvatarUrl: null // L'avatar è un'altra chiamata
                );
                context.Profiles.Add(profileToSave);
            }
            else
            {
                // 4. AGGIORNAMENTO: aggiorna solo i campi permessi
                logger.LogInformation($"Updating profile for {userEmail}");
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