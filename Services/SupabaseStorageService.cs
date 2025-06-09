using Supabase;

public class SupabaseStorageService
{
    private readonly Client _client;
    private readonly string _bucketName = "post-images";

    public SupabaseStorageService(Client client)
    {
        _client = client;
    }

    public async Task<string> UploadImageAsync(IFormFile file)
    {
        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);

        using var stream = file.OpenReadStream();

        // Convert stream to byte[]
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();

        var result = await _client.Storage
            .From(_bucketName)
            .Upload(fileBytes, fileName, new Supabase.Storage.FileOptions
            {
                CacheControl = "3600",
                Upsert = true
            });

        if (string.IsNullOrWhiteSpace(result))
        {
            throw new Exception("Failed to upload file to Supabase.");
        }

        // Construct public URL 
        return $"https://ashsitapkuigakmgrijr.supabase.co/storage/v1/object/public/{_bucketName}/{fileName}";
    }
}
