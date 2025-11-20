using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Behemoth.Web.Pages;

public partial class Profile : ComponentBase
{
    private Model? model;

    private bool isLoading = true;
    private bool isSaving;
    private bool isUploadingAvatar;
    private const long MaxFileSize = 5 * 1024 * 1024;

    protected override async Task OnInitializedAsync()
    {
        isLoading = true;
        try
        {
            var profile = await ProfileClient.GetMyProfileAsync();
            if (profile is null) Snackbar.Add("Errore nel caricamento del profilo", Severity.Error);

            model = new Model(profile);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Snackbar.Add($"Errore: {ex.Message}", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SaveAsync()
    {
        if (model is null || string.IsNullOrWhiteSpace(model.Username))
        {
            Snackbar.Add("Il nome utente è obbligatorio", Severity.Warning);
            return;
        }

        isSaving = true;
        try
        {
            var success = await ProfileClient.SaveProfileAsync(model.Username, model.Bio);
            if (success)
                Snackbar.Add("Profilo salvato con successo!", Severity.Success);
            else
                Snackbar.Add("Errore nel salvataggio del profilo", Severity.Error);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Errore: {ex.Message}", Severity.Error);
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task OnAvatarSelected(IBrowserFile file)
    {
        if (file.Size > MaxFileSize)
        {
            Snackbar.Add("Il file è troppo grande. Dimensione massima: 5MB", Severity.Warning);
            return;
        }

        if (!file.ContentType.StartsWith("image/"))
        {
            Snackbar.Add("Seleziona un'immagine valida", Severity.Warning);
            return;
        }

        isUploadingAvatar = true;
        try
        {
            var result = await ProfileClient.UploadProfileImageAsync(file, MaxFileSize);
            if (model is not null && result is not null)
            {
                model.AvatarUrl = result.Url;
                Snackbar.Add("Avatar caricato con successo!", Severity.Success);
            }
            else
            {
                Snackbar.Add("Errore nel caricamento dell'avatar", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Errore: {ex.Message}", Severity.Error);
        }
        finally
        {
            isUploadingAvatar = false;
        }
    }
}