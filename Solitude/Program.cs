global using Serilog;

using Solitude.Managers;
using Solitude.Objects;
using Spectre.Console;

var dataminer = Core.Init().Result;

if (dataminer is null)
    return;

var choice = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Choose the [45]Solitude[/] mode")
        .PageSize(10)
        .HighlightStyle("45")
        .MoreChoicesText("[grey](Move up and down to see more options)[/]")
        .AddChoices(new[]
        {
            "Get New",
            "Update Mode"
        }));

ESolitudeMode mode;

switch (choice)
{
    case "Update Mode":
        mode = ESolitudeMode.UpdateMode;
        break;

    case "Get New":
    default:
        mode = ESolitudeMode.GetNew;
        break;
}

await Core.RunAsync(mode, dataminer);

Log.Information("Done! Press any key to close.");

Console.ReadKey();