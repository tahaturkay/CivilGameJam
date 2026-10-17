using UnityEngine;

/// <summary>A periodic sunrise/day/sunset/moonlight palette, continuous across the day counter rollover.</summary>
public static class IslandSolarCycle
{
    public readonly struct Sample
    {
        public readonly float night,dawn,dusk,elevation,azimuth;
        public float Twilight=>Mathf.Max(dawn,dusk);
        public Sample(float night,float dawn,float dusk,float elevation,float azimuth)
        {this.night=night;this.dawn=dawn;this.dusk=dusk;this.elevation=elevation;this.azimuth=azimuth;}
    }

    public static Sample Evaluate(float phase)
    {
        float p=Mathf.Repeat(phase,1);
        // Both sides of midnight evaluate the same dawn curve, including its first derivative.
        // The >=50% darkness interval remains 60%..100% (40% of the economic day).
        float night=p<.3f ? 1-Smooth(-.16f,.16f,p)
            : Smooth(.40f,.80f,p)*(1-Smooth(.84f,1.16f,p));
        float dawn=1-Smooth(0,.18f,Mathf.Abs(Mathf.DeltaAngle(p*360,0))/360f);
        float dusk=Smooth(.34f,.54f,p)*(1-Smooth(.62f,.82f,p));
        float arc=p<.6f?Mathf.Sin(p/.6f*Mathf.PI):Mathf.Sin((p-.6f)/.4f*Mathf.PI);
        float elevation=6+arc*arc*(p<.6f?49:24);
        return new Sample(night,dawn,dusk,elevation,p*360-65);
    }
    static float Smooth(float a,float b,float value)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,value));
}
