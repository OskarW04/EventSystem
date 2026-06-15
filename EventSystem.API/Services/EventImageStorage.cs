namespace EventSystem.API.Services;

// Wspólna logika zapisu banera wydarzenia na dysk - używana zarówno przez
// organizatorski upload (EventService), jak i adminowy (SystemAdminService).
public static class EventImageStorage
{
    // Zapisuje przesłany plik i zwraca względny URL (/images/events/...).
    public static async Task<string> SaveAsync(IFormFile image)
    {
        var uploadsFolder = Path.Combine(
            Directory.GetCurrentDirectory(), "wwwroot", "images", "events");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await image.CopyToAsync(stream);

        return $"/images/events/{fileName}";
    }
}
