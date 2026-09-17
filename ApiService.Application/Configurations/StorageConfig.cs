namespace ApiService.Application.Configurations
{
    public class StorageConfig
    {
        public string UploadPath { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string ImageFolder { get; set; } = string.Empty;
        public string AttachmentFolder { get; set; } = string.Empty;
    }
}