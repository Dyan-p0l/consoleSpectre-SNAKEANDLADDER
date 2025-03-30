using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;

public class SnakeLadderGame
{
    private static readonly Random _random = new();
    private static readonly Dictionary<int, int> _snakes = new()
    {
        {16, 6}, {47, 26}, {49, 11}, {56, 53}, {62, 19}, {64, 60}, {87, 24}, {93, 73}, {95, 75}, {98, 78}
    };

    private static readonly Dictionary<int, int> _ladders = new()
    {
        {1, 38}, {4, 14}, {9, 31}, {21, 42}, {28, 84}, {36, 44}, {51, 67}, {71, 91}, {80, 100}
    };

    private static readonly HashSet<int> _skillTiles = new() { 10, 20, 30, 40, 50, 60, 70, 85, 90 };
    private static readonly string[] _availableSkills = { "Shield", "Stun", "Swap", "Dice", "Anchor", "Sabotage" };
    private static List<Player> _players = new();

    private static int? _lastRoll;
    private static string? _lastSkill;
    private static string? _actionMessage;
    private static string? _currentPlayerName;

    public class Player
    {
        public string Name { get; }
        public int Position { get; set; }
        public List<string> Skills { get; } = new();
        public bool SkipTurn { get; set; }
        public string Color { get; }
        public const int MaxSkills = 2;

        public Player(string name, string color)
        {
            Name = name;
            Position = 0;
            Color = color;
        }
    }

    public static void StartGame()
    {
        InitializeGame();
        GameLoop();
    }

    private static void InitializeGame()
    {
        AnsiConsole.Clear();
        _players.Clear();
        ResetGameState();

        var numberOfPlayers = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Number of players?")
                .AddChoices(new[] { "2 Players", "3 Players", "4 Players", "Back" }));

        if (numberOfPlayers == "Back") return;

        for (int i = 0; i < int.Parse(numberOfPlayers[0].ToString()); i++)
        {
            _players.Add(new Player(
                AnsiConsole.Ask<string>($"Enter Player {i + 1} Name:"),
                new[] { "blue", "green", "purple", "yellow" }[i]
            ));
        }
    }

    private static void GameLoop()
    {
        int currentPlayerIndex = 0;
        while (true)
        {
            var player = _players[currentPlayerIndex];
            _currentPlayerName = $"[{player.Color}]{player.Name}[/]'s Turn";

            if (!player.SkipTurn)
            {
                UpdateDisplay(player);
                if (player.Skills.Count > 0 && AnsiConsole.Confirm("Use a skill?"))
                {
                    UseSkill(player);
                    UpdateDisplay(player);
                }

                RollAndMove(player);
                CheckSkillTile(player);
            }
            else
            {
                _actionMessage = $"{player.Name} skips turn (stunned)";
                player.SkipTurn = false;
                UpdateDisplay(player);
                WaitForInput();
            }

            if (player.Position >= 100)
            {
                UpdateDisplay(player);
                AnsiConsole.MarkupLine($"[bold green]⭐ {player.Name} WINS! ⭐[/]");
                break;
            }

            currentPlayerIndex = (currentPlayerIndex + 1) % _players.Count;
        }
        AnsiConsole.MarkupLine("[yellow]Game over! Press any key...[/]");
        Console.ReadKey();
    }

    private static void UpdateDisplay(Player? player = null)
    {
        AnsiConsole.Clear();

        // Create the main layout
        var layout = new Layout("Root")
            .SplitColumns(
                new Layout("Board").Size(60),
                new Layout("RightPanel").Size(30));

        // Create the game board
        var board = new Grid()
            .Width(60)
            .Collapse();

        // Add 10 columns
        for (int i = 0; i < 10; i++)
        {
            board.AddColumn(new GridColumn().Width(6));
        }

        // Build the board from top to bottom (100 at top)
        for (int row = 9; row >= 0; row--)
        {
            var cells = new List<string>();
            for (int col = 0; col < 10; col++)
            {
                int cellNum = row % 2 == 0 ? row * 10 + col + 1 : (row + 1) * 10 - col;
                string cell = cellNum.ToString();

                if (_snakes.ContainsKey(cellNum)) cell = $"[red]S{cellNum}[/]";
                else if (_ladders.ContainsKey(cellNum)) cell = $"[green]L{cellNum}[/]";
                else if (_skillTiles.Contains(cellNum)) cell = $"[blue]*{cellNum}[/]";

                var playersHere = _players.Where(p => p.Position == cellNum).ToList();
                if (playersHere.Any())
                {
                    cell += "\n" + string.Join("", playersHere.Select(p => $"[{p.Color}]{p.Name[0]}[/]"));
                }

                cells.Add(cell);
            }
            board.AddRow(cells.ToArray());
        }

        // Create the right panel content
        var rightPanelContent = new Rows(
            new Panel(new Markup(_currentPlayerName ?? ""))
                .Border(BoxBorder.None),
            new Panel(CreateStatusTable())
                .Border(BoxBorder.None),
            new Panel(
                _lastRoll.HasValue
                    ? new Markup($"[bold yellow]Rolled: {_lastRoll}[/]")
                    : new Markup("[bold yellow]Roll...[/]"))
                .Border(BoxBorder.Rounded),
            new Panel(
                !string.IsNullOrEmpty(_lastSkill)
                    ? new Markup($"[bold blue]New Skill: {_lastSkill}[/]")
                    : new Text(""))
                .Border(BoxBorder.Rounded),
            new Panel(
                !string.IsNullOrEmpty(_actionMessage)
                    ? new Markup($"[bold green]{_actionMessage}[/]")
                    : new Text(""))
                .Border(BoxBorder.Rounded),
            new Panel(new Markup("[bold yellow]Press any key...[/]"))
                .Border(BoxBorder.None));

        // Update the layout
        layout["Board"].Update(
            new Panel(board)
                .Header(" SNAKES & LADDERS ")
                .Border(BoxBorder.Rounded));

        layout["RightPanel"].Update(
            new Panel(rightPanelContent)
                .Border(BoxBorder.Rounded));

        AnsiConsole.Write(layout);
    }

    private static Table CreateStatusTable()
    {
        var table = new Table()
            .Border(TableBorder.Simple)
            .AddColumns(
                new TableColumn("Player").Width(15),
                new TableColumn("Pos").Width(5).Centered(),
                new TableColumn("Skills").Width(10));

        foreach (var p in _players)
        {
            table.AddRow(
                new Markup($"[{p.Color}]{p.Name.Elipsis(12)}[/]"),
                new Text(p.Position.ToString()),
                new Text(string.Join(",", p.Skills.Take(2))));
        }

        return table;
    }

    private static void RollAndMove(Player player)
    {
        _lastRoll = null;
        _lastSkill = null;
        _actionMessage = null;
        UpdateDisplay(player);
        WaitForInput();

        _lastRoll = _random.Next(1, 7);
        _actionMessage = $"{player.Name} rolled {_lastRoll}";
        UpdateDisplay(player);
        WaitForInput();

        MovePlayer(player, _lastRoll.Value);
    }

    private static void MovePlayer(Player player, int roll)
    {
        int newPos = player.Position + roll;
        if (newPos > 100)
        {
            _actionMessage = $"{player.Name} stays at {player.Position}";
            return;
        }

        player.Position = newPos;
        _actionMessage = $"{player.Name} moves to {newPos}";

        if (_snakes.TryGetValue(newPos, out int snakeTail))
        {
            if (player.Skills.Contains("Shield"))
            {
                player.Skills.Remove("Shield");
                _actionMessage = $"{player.Name} blocked snake with Shield!";
            }
            else
            {
                player.Position = snakeTail;
                _actionMessage = $"[red]{player.Name} got bitten! Fell to {snakeTail}[/]";
            }
        }
        else if (_ladders.TryGetValue(newPos, out int ladderTop))
        {
            player.Position = ladderTop;
            _actionMessage = $"[green]{player.Name} climbed to {ladderTop}![/]";
        }
    }

    private static void UseSkill(Player player)
    {
        var choices = new List<string>(player.Skills);
        choices.Add("Cancel");

        var skill = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"{player.Name}'s skills:")
                .AddChoices(choices));

        if (skill == "Cancel") return;

        player.Skills.Remove(skill);
        _actionMessage = $"{player.Name} used {skill}!";

        switch (skill)
        {
            case "Stun":
                var target = SelectTarget(player, "stun");
                if (target != null)
                {
                    target.SkipTurn = true;
                    _actionMessage += $"\n{target.Name} will skip next turn!";
                }
                break;

            case "Swap":
                target = SelectTarget(player, "swap with");
                if (target != null)
                {
                    (player.Position, target.Position) = (target.Position, player.Position);
                    _actionMessage += $"\nSwapped with {target.Name}!";
                }
                break;

            case "Dice":
                int value = AnsiConsole.Ask<int>("Enter dice value (1-6):", 3);
                MovePlayer(player, Math.Clamp(value, 1, 6));
                break;

            case "Sabotage":
                target = SelectTarget(player, "sabotage");
                if (target != null)
                {
                    int sabotage = _random.Next(1, 7);
                    target.Position = Math.Max(0, target.Position - sabotage);
                    _actionMessage += $"\n{target.Name} moved back {sabotage}!";
                }
                break;
        }
    }

    private static void CheckSkillTile(Player player)
    {
        if (!_skillTiles.Contains(player.Position)) return;

        if (player.Skills.Count >= Player.MaxSkills)
        {
            var choices = new List<string>(player.Skills);
            choices.Add("Keep skills");

            var toReplace = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Replace a skill?")
                    .AddChoices(choices));

            if (toReplace != "Keep skills")
            {
                player.Skills.Remove(toReplace);
                _lastSkill = _availableSkills[_random.Next(_availableSkills.Length)];
                player.Skills.Add(_lastSkill);
                _actionMessage = $"Learned {_lastSkill}!";
            }
        }
        else
        {
            _lastSkill = _availableSkills[_random.Next(_availableSkills.Length)];
            player.Skills.Add(_lastSkill);
            _actionMessage = $"Learned {_lastSkill}!";
        }
    }

    private static Player? SelectTarget(Player current, string action)
    {
        var targets = _players.Where(p => p != current).ToList();
        if (targets.Count == 0) return null;

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"Choose target to {action}:")
                .AddChoices(targets.Select(p => p.Name)));

        return targets.First(p => p.Name == choice);
    }

    private static void WaitForInput()
    {
        Console.ReadKey(true);
    }

    private static void ResetGameState()
    {
        _lastRoll = null;
        _lastSkill = null;
        _actionMessage = null;
        _currentPlayerName = null;
    }
}

public static class Extensions
{
    public static string Elipsis(this string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
    }
}

public class Program
{
    public static void Main()
    {
        AnsiConsole.Write(
            new Panel(
                new FigletText("Snakes & Ladders")
                    .Centered()
                    .Color(Color.Yellow))
                .Border(BoxBorder.None));

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Main Menu")
                    .AddChoices(new[] { "Play Game", "How to Play", "Credits", "Exit" }));

            switch (choice)
            {
                case "Play Game":
                    SnakeLadderGame.StartGame();
                    break;
                case "How to Play":
                    ShowInstructions();
                    break;
                case "Credits":
                    ShowCredits();
                    break;
                case "Exit":
                    return;
            }
        }
    }

    private static void ShowInstructions()
    {
        AnsiConsole.Write(new Panel(new Rows(
            new Text("HOW TO PLAY:"),
            new Text("- Roll dice to move"),
            new Text("- [green]Ladders (▲)[/] move up"),
            new Text("- [red]Snakes (■)[/] move down"),
            new Text("- [blue]Skill tiles (*)[/] give abilities"),
            new Text(""),
            new Text("SKILLS:"),
            new Text("- Shield: Block snakes"),
            new Text("- Stun: Skip opponent's turn"),
            new Text("- Swap: Trade positions"),
            new Text("- Dice: Choose your roll"),
            new Text("- Sabotage: Push opponent back")
        )).Header("Instructions"));

        WaitForInput();
    }

    private static void ShowCredits()
    {
        AnsiConsole.Write(new Panel(new Rows(
            new Text("CREDITS"),
            new Text(""),
            new Text("Game Design: Your Name"),
            new Text("Programming: Your Name"),
            new Text(""),
            new Text("© 2023")
        )).Header("About"));

        WaitForInput();
    }

    private static void WaitForInput()
    {
        AnsiConsole.MarkupLine("[yellow]Press any key...[/]");
        Console.ReadKey();
    }
}