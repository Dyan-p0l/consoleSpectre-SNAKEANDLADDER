using System;
using System.Collections.Generic;
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
    private static readonly string[] availableSkills = { "Shield", "Stun", "Swap", "Dice Manipulation", "Sabotage" };

    private class Player
    {
        public string Name { get; }
        public int Position { get; set; }
        public List<string> Skills { get; } = new();
        public bool SkipTurn { get; set; } = false;

        public Player(string name)
        {
            Name = name;
            Position = 0;
        }
    }

    private static List<Player> players = new();

    public static void StartGame()
    {
        Console.Clear();

        // Reset game state
        players.Clear();

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

        for (int i = 1; i <= numberOfPlayers; i++)
        {
            Console.Write($"Enter Player {i} Name: ");
            players.Add(new Player(Console.ReadLine()));
        }

        int currentPlayerIndex = 0;
        while (true)
        {
            DisplayBoard();
            Player currentPlayer = players[currentPlayerIndex];

            if (!currentPlayer.SkipTurn)
            {
                UseSkill(currentPlayer, players);
                int roll = RollDice();
                MovePlayer(currentPlayer, roll);
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]{currentPlayer.Name} is stunned and skips this turn![/]");
                currentPlayer.SkipTurn = false;
            }

            CheckSkillTile(currentPlayer);

            if (currentPlayer.Position == 100)
            {
                AnsiConsole.MarkupLine($"\n[bold green]🎉 Congratulations {currentPlayer.Name}! You win the game! 🎉[/]");
                Console.WriteLine("\nPress any key to return to the Main Menu...");
                Console.ReadKey();
                return; // Go back to Main Menu
            }

            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            Console.ReadKey();
        }
    }


    private static void DisplayBoard()
    {
        var table = new Table();
        table.Border = TableBorder.Heavy;
        table.ShowRowSeparators();
        table.HideHeaders();

        // Adjust column width for better visibility
        for (int col = 0; col < 10; col++)
            table.AddColumn(new TableColumn((col + 1).ToString()).Centered());

        for (int row = 9; row >= 0; row--)
        {
            var rowCells = new List<string>();
            for (int col = 0; col < 10; col++)
            {
                int cellNumber = (row % 2 == 0) ? (row * 10 + col + 1) : (row * 10 + (9 - col) + 1);
                string cellContent = cellNumber.ToString("D2");

                if (snakes.ContainsKey(cellNumber))
                    cellContent = $"[red]S{cellNumber}[/]";
                else if (ladders.ContainsKey(cellNumber))
                    cellContent = $"[green]L{cellNumber}[/]";
                else if (skillTiles.Contains(cellNumber))
                    cellContent = $"[blue]*{cellNumber}[/]";

                var occupyingPlayers = players.FindAll(p => p.Position == cellNumber);
                if (occupyingPlayers.Count > 0)
                {
                    cellContent = $"[bold]P{string.Join("+P", occupyingPlayers.ConvertAll(p => (players.IndexOf(p) + 1).ToString()))}[/]";
                }

                rowCells.Add(cellContent);
            }
            table.AddRow(rowCells.ToArray());
        }

        // Player status box
        var statusTable = new Table().Border(TableBorder.Rounded);
        statusTable.AddColumn("Player");
        statusTable.AddColumn("Position");

        foreach (var player in players)
        {
            statusTable.AddRow($"[bold yellow]{player.Name}[/]", $"[bold yellow]{player.Position}[/]");
        }

        statusTable.AddEmptyRow();

        statusTable.AddRow("[bold cyan]Legend[/]", "");
        statusTable.AddRow("[green]🟩 Ladder[/]", "");
        statusTable.AddRow("[red]🟥 Snake[/]");
        statusTable.AddRow("[blue]🟦 Skill Tile[/]", "");

        statusTable.AddEmptyRow();



        // Combine board and status using a Grid
        var grid = new Grid();
        grid.AddColumn().AddColumn();
        grid.AddRow(table, statusTable);


        AnsiConsole.Clear();
        AnsiConsole.Write(grid);
    }

    private static int RollDice()
    {
        Console.WriteLine("Press Enter to roll the dice...");
        Console.ReadKey();
        int roll = random.Next(1, 7);
        AnsiConsole.MarkupLine($"[bold white on blue]  Rolled a {roll}![/]");

        return roll;
    }

    private static void MovePlayer(Player player, int roll)
    {
        int newPosition = player.Position + roll;
        if (newPosition > 100)
        {
            AnsiConsole.MarkupLine($"[yellow]{player.Name} stays at {player.Position} (Roll exceeds 100)[/]");
            return;
        }

        if (snakes.ContainsKey(newPosition))
        {
            if (player.Skills.Contains("Anchor"))
            {
                player.Skills.Remove("Anchor");
                AnsiConsole.MarkupLine($"[bold green]{player.Name} used Anchor to resist the snake![/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]🐍 {player.Name} got bitten! Moves down to {snakes[newPosition]}[/]");
                player.Position = snakes[newPosition];
                return;
            }
        }
        if (ladders.ContainsKey(newPosition))
        {
            AnsiConsole.MarkupLine($"[green]🪜 {player.Name} climbed a ladder! Moves up to {ladders[newPosition]}[/]");
            player.Position = ladders[newPosition];
            return;
        }

        AnsiConsole.MarkupLine($"[bold white]{player.Name} moves to {newPosition}[/]");
        player.Position = newPosition;
    }

    private static void UseSkill(Player player, List<Player> players)
    {
        if (player.Skills.Count == 0)
            return;

        AnsiConsole.MarkupLine($"[bold magenta]{player.Name}'s Skills:[/]");
        for (int i = 0; i < player.Skills.Count; i++)
        {
            AnsiConsole.MarkupLine($"[yellow]{i + 1}. {player.Skills[i]}[/]");
        }

        Console.Write("Enter skill number to use or press Enter to skip: ");
        string input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input))
        {
            // Player pressed Enter without using any skill
            return;
        }

        if (!int.TryParse(input, out int skillIndex) || skillIndex < 1 || skillIndex > player.Skills.Count)
        {
            AnsiConsole.MarkupLine("[red]Invalid choice. Skill use canceled.[/]");
            return;
        }

        string skill = player.Skills[skillIndex - 1];

        // Handle skills that need a target
        if (skill == "Stun" || skill == "Swap" || skill == "Sabotage")
        {
            var targetPlayers = players.FindAll(p => p != player);
            if (targetPlayers.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No other players to target. Skill use canceled.[/]");
                return;
            }

            AnsiConsole.MarkupLine("[bold]Choose a player to target (or press Enter to cancel):[/]");
            for (int i = 0; i < targetPlayers.Count; i++)
            {
                AnsiConsole.MarkupLine($"[yellow]{i + 1}. {targetPlayers[i].Name}[/]");
            }

            Console.Write("Enter player number: ");
            string targetInput = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(targetInput))
            {
                AnsiConsole.MarkupLine("[red]Skill use canceled.[/]");
                return;
            }

            if (!int.TryParse(targetInput, out int playerChoice) || playerChoice < 1 || playerChoice > targetPlayers.Count)
            {
                AnsiConsole.MarkupLine("[red]Invalid choice. Skill use canceled.[/]");
                return;
            }

            Player targetPlayer = targetPlayers[playerChoice - 1];

            // Execute the selected skill
            ExecuteSkill(skill, player, targetPlayer);
        }
        else if (skill == "Dice Manipulation")
        {
            Console.Write("Choose dice roll (1-6) or press Enter to cancel: ");
            string diceInput = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(diceInput))
            {
                AnsiConsole.MarkupLine("[red]Skill use canceled.[/]");
                return;
            }

            if (int.TryParse(diceInput, out int chosenRoll) && chosenRoll >= 1 && chosenRoll <= 6)
            {
                player.Skills.RemoveAt(skillIndex - 1);
                MovePlayer(player, chosenRoll);
                AnsiConsole.MarkupLine($"[bold white on red]{player.Name} used {skill} and chose to roll a {chosenRoll}![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Invalid roll. Skill use canceled.[/]");
            }
        }
        else
        {
            player.Skills.RemoveAt(skillIndex - 1);
            AnsiConsole.MarkupLine($"[bold white on red]{player.Name} used {skill}![/]");
        }
    }

    private static void ExecuteSkill(string skill, Player player, Player targetPlayer)
    {
        if (skill == "Stun")
        {
            if (!targetPlayer.Skills.Contains("Shield"))
            {
                targetPlayer.SkipTurn = true;
                AnsiConsole.MarkupLine($"[red]{targetPlayer.Name} is stunned and will skip the next turn![/]");
            }
        }
        else if (skill == "Swap")
        {
            (player.Position, targetPlayer.Position) = (targetPlayer.Position, player.Position);
            AnsiConsole.MarkupLine($"[yellow]{player.Name} swapped positions with {targetPlayer.Name}![/]");
        }
        else if (skill == "Sabotage")
        {
            if (!targetPlayer.Skills.Contains("Shield"))
            {
                int sabotageRoll = random.Next(1, 7);
                targetPlayer.Position = Math.Max(0, targetPlayer.Position - sabotageRoll);
                AnsiConsole.MarkupLine($"[red]{targetPlayer.Name} was sabotaged and moved back {sabotageRoll} spaces![/]");
            }
        }

        player.Skills.Remove(skill);
    }



    private static void CheckSkillTile(Player player)
    {
        if (skillTiles.Contains(player.Position))
        {
            // Check if player already has 2 skills
            if (player.Skills.Count >= 2)
            {
                AnsiConsole.MarkupLine($"[blue]{player.Name} cannot acquire more skills. Maximum skill limit reached![/]");
                return;
            }

            // Randomly select a new skill
            string newSkill;
            do
            {
                newSkill = availableSkills[random.Next(availableSkills.Length)];
            } while (player.Skills.Contains(newSkill)); // Ensure no duplicates

            // Add the skill
            player.Skills.Add(newSkill);
            AnsiConsole.MarkupLine($"[blue]{player.Name} acquired a skill[/]!");
        }
    }


}

