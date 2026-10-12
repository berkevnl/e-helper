using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;

namespace EHelper.Services
{
    public class UpdateCheckResult
    {
        public bool HasUpdate { get; set; }
        public string CurrentVersion { get; set; } = "";
        public string LatestVersion { get; set; } = "";
        public string? DownloadUrl { get; set; }
        public string? ReleaseNotes { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public static class UpdateService
    {
        private const string GitHubRepo = "berkevnl/E-Helper";
        public const string CurrentVersion = "1.1.1";

        private static readonly HttpClient HttpClient = new();

        static UpdateService()
        {
            HttpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("E-Helper-App", CurrentVersion)
            );
            HttpClient.Timeout = TimeSpan.FromSeconds(20);
        }

        /// <summary>
        /// Silently checks for updates in the background without showing any popups.
        /// Returns UpdateCheckResult containing version details and zip/exe download URL.
        /// </summary>
        public static async Task<UpdateCheckResult> CheckUpdateSilentAsync()
        {
            var result = new UpdateCheckResult
            {
                CurrentVersion = CurrentVersion,
                LatestVersion = CurrentVersion,
                HasUpdate = false
            };

            try
            {
                string url = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";
                using var response = await HttpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"HTTP {(int)response.StatusCode}";
                    return result;
                }

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
                string body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";
                string cleanVersion = tagName.TrimStart('v', 'V').Trim();

                result.LatestVersion = string.IsNullOrEmpty(cleanVersion) ? CurrentVersion : cleanVersion;
                result.ReleaseNotes = body;

                if (Version.TryParse(cleanVersion, out var remoteVersion) &&
                    Version.TryParse(CurrentVersion, out var localVersion))
                {
                    if (remoteVersion > localVersion)
                    {
                        result.HasUpdate = true;
                    }
                }

                string? zipUrl = null;
                string? exeUrl = null;

                if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assetsProp.EnumerateArray())
                    {
                        if (asset.TryGetProperty("name", out var nameProp) &&
                            asset.TryGetProperty("browser_download_url", out var urlProp))
                        {
                            string name = nameProp.GetString() ?? "";
                            string assetUrl = urlProp.GetString() ?? "";

                            if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                zipUrl = assetUrl;
                            }
                            else if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && exeUrl == null)
                            {
                                exeUrl = assetUrl;
                            }
                        }
                    }
                }

                result.DownloadUrl = zipUrl ?? exeUrl;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Downloads the latest release package (zip or exe), extracts the new executable,
        /// launches a self-replacement script, and gracefully closes the current process.
        /// The user experiences a quick seamless restart into the updated version.
        /// </summary>
        public static async Task<bool> DownloadAndRestartAsync(string downloadUrl)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl)) return false;

            try
            {
                string currentExePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(currentExePath) || !File.Exists(currentExePath))
                {
                    // Fallback to entry assembly location
                    currentExePath = Path.Combine(AppContext.BaseDirectory, "E-Helper.exe");
                }

                string updateWorkDir = Path.Combine(Path.GetTempPath(), "EHelperUpdate_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(updateWorkDir);

                string downloadedFilePath = Path.Combine(updateWorkDir, "package" + (downloadUrl.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? ".zip" : ".exe"));

                // Download the asset file
                using (var downloadStream = await HttpClient.GetStreamAsync(downloadUrl))
                using (var fileStream = new FileStream(downloadedFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await downloadStream.CopyToAsync(fileStream);
                }

                string newExePath = "";

                if (downloadedFilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    string extractDir = Path.Combine(updateWorkDir, "extracted");
                    ZipFile.ExtractToDirectory(downloadedFilePath, extractDir);

                    // Find the extracted E-Helper.exe
                    string[] foundExes = Directory.GetFiles(extractDir, "E-Helper.exe", SearchOption.AllDirectories);
                    if (foundExes.Length > 0)
                    {
                        newExePath = foundExes[0];
                    }
                    else
                    {
                        // Any exe in the extracted directory
                        string[] anyExes = Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories);
                        if (anyExes.Length > 0)
                        {
                            newExePath = anyExes[0];
                        }
                    }
                }
                else
                {
                    newExePath = downloadedFilePath;
                }

                if (string.IsNullOrEmpty(newExePath) || !File.Exists(newExePath))
                {
                    return false;
                }

                // Create a batch script that waits for current process to exit, replaces the executable, and relaunches it
                int currentPid = Process.GetCurrentProcess().Id;
                string scriptPath = Path.Combine(updateWorkDir, "update.bat");

                string batContent = $@"@echo off
chcp 65001 >nul
timeout /t 1 /nobreak >nul
:waitloop
tasklist /fi ""PID eq {currentPid}"" 2>nul | find ""{currentPid}"" >nul
if not errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto waitloop
)

copy /y ""{newExePath}"" ""{currentExePath}"" >nul
if errorlevel 1 (
    timeout /t 1 /nobreak >nul
    copy /y ""{newExePath}"" ""{currentExePath}"" >nul
)

start """" ""{currentExePath}""
timeout /t 2 /nobreak >nul
rmdir /s /q ""{updateWorkDir}"" >nul 2>&1
";

                File.WriteAllText(scriptPath, batContent);

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"\"{scriptPath}\"\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(currentExePath) ?? AppContext.BaseDirectory
                };

                Process.Start(psi);

                // Gracefully shut down current application
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current is App app)
                    {
                        app.ShutdownApp();
                    }
                    else
                    {
                        Application.Current.Shutdown();
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update failed: {ex.Message}");
                return false;
            }
        }

        public static async Task CheckForUpdatesAsync(bool isEnglish, Window? owner = null)
        {
            try
            {
                var check = await CheckUpdateSilentAsync();

                if (!string.IsNullOrEmpty(check.ErrorMessage) && !check.HasUpdate && check.DownloadUrl == null)
                {
                    string notFoundMsg = isEnglish
                        ? "Could not check updates from GitHub.\nUpdates will be downloaded here automatically once a release is published."
                        : "GitHub üzerinde güncelleme kontrol edilemedi.\nİlk sürüm yayınlandığında güncellemeler buradan otomatik kontrol edilecektir.";

                    MessageBox.Show(owner ?? Application.Current.MainWindow, notFoundMsg, "E-Helper", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!check.HasUpdate)
                {
                    string upToDateMsg = isEnglish
                        ? $"E-Helper is up to date (v{CurrentVersion}). No new updates found."
                        : $"E-Helper güncel (v{CurrentVersion}). Yeni bir güncelleme bulunmuyor.";

                    MessageBox.Show(owner ?? Application.Current.MainWindow, upToDateMsg, "E-Helper", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // New version found
                string promptMsg = isEnglish
                    ? $"A new version is available: v{check.LatestVersion}\n(Current version: v{CurrentVersion})\n\nRelease notes:\n{check.ReleaseNotes}\n\nDo you want to update to v{check.LatestVersion} now?"
                    : $"Yeni bir sürüm mevcut: v{check.LatestVersion}\n(Mevcut sürüm: v{CurrentVersion})\n\nSürüm notları:\n{check.ReleaseNotes}\n\nv{check.LatestVersion} sürümüne şimdi güncellemek istiyor musunuz?";

                var result = MessageBox.Show(
                    owner ?? Application.Current.MainWindow,
                    promptMsg,
                    isEnglish ? "Update Available" : "Güncelleme Mevcut",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information
                );

                if (result == MessageBoxResult.Yes && !string.IsNullOrEmpty(check.DownloadUrl))
                {
                    bool started = await DownloadAndRestartAsync(check.DownloadUrl);
                    if (!started)
                    {
                        Process.Start(new ProcessStartInfo(check.DownloadUrl) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                string errMsg = isEnglish
                    ? $"Update check failed: {ex.Message}"
                    : $"Güncelleme kontrolü başarısız oldu: {ex.Message}";

                MessageBox.Show(owner ?? Application.Current.MainWindow, errMsg, "E-Helper", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
