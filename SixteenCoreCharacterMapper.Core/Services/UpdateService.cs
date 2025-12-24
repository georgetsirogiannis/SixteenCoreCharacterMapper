using System;
using System.Net.Http;
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

            using var stream = await _httpClient.GetStreamAsync(AppConstants.UpdateCheckUrl);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var updateInfo = await JsonSerializer.DeserializeAsync<UpdateInfo>(stream, options);

            if (updateInfo?.Version == null || updateInfo.Url == null || updateInfo.ReleaseNotes == null)
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

            return null;
        }
    }
}
