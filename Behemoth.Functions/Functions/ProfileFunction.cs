using System.Net;
                    using Behemoth.Domain;
                    using Behemoth.Infrastructure;
                    using Behemoth.Functions.Extensions;
                    using Microsoft.Azure.Functions.Worker;
                    using Microsoft.Azure.Functions.Worker.Http;
                    using Microsoft.Extensions.Logging;
                    using Azure.Storage.Blobs;
                    // Aggiungi i using per i contratti condivisi
                    using Behemoth.Contracts;
                    
                    namespace Behemoth.Functions.Functions;
                    
                    public class ProfileFunction(ILogger<ProfileFunction> logger, BehemothContext context, BlobServiceClient containerClient)
                    {
                        private readonly BlobContainerClient _containerClient = containerClient.GetBlobContainerClient("avatars");
                    
                        [Function("UploadProfileImage")]
                        public async Task<HttpResponseData> UploadProfileImage(
                            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "profiles/image")] HttpRequestData req)
                        {
                            logger.LogInformation("C# HTTP trigger function processed a request to upload a profile image.");
                    
                            try
                            {
                                var userId = req.GetUserId();
                    
                                var file = req.Body;
                    
                                var blobName = $"{userId}-{DateTimeOffset.UtcNow.Ticks}";
                                var blobClient = _containerClient.GetBlobClient(blobName);
                    
                                await blobClient.UploadAsync(file, overwrite: true);
                                var newAvatarUrl = blobClient.Uri.ToString();
                    
                                var existingProfile = await context.Profiles.FindAsync(userId);
                                if (existingProfile is null) return req.CreateResponse(HttpStatusCode.NotFound);
                    
                                // Poiché Profile è immutabile, creiamo una nuova istanza con l'URL aggiornato
                                var updatedProfile = existingProfile with { AvatarUrl = newAvatarUrl };
                                context.Profiles.Update(updatedProfile);
                                await context.SaveChangesAsync();
                    
                                var response = req.CreateResponse(HttpStatusCode.OK);
                                await response.WriteAsJsonAsync(new UploadImageResponse(newAvatarUrl));
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
                                    profile = new Profile(userId, string.Empty, null, null);
                                    context.Profiles.Add(profile);
                                    await context.SaveChangesAsync();
                                }
                    
                                var response = req.CreateResponse(HttpStatusCode.OK);
                                // Mappa l'entità al DTO usando l'inizializzatore di oggetto
                                var profileDto = new ProfileDto
                                {
                                    Id = profile.Id,
                                    Username = profile.Username,
                                    Bio = profile.Bio,
                                    AvatarUrl = profile.AvatarUrl
                                };
                                await response.WriteAsJsonAsync(profileDto);
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
                    
                                // Specifica il namespace per risolvere l'ambiguità
                                var request = await req.ReadFromJsonAsync<Behemoth.Contracts.UpdateProfileRequest>();
                                if (request == null)
                                {
                                    return req.CreateResponse(HttpStatusCode.BadRequest);
                                }
                    
                                var existingProfile = await context.Profiles.FindAsync(userId);
                                Profile updatedProfile;
                    
                                if (existingProfile == null)
                                {
                                    logger.LogInformation("Creating new profile for user {UserId}", userId);
                                    updatedProfile = new Profile(userId, request.Username, request.Bio, null);
                                    context.Profiles.Add(updatedProfile);
                                }
                                else
                                {
                                    logger.LogInformation("Updating profile for user {UserId}", userId);
                                    // Crea una nuova istanza del record con i valori aggiornati
                                    updatedProfile = existingProfile with
                                    {
                                        Username = request.Username,
                                        Bio = request.Bio
                                    };
                                    context.Profiles.Update(updatedProfile);
                                }
                    
                                await context.SaveChangesAsync();
                    
                                var response = req.CreateResponse(HttpStatusCode.OK);
                                // Mappa l'entità aggiornata al DTO
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
                    }