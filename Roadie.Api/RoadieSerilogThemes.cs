using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Api
{
    public static class RoadieSerilogThemes
    {
        public static AnsiConsoleTheme RoadieRainbow { get; } = new AnsiConsoleTheme(
            new Dictionary<ConsoleThemeStyle, string>
            {
                [ConsoleThemeStyle.Text] = "\x1b[38;5;0034m",
                [ConsoleThemeStyle.SecondaryText] = "\x1b[38;5;0025m",
                [ConsoleThemeStyle.TertiaryText] = "\x1b[38;5;0089m",
                [ConsoleThemeStyle.Invalid] = "\x1b[38;5;0126m",
                [ConsoleThemeStyle.Null] = "\x1b[38;5;0100m",
                [ConsoleThemeStyle.Name] = "\x1b[38;5;0007m",
                [ConsoleThemeStyle.String] = "\x1b[38;5;0117m",
                [ConsoleThemeStyle.Number] = "\x1b[38;5;0200m",
                [ConsoleThemeStyle.Boolean] = "\x1b[38;5;0027m",
                [ConsoleThemeStyle.Scalar] = "\x1b[38;5;0085m",
                [ConsoleThemeStyle.LevelVerbose] = "\x1b[38;5;0007m",
                [ConsoleThemeStyle.LevelDebug] = "\x1b[38;5;0015m",
                [ConsoleThemeStyle.LevelInformation] = "\x1b[38;5;0015m",
                [ConsoleThemeStyle.LevelWarning] = "\x1b[38;5;0011m",
                [ConsoleThemeStyle.LevelError] = "\x1b[38;5;0015m\x1b[48;5;0196m",
                [ConsoleThemeStyle.LevelFatal] = "\x1b[38;5;0011m\x1b[48;5;0009m",
            }
        );
    }
}
