namespace EHelper.Core
{
    /// <summary>
    /// Power mode + keyboard lighting state shared by the global settings, app profiles and the live UI state.
    /// </summary>
    public interface ILightingProfile
    {
        PowerMode PowerMode { get; set; }
        LedMode LedMode { get; set; }
        byte LedBrightness { get; set; }
        int CycleSpeed { get; set; }

        byte Red { get; set; }
        byte Green { get; set; }
        byte Blue { get; set; }

        byte ZoneLeftRed { get; set; }
        byte ZoneLeftGreen { get; set; }
        byte ZoneLeftBlue { get; set; }
        byte ZoneCenterRed { get; set; }
        byte ZoneCenterGreen { get; set; }
        byte ZoneCenterBlue { get; set; }
        byte ZoneRightRed { get; set; }
        byte ZoneRightGreen { get; set; }
        byte ZoneRightBlue { get; set; }
    }

    public static class LightingProfileExtensions
    {
        /// <summary>
        /// Copies LED mode, brightness, single color and 3-zone colors.
        /// Power mode and cycle speed are copied only when <paramref name="includePowerAndSpeed"/> is true.
        /// </summary>
        public static void CopyLightingTo(this ILightingProfile src, ILightingProfile dst, bool includePowerAndSpeed = true)
        {
            if (includePowerAndSpeed)
            {
                dst.PowerMode = src.PowerMode;
                dst.CycleSpeed = src.CycleSpeed;
            }

            dst.LedMode = src.LedMode;
            dst.LedBrightness = src.LedBrightness;
            dst.Red = src.Red;
            dst.Green = src.Green;
            dst.Blue = src.Blue;
            dst.ZoneLeftRed = src.ZoneLeftRed;
            dst.ZoneLeftGreen = src.ZoneLeftGreen;
            dst.ZoneLeftBlue = src.ZoneLeftBlue;
            dst.ZoneCenterRed = src.ZoneCenterRed;
            dst.ZoneCenterGreen = src.ZoneCenterGreen;
            dst.ZoneCenterBlue = src.ZoneCenterBlue;
            dst.ZoneRightRed = src.ZoneRightRed;
            dst.ZoneRightGreen = src.ZoneRightGreen;
            dst.ZoneRightBlue = src.ZoneRightBlue;
        }
    }
}
