namespace RestaurantPOS.Services;

public interface ISystemBackupService
{
    Task WriteBackupArchiveAsync(Stream outputStream, CancellationToken cancellationToken = default);

    Task RestoreFromArchiveAsync(Stream archiveStream, CancellationToken cancellationToken = default);
}
