namespace RestaurantPOS.Models.Requests;

public class RestoreSystemBackupRequest
{
    public string Password { get; set; } = string.Empty;

    public IFormFile? File { get; set; }
}
