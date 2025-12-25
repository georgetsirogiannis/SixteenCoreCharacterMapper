using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using SixteenCoreCharacterMapper.Core.Models;

namespace SixteenCoreCharacterMapper.Core.Services
{
    public interface IUpdateService
    {
        Task<UpdateInfo?> CheckForUpdateAsync(Version currentVersion);
    }

    public class UpdateService : IUpdateService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<UpdateInfo?> CheckForUpdateAsync(Version currentVersion)
        {
            // Ensure User-Agent is set for GitHub API or raw content access
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", AppConstants.UpdateCheckUserAgent);
            }

            try
            {
                using var stream = await _httpClient.GetStreamAsync(AppConstants.UpdateCheckUrl);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var updateInfo = await JsonSerializer.DeserializeAsync<UpdateInfo>(stream, options);

                if (updateInfo?.Version == null)
                {
                    return null;
                }

                // Logic to determine the correct URL for the current platform
                ResolvePlatformUrl(updateInfo);

                // If we couldn't find a URL for this platform, we can't update
                if (string.IsNullOrEmpty(updateInfo.Url))
                {
                    return null;
                }

                if (Version.TryParse(updateInfo.Version.Trim(), out var latestVersion))
                {
                    if (latestVersion > currentVersion)
                    {
                        return updateInfo;
                    }
                }
            }
            catch
            {
                // Fail silently on network/parsing errors
                return null;
            }

            return null;
        }

        private void ResolvePlatformUrl(UpdateInfo info)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Prefer specific Windows URL, fallback to legacy Url
                if (!string.IsNullOrEmpty(info.UrlWindows))
                {
                    info.Url = info.UrlWindows;
                }
                // If UrlWindows is empty, info.Url is already set from JSON (Legacy support)
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Check architecture for Apple Silicon vs Intel
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    info.Url = info.UrlMacArm;
                }
                else
                {
                    info.Url = info.UrlMacIntel;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                info.Url = info.UrlLinux;
            }
            else
            {
                // Unknown platform
                info.Url = null;
            }
        }
    }
}