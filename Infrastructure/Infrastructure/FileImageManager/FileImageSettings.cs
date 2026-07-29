namespace Infrastructure.FileImageManager;
public class FileImageSettings
{
    public string PathFolder { get; set; } = string.Empty;
    public int MaxFileSizeInMB { get; set; }
    public string SupabaseUrl { get; set; } = string.Empty;
    public string SupabaseServiceRoleKey { get; set; } = string.Empty;
    public string SupabaseStorageBucket { get; set; } = "products";
}
