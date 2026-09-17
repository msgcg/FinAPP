using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FinAPP.Models;

namespace FinAPP.Services;

public class StorageService
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public StorageService(string? customPath = null)
    {
        _filePath = customPath ?? GetDefaultPath();
    }

    private static string GetDefaultPath()
    {
        try
        {
            var mauiType = Type.GetType("Microsoft.Maui.Storage.FileSystem, Microsoft.Maui.Essentials");
            if (mauiType != null)
            {
                var prop = mauiType.GetProperty("AppDataDirectory");
                var dir = prop?.GetValue(null) as string;
                if (!string.IsNullOrEmpty(dir))
                {
                    return Path.Combine(dir, "finapp_profile.json");
                }
            }
        }
        catch { }

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrEmpty(appData))
        {
            string dir = Path.Combine(appData, "FinAPP");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "finapp_profile.json");
        }

        return Path.Combine(Path.GetTempPath(), "finapp_profile.json");
    }

    public async Task<PetProfile> LoadProfileAsync()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath);
                var profile = JsonSerializer.Deserialize<PetProfile>(json, JsonOptions);
                if (profile != null) return profile;
            }
        }
        catch (Exception)
        {
            // fallback to default
        }

        return CreateInitialProfile();
    }

    public async Task SaveProfileAsync(PetProfile profile)
    {
        try
        {
            var json = JsonSerializer.Serialize(profile, JsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch (Exception)
        {
            // silent save failure handling
        }
    }

    public PetProfile ResetToDemoProfile()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
        catch (Exception)
        {
            // ignore
        }

        return CreateInitialProfile();
    }

    public PetProfile CreateInitialProfile()
    {
        return new PetProfile
        {
            KidName = "Юный финансист",
            PetName = "Финни",
            Outfit = OutfitType.ClassicGreen,
            Accessory = AccessoryType.None,
            Stage = GrowthStage.Baby,
            Hunger = 90,
            Mood = 90,
            Balance = 400,
            Savings = 100,
            SelectedGoalId = "goal_scooter",
            CurrentPeriod = 1,
            IsDemoMode = true,
            IsOnboardingCompleted = true,
            PlannedObligatory = 150,
            PlannedDiscretionary = 100,
            PlannedSavings = 100,
            IsPlanConfirmed = false,
            ActualObligatory = 0,
            ActualDiscretionary = 0,
            ActualSavings = 0,
            TestsPassedCount = 0
        };
    }
}
