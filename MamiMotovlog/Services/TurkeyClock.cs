namespace MamiMotovlog.Services;

public static class TurkeyClock
{
    public static DateTime Today => DateTime.UtcNow.AddHours(3).Date;
}
