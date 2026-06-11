using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Text.Json;

namespace FluxRoute.ViewModels;

public partial class MainViewModel
{
    [ObservableProperty] private string singBoxConfigStatus = "Не проверено";
    [ObservableProperty] private bool isSingBoxConfigValid;

    [RelayCommand]
    private void ValidateSingBoxConfig()
    {
        try
        {
            var singboxDir = Path.Combine(EngineDir, "sing-box");
            var configPath = Path.Combine(singboxDir, "config.json");
            if (!File.Exists(configPath))
            {
                SingBoxConfigStatus = "❌ Файл config.json не найден";
                IsSingBoxConfigValid = false;
                return;
            }

            var content = File.ReadAllText(configPath);
            // Simple JSON validation
            JsonDocument.Parse(content);

            SingBoxConfigStatus = "✅ Конфигурация валидна";
            IsSingBoxConfigValid = true;
            Logs.Add("[Sing-Box] Конфигурация проверена успешно.");
        }
        catch (Exception ex)
        {
            SingBoxConfigStatus = $"❌ Ошибка JSON: {ex.Message}";
            IsSingBoxConfigValid = false;
            Logs.Add($"[Sing-Box] Ошибка конфигурации: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenSingBoxConfig()
    {
        try
        {
            var singboxDir = Path.Combine(EngineDir, "sing-box");
            Directory.CreateDirectory(singboxDir);
            var configPath = Path.Combine(singboxDir, "config.json");
            if (!File.Exists(configPath))
                File.WriteAllText(configPath, "{\n  \"outbounds\": []\n}");

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(configPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Logs.Add($"[Sing-Box] Не удалось открыть конфиг: {ex.Message}");
        }
    }
}
