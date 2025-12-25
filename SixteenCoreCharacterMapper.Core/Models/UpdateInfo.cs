namespace SixteenCoreCharacterMapper.Core.Models
{
    public class UpdateInfo
    {
        public string? Version { get; set; }

        // Legacy field (used by v1.x clients) - Points to Windows Installer
        public string? Url { get; set; }

        public string? ReleaseNotes { get; set; }

        // New platform-specific fields
        public string? UrlWindows { get; set; }
        public string? UrlMacIntel { get; set; }
        public string? UrlMacArm { get; set; }
        public string? UrlLinux { get; set; }
    }
}