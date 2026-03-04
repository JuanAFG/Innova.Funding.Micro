
namespace InnovaFunding.Functions.Util
{
    public static class HelperExtensions
    {
        //public static DateTime GetLocalDateTime()
        //{
        //    DateTime serverTime = DateTime.Now; // gives you current Time in server timeZone
        //    DateTime utcTime = serverTime.ToUniversalTime(); // convert it to Utc using timezone setting of server computer
        //    TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        //    DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, tzi); // convert from utc to local
        //    return localTime;
        //}
        public static DateTime GetLocalDateTime()
       
       => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SA Pacific Standard Time");
        
    }
}
