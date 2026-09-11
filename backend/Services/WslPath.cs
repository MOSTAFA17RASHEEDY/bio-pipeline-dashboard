namespace BioPipeline.Api.Services;

/// <summary>
/// The API runs as a normal Windows .NET process, but Nextflow runs inside
/// WSL2 (see pipeline/README.md for why). Any path we pass into a Nextflow
/// command needs to be in WSL's view of the filesystem, not Windows':
/// "E:\Projects\...\pipeline" becomes "/mnt/e/Projects/.../pipeline".
/// </summary>
public static class WslPath
{
    public static string FromWindows(string windowsPath)
    {
        var full = Path.GetFullPath(windowsPath);
        if (full.Length < 2 || full[1] != ':')
            throw new ArgumentException($"Expected an absolute Windows path (e.g. 'E:\\...'), got '{windowsPath}'.");

        var driveLetter = char.ToLowerInvariant(full[0]);
        var rest = full[2..].Replace('\\', '/');
        return $"/mnt/{driveLetter}{rest}";
    }
}