internal class Program
{
    private static void Main()
    {
        while (true)
        {
            Console.Clear();
            AnsiConsole.Write(new Panel("[bold cyan]🐍🎲 Welcome to Snake and Ladder! 🎲🪜[/]").BorderColor(Color.Green));

            AnsiConsole.MarkupLine("[yellow]1. Play Game[/]");
            AnsiConsole.MarkupLine("[yellow]2. How to Play[/]");
            AnsiConsole.MarkupLine("[yellow]3. Developers[/]");
            AnsiConsole.MarkupLine("[yellow]4. Exit[/]");

            var key = Console.ReadKey(intercept: true).Key;
            Console.Clear();

            switch (key)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    SnakeLadderGame.StartGame();
                    break;

                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    Console.WriteLine("📝 This is the How to Play section. (Add detailed instructions here)");
                    Console.WriteLine("\nPress any key to return to the Main Menu...");
                    Console.ReadKey(true);
                    break;

                case ConsoleKey.D3:
                case ConsoleKey.NumPad3:
                    Console.WriteLine("👩‍💻 This game was developed by... (Add developer details here)");
                    Console.WriteLine("\nPress any key to return to the Main Menu...");
                    Console.ReadKey(true);
                    break;

                case ConsoleKey.D4:
                case ConsoleKey.NumPad4:
                    Console.WriteLine("Goodbye! Thanks for playing. 🎉");
                    return;

                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    Thread.Sleep(1000);
                    break;
            }
        }
    }
}


