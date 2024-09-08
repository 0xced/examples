using System;
using System.Threading;
using Spectre.Console;

namespace Prompt
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                // Do not terminate the current process on Ctrl+C
                eventArgs.Cancel = true;
                cts.Cancel();
            };

            var result = RunPrompts(cts.Token);
            if (result == null)
                return;

            // Summary
            AnsiConsole.Write(new Rule("[yellow]Results[/]").RuleStyle("grey").LeftJustified());
            const string NotAvailable = "[b]N/A[/]";
            AnsiConsole.Write(new Table().AddColumns("[grey]Question[/]", "[grey]Answer[/]")
                .RoundedBorder()
                .BorderColor(Color.Grey)
                .AddRow("[grey]Name[/]", result.Name ?? NotAvailable)
                .AddRow("[grey]Favorite fruit[/]", result.Fruit ?? NotAvailable)
                .AddRow("[grey]Favorite sport[/]", result.Sport ?? NotAvailable)
                .AddRow("[grey]Age[/]", result.Age?.ToString() ?? NotAvailable)
                .AddRow("[grey]Password[/]", result.Password ?? NotAvailable)
                .AddRow("[grey]Mask[/]", result.Mask ?? NotAvailable)
                .AddRow("[grey]Null Mask[/]", result.NullMask ?? NotAvailable)
                .AddRow("[grey]Favorite color[/]", result.Color == null ? NotAvailable : result.Color.Length == 0 ? "Unknown" : result.Color));
        }

        public static Result? RunPrompts(CancellationToken cancellationToken)
        {
            // Check if we can accept key strokes
            if (!AnsiConsole.Profile.Capabilities.Interactive)
            {
                AnsiConsole.MarkupLine("[red]Environment does not support interaction.[/]");
                return null;
            }

            // Confirmation
            try
            {
                if (!AnsiConsole.Confirm("Run prompt example?", cancellationToken: cancellationToken))
                {
                    AnsiConsole.MarkupLine("Ok... :(");
                    return null;
                }
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("Canceled... :(");
                return null;
            }

            string? name = null;
            string? fruit = null;
            string? sport = null;
            int? age = null;
            string? password = null;
            string? mask = null;
            string? nullMask = null;
            string? color = null;

            try
            {
                // Ask the user for some different things
                WriteDivider("Strings");
                name = AskName(cancellationToken);

                WriteDivider("Lists");
                fruit = AskFruit(cancellationToken);

                WriteDivider("Choices");
                sport = AskSport(cancellationToken);

                WriteDivider("Integers");
                age = AskAge(cancellationToken);

                WriteDivider("Secrets");
                password = AskPassword(cancellationToken);

                WriteDivider("Mask");
                mask = AskPasswordWithCustomMask(cancellationToken);

                WriteDivider("Null Mask");
                nullMask = AskPasswordWithNullMask(cancellationToken);

                WriteDivider("Optional");
                color = AskColor(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.WriteLine();
            }

            return new Result(name, fruit, sport, age, password, mask, nullMask, color);
        }

        private static void WriteDivider(string text)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule($"[yellow]{text}[/]").RuleStyle("grey").LeftJustified());
        }

        public static string AskName(CancellationToken cancellationToken)
        {
            var name = AnsiConsole.Ask<string>("What's your [green]name[/]?", cancellationToken);
            return name;
        }

        public static string
            AskFruit(CancellationToken cancellationToken)
        {
            var favorites = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .PageSize(10)
                    .Title("What are your [green]favorite fruits[/]?")
                    .MoreChoicesText("[grey](Move up and down to reveal more fruits)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle a fruit, [green]<enter>[/] to accept)[/]")
                    .AddChoiceGroup("Berries", new[]
                    {
                        "Blackcurrant", "Blueberry", "Cloudberry",
                        "Elderberry", "Honeyberry", "Mulberry"
                    })
                    .AddChoices(new[]
                    {
                        "Apple", "Apricot", "Avocado", "Banana",
                        "Cherry", "Cocunut", "Date", "Dragonfruit", "Durian",
                        "Egg plant",  "Fig", "Grape", "Guava",
                        "Jackfruit", "Jambul", "Kiwano", "Kiwifruit", "Lime", "Lylo",
                        "Lychee", "Melon", "Nectarine", "Orange", "Olive"
                    }), cancellationToken);

            var fruit = favorites.Count == 1 ? favorites[0] : null;
            if (string.IsNullOrWhiteSpace(fruit))
            {
                fruit = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .EnableSearch()
                        .Title("Ok, but if you could only choose [green]one[/]?")
                        .MoreChoicesText("[grey](Move up and down to reveal more fruits)[/]")
                        .AddChoices(favorites));
            }

            AnsiConsole.MarkupLine("You selected: [yellow]{0}[/]", fruit);
            return fruit;
        }

        public static string AskSport(CancellationToken cancellationToken)
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>("What's your [green]favorite sport[/]?")
                    .InvalidChoiceMessage("[red]That's not a sport![/]")
                    .DefaultValue("Sport?")
                    .AddChoice("Soccer")
                    .AddChoice("Hockey")
                    .AddChoice("Basketball"), cancellationToken);
        }

        public static int AskAge(CancellationToken cancellationToken)
        {
            return AnsiConsole.Prompt(
                new TextPrompt<int>("How [green]old[/] are you?")
                    .PromptStyle("green")
                    .ValidationErrorMessage("[red]That's not a valid age[/]")
                    .Validate(age =>
                    {
                        return age switch
                        {
                            <= 0 => ValidationResult.Error("[red]You must at least be 1 years old[/]"),
                            >= 123 => ValidationResult.Error("[red]You must be younger than the oldest person alive[/]"),
                            _ => ValidationResult.Success(),
                        };
                    }), cancellationToken);
        }

        public static string AskPassword(CancellationToken cancellationToken)
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>("Enter [green]password[/]?")
                    .PromptStyle("red")
                    .Secret(), cancellationToken);
        }

        public static string AskPasswordWithCustomMask(CancellationToken cancellationToken)
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>("Enter [green]password[/]?")
                    .PromptStyle("red")
                    .Secret('-'), cancellationToken);
        }

        public static string AskPasswordWithNullMask(CancellationToken cancellationToken)
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>("Enter [green]password[/]?")
                    .PromptStyle("red")
                    .Secret(null), cancellationToken);
        }

        public static string AskColor(CancellationToken cancellationToken)
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>("[grey][[Optional]][/] What is your [green]favorite color[/]?")
                    .AllowEmpty(), cancellationToken);
        }
    }

    public record Result(string? Name, string? Fruit, string? Sport, int? Age, string? Password, string? Mask, string? NullMask, string? Color);
}