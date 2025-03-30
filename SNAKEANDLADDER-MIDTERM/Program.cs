using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;

internal class SnakeLadderGame
{
    private static readonly Random random = new();
    private static readonly Dictionary<int, int> snakes = new()
    {
        { 16, 6 }, { 47, 26 }, { 49, 11 }, { 56, 53 }, { 62, 19 }, { 64, 60 }, { 87, 24 }, { 93, 73 }, { 95, 75 }, { 98, 78 }
    };

    private static readonly Dictionary<int, int> ladders = new()
    {
        { 1, 38 }, { 4, 14 }, { 9, 31 }, { 21, 42 }, { 28, 84 }, { 36, 44 }, { 51, 67 }, { 71, 91 }, { 80, 100 }
    };

    private static readonly HashSet<int> skillTiles = new() { 10, 20, 30, 40, 50, 60, 70, 85, 90 };
    private static readonly string[] availableSkills = { "Shield 🛡️", "Stun ⚡", "Swap 🔄", "Dice Manipulation 🎲", "Anchor ⚓", "Sabotage 💣" };

    private static int? lastRoll = null;
    private static string lastAction = "";
    private static string lastSkillUsed = "";
    private static int currentPlayerIndex = 0;
    private static bool gameEnded = false;

    private class Player
    {
        public string Name { get; }
        public int Position { get; set; }
        public List<string> Skills { get; } = new();
        public bool SkipTurn { get; set; } = false;
        public string Color { get; }
        public bool HasWon { get; set; } = false;

        public Player(string name, string color)
        {
            Name = name;
            Position = 0;
            Color = color;
        }
    }

    private static List<Player> players = new();

    public static void StartGame()
    {
        bool playAgain;
        do
        {
            Console.Clear();
            players.Clear();
            ResetGameState();

            int numberOfPlayers;

            while (true)
            {
                Console.Write("Enter number of players (2 to 4) or press any other key to return to the Main Menu: ");
                var key = Console.ReadKey(true);

                if (key.KeyChar >= '2' && key.KeyChar <= '4')
                {
                    numberOfPlayers = key.KeyChar - '0';
                    Console.WriteLine($"\nYou selected {numberOfPlayers} players.");
                    break;
                }
                else
                {
                    Console.WriteLine("\nReturning to the Main Menu...");
                    return;
                }
            }

            string[] colors = { "red", "green", "blue", "yellow" };
            for (int i = 0; i < numberOfPlayers; i++)
            {
                Console.Write($"Enter Player {i + 1} Name: ");
                string name = Console.ReadLine();
                while (string.IsNullOrWhiteSpace(name) || players.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("Name cannot be empty or duplicate. Please enter again:");
                    name = Console.ReadLine();
                }
                players.Add(new Player(name, colors[i]));
            }

            currentPlayerIndex = 0;
            gameEnded = false;
            playAgain = false;

            while (!gameEnded)
            {
                Player currentPlayer = players[currentPlayerIndex];
                if (currentPlayer.HasWon)
                {
                    currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
                    continue;
                }

                DisplayBoard();

                if (currentPlayer.SkipTurn)
                {
                    lastAction = $"[red]{currentPlayer.Name} is stunned ⚡ and skips this turn![/]";
                    currentPlayer.SkipTurn = false;
                    DisplayBoard();
                    Console.ReadKey();
                }
                else
                {
                    int roll = RollDice();
                    lastRoll = roll;
                    DisplayBoard();

                    if (currentPlayer.Skills.Count > 0 && AnsiConsole.Confirm($"[{currentPlayer.Color}]{currentPlayer.Name}[/], do you want to use a skill?"))
                    {
                        UseSkill(currentPlayer, players);
                        DisplayBoard();
                    }

                    MovePlayer(currentPlayer, roll);
                    CheckSkillTile(currentPlayer);

                    if (currentPlayer.Position >= 100)
                    {
                        currentPlayer.HasWon = true;
                        DisplayBoard();
                        AnsiConsole.MarkupLine($"\n[bold green]🎉 Congratulations {currentPlayer.Name}! You win the game! 🏆[/]");

                        if (players.Count(p => !p.HasWon) == 0 || AnsiConsole.Confirm("\nDo you want to play again?"))
                        {
                            playAgain = true;
                            break;
                        }
                        else
                        {
                            gameEnded = true;
                        }
                    }
                }

                currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
                Console.ReadKey();
            }
        } while (playAgain);
    }

    private static void DisplayBoard()
    {
        var table = new Table();
        table.Border = TableBorder.Heavy;
        table.ShowRowSeparators();
        table.HideHeaders();

        for (int col = 0; col < 10; col++)
            table.AddColumn(new TableColumn((col + 1).ToString()).Centered();

        for (int row = 9; row >= 0; row--)
        {
            var rowCells = new List<string>();
            for (int col = 0; col < 10; col++)
            {
                int cellNumber = (row % 2 == 0) ? (row * 10 + col + 1) : (row * 10 + (9 - col) + 1);
                string cellContent = cellNumber.ToString("D2");

                if (snakes.ContainsKey(cellNumber))
                    cellContent = $"[red]🐍 {cellNumber}[/]";
                else if (ladders.ContainsKey(cellNumber))
                    cellContent = $"[green]🪜 {cellNumber}[/]";
                else if (skillTiles.Contains(cellNumber))
                    cellContent = $"[blue]✨ {cellNumber}[/]";

                var occupyingPlayers = players.Where(p => p.Position == cellNumber && !p.HasWon).ToList();
                if (occupyingPlayers.Count > 0)
                {
                    cellContent = string.Join("", occupyingPlayers.Select(p => $"[{p.Color}]{(p.Position == 100 ? "🏁" : p.Name[0].ToString())}[/]"));
                }
                else if (cellNumber == 100)
                {
                    cellContent = "🏁";
                }

                rowCells.Add(cellContent);
            }
            table.AddRow(rowCells.ToArray());
        }

        var rightPanel = new Panel(
            new Rows(
                new Text($"[bold]Current Turn:[/] [{players[currentPlayerIndex].Color}]▶ {players[currentPlayerIndex].Name}[/]"),
                new Text(""),
                lastRoll.HasValue ? new Text($"[bold]Last Roll:[/] 🎲 {lastRoll}") : new Text("[bold]Last Roll:[/] 🎲 -"),
                new Text(""),
                !string.IsNullOrEmpty(lastSkillUsed) ? new Text($"[bold]Last Skill Used:[/] {lastSkillUsed}") : new Text("[bold]Last Skill Used:[/] -"),
                new Text(""),
                !string.IsNullOrEmpty(lastAction) ? new Markup($"[bold]Last Action:[/] {lastAction}") : new Text("[bold]Last Action:[/] -")
            ))
            .Border(BoxBorder.Rounded)
            .Header("[bold]Game Info[/]");

        var statusTable = new Table().Border(TableBorder.Simple);
        statusTable.AddColumns(
            new TableColumn("Player").Width(15),
            new TableColumn("Position").Width(8).Centered(),
            new TableColumn("Skills").Width(20),
            new TableColumn("Status").Width(12).Centered());

        foreach (var player in players.OrderBy(p => p.HasWon))
        {
            string status = player.HasWon ? "[green]🏆 WINNER![/]" :
                          player.SkipTurn ? "[red]⚡ STUNNED[/]" : "[yellow]✅ ACTIVE[/]";

            statusTable.AddRow(
                $"[{player.Color}]{player.Name}[/]",
                $"[bold]{(player.Position == 100 ? "🏁" : player.Position.ToString())}[/]",
                player.Skills.Count > 0 ? string.Join(", ", player.Skills) : "None",
                status);
        }

        statusTable.AddEmptyRow();
        statusTable.AddRow("[bold cyan]Legend[/]", "", "", "");
        statusTable.AddRow("[green]🪜 Ladder[/]", "", "", "");
        statusTable.AddRow("[red]🐍 Snake[/]", "", "", "");
        statusTable.AddRow("[blue]✨ Skill Tile[/]", "", "", "");

        var rightPanelContent = new Rows(rightPanel, statusTable);

        var grid = new Grid();
        grid.AddColumn().AddColumn();
        grid.AddRow(table, rightPanelContent);

        AnsiConsole.Clear();
        AnsiConsole.Write(grid);
    }

    private static int RollDice()
    {
        AnsiConsole.MarkupLine("[yellow]Press any key to roll the dice... 🎲[/]");
        Console.ReadKey();
        int roll = random.Next(1, 7);
        lastAction = $"[bold white on blue]{players[currentPlayerIndex].Name} rolled a {roll}! 🎲[/]";
        return roll;
    }

    private static void MovePlayer(Player player, int roll)
    {
        int newPosition = player.Position + roll;
        if (newPosition > 100)
        {
            lastAction = $"[yellow]{player.Name} stays at {player.Position} (Roll exceeds 100) ❌[/]";
            return;
        }

        player.Position = newPosition;
        lastAction = $"[bold white]{player.Name} moves to {newPosition} ➡️[/]";

        if (snakes.ContainsKey(newPosition))
        {
            if (player.Skills.Contains("Shield 🛡️"))
            {
                player.Skills.Remove("Shield 🛡️");
                lastAction = $"[bold green]{player.Name} used 🛡️ Shield to resist the snake![/]";
            }
            else if (player.Skills.Contains("Anchor ⚓"))
            {
                player.Skills.Remove("Anchor ⚓");
                lastAction = $"[bold green]{player.Name} used ⚓ Anchor to resist the snake![/]";
            }
            else
            {
                player.Position = snakes[newPosition];
                lastAction = $"[red]🐍 {player.Name} got bitten! Moves down to {snakes[newPosition]} ⬇️[/]";
            }
        }
        else if (ladders.ContainsKey(newPosition))
        {
            player.Position = ladders[newPosition];
            lastAction = $"[green]🪜 {player.Name} climbed a ladder! Moves up to {ladders[newPosition]} ⬆️[/]";
        }
    }

    private static void UseSkill(Player player, List<Player> players)
    {
        var skillChoices = new List<string>();
        for (int i = 0; i < player.Skills.Count; i++)
        {
            skillChoices.Add($"{i + 1}. {player.Skills[i]}");
        }
        skillChoices.Add("❌ Cancel");

        var skillSelection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[{player.Color}]{player.Name}'s Skills:[/]")
                .AddChoices(skillChoices));

        if (skillSelection == "❌ Cancel") return;

        int skillIndex = int.Parse(skillSelection.Split('.')[0]) - 1;
        string skill = player.Skills[skillIndex];
        player.Skills.RemoveAt(skillIndex);
        lastSkillUsed = $"[{player.Color}]{player.Name}[/] used [bold]{skill}[/]";
        lastAction = $"[bold white on red]{player.Name} used {skill}! 💥[/]";

        if (skill == "Stun ⚡" || skill == "Swap 🔄" || skill == "Sabotage 💣")
        {
            var targetPlayers = players.Where(p => p != player && !p.HasWon).ToList();
            if (targetPlayers.Count == 0)
            {
                lastAction = $"[red]No valid players to target! ❌[/]";
                return;
            }

            var targetSelection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select target player:")
                    .AddChoices(targetPlayers.Select(p => p.Name)));

            var targetPlayer = targetPlayers.First(p => p.Name == targetSelection);
            ExecuteSkill(skill, player, targetPlayer);
        }
        else if (skill == "Dice Manipulation 🎲")
        {
            var rollChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose your dice roll:")
                    .AddChoices(new[] { "1", "2", "3", "4", "5", "6" }));

            int chosenRoll = int.Parse(rollChoice);
            lastAction = $"[bold white on red]{player.Name} chose to roll a {chosenRoll}! 🎲[/]";
            MovePlayer(player, chosenRoll);
        }
    }

    private static void ExecuteSkill(string skill, Player player, Player targetPlayer)
    {
        if (skill == "Stun ⚡")
        {
            if (!targetPlayer.Skills.Contains("Shield 🛡️"))
            {
                targetPlayer.SkipTurn = true;
                lastAction += $"\n[red]{targetPlayer.Name} is stunned ⚡ and will skip next turn![/]";
            }
            else
            {
                targetPlayer.Skills.Remove("Shield 🛡️");
                lastAction += $"\n[green]{targetPlayer.Name} blocked the stun with 🛡️ Shield![/]";
            }
        }
        else if (skill == "Swap 🔄")
        {
            (player.Position, targetPlayer.Position) = (targetPlayer.Position, player.Position);
            lastAction += $"\n[yellow]{player.Name} swapped positions 🔄 with {targetPlayer.Name}![/]";
        }
        else if (skill == "Sabotage 💣")
        {
            if (!targetPlayer.Skills.Contains("Shield 🛡️"))
            {
                int sabotageRoll = random.Next(1, 7);
                targetPlayer.Position = Math.Max(0, targetPlayer.Position - sabotageRoll);
                lastAction += $"\n[red]{targetPlayer.Name} was sabotaged 💣 and moved back {sabotageRoll} spaces! ⬇️[/]";
            }
            else
            {
                targetPlayer.Skills.Remove("Shield 🛡️");
                lastAction += $"\n[green]{targetPlayer.Name} blocked the sabotage with 🛡️ Shield![/]";
            }
        }
    }

    private static void CheckSkillTile(Player player)
    {
        if (skillTiles.Contains(player.Position))
        {
            if (player.Skills.Count >= 2)
            {
                lastAction = $"[blue]{player.Name} cannot acquire more skills. Maximum skill limit reached! ⚠️[/]";
                return;
            }

            string newSkill;
            do
            {
                newSkill = availableSkills[random.Next(availableSkills.Length)];
            } while (player.Skills.Contains(newSkill));

            player.Skills.Add(newSkill);
            lastAction = $"[blue]{player.Name} acquired {newSkill}! ✨[/]";
            lastSkillUsed = $"[{player.Color}]{player.Name}[/] got [bold]{newSkill}[/]";
        }
    }

    private static void ResetGameState()
    {
        lastRoll = null;
        lastAction = "";
        lastSkillUsed = "";
        currentPlayerIndex = 0;
        gameEnded = false;
    }
}

internal class Program
{
    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8; // Enable UTF-8 encoding for emoji support

        while (true)
        {
            Console.Clear();
            AnsiConsole.Write(new Panel("[bold cyan]🐍🎲 Welcome to Snake and Ladder! 🎲🪜[/]").BorderColor(Color.Green));

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Main Menu")
                    .AddChoices(new[] { "🎮 Play Game", "📖 How to Play", "👨‍💻 Developers", "🚪 Exit" }));

            switch (choice)
            {
                case "🎮 Play Game":
                    SnakeLadderGame.StartGame();
                    break;

                case "📖 How to Play":
                    ShowInstructions();
                    break;

                case "👨‍💻 Developers":
                    ShowCredits();
                    break;

                case "🚪 Exit":
                    Console.WriteLine("Goodbye! Thanks for playing. 🎉");
                    return;
            }
        }
    }

    private static void ShowInstructions()
    {
        AnsiConsole.Write(new Panel(new Rows(
            new Text("📜 HOW TO PLAY:"),
            new Text("- 🎲 Roll dice to move forward"),
            new Text("- 🏁 First player to reach 100 wins"),
            new Text(""),
            new Text("🌈 SPECIAL TILES:"),
            new Text("- [green]🪜 Ladders[/]: Move up to higher number"),
            new Text("- [red]🐍 Snakes[/]: Move down to lower number"),
            new Text("- [blue]✨ Skill Tiles[/]: Gain special abilities"),
            new Text(""),
            new Text("🛠️ SKILLS (Max 2 per player):"),
            new Text("- [blue]🛡️ Shield[/]: Block snakes and some skills"),
            new Text("- [blue]⚡ Stun[/]: Make opponent skip next turn"),
            new Text("- [blue]🔄 Swap[/]: Exchange positions with another player"),
            new Text("- [blue]🎲 Dice Manipulation[/]: Choose your next roll"),
            new Text("- [blue]⚓ Anchor[/]: Resist snake bites"),
            new Text("- [blue]💣 Sabotage[/]: Push another player back"),
            new Text(""),
            new Text("👥 PLAYER STATUS:"),
            new Text("- Shows current position of all players"),
            new Text("- Displays active skills for each player"),
            new Text("- Indicates who is stunned or has won"),
            new Text("- Updates in real-time during gameplay")
        )).Header("📚 Instructions").BorderColor(Color.Yellow));

        Console.WriteLine("\nPress any key to return to the Main Menu...");
        Console.ReadKey(true);
    }

    private static void ShowCredits()
    {
        AnsiConsole.Write(new Panel(new Rows(
            new Text("👨‍💻 DEVELOPERS:"),
            new Text(""),
            new Text("🎨 Game Design: Allen Paul Belarmino"),
            new Text("💻 Programming: Allen Paul Belarmino"),
            new Text(""),
            new Text("© 2023")
        )).Header("🏆 Credits").BorderColor(Color.Green));

        Console.WriteLine("\nPress any key to return to the Main Menu...");
        Console.ReadKey(true);
    }
}