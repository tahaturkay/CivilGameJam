using UnityEngine;

/// <summary>Read the existing authored schedule without maintaining a second disaster state.</summary>
public static class IslandDisasterCalendar
{
    public static int TsunamiDay
    {
        get
        {
            var settings=DengeAyarlari.Aktif;
            if(settings!=null&&settings.afetler!=null)
                foreach(var incident in settings.afetler)
                    if(incident.afet==AfetTuru.Tsunami)return incident.gun;
            return -1;
        }
    }

    public static bool IsDisasterDay(int day)
    {
        var settings=DengeAyarlari.Aktif;
        if(settings!=null&&settings.afetler!=null)
            foreach(var incident in settings.afetler)if(incident.gun==day)return true;
        return false;
    }

    public static bool TsunamiShelterRequired(GameManager gm)
    {
        int day=TsunamiDay;
        return gm!=null&&day>1&&(gm.gun==day-1||gm.gun==day);
    }

    // Give the previous evening a sunrise into the disaster day; do not stack moonlight
    // darkness on the authored storm/fire palette. The following morning also stays smooth.
    public static float DaylightOverride(int day,float phase)
    {
        // The harbor arrival is shown in daylight on the warning day as well.
        if(IsDisasterDay(day)||day==TsunamiDay-1)return 1;
        if(IsDisasterDay(day+1)||day+1==TsunamiDay-1)return Mathf.SmoothStep(0,1,Mathf.InverseLerp(.82f,1,phase));
        if(IsDisasterDay(day-1))return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.18f,phase));
        return 0;
    }
}
