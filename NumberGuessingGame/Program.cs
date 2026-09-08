using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;

namespace NumberGuessingGame
{
    class Program
    {
        // Database connection string - creates a file called "leaderboard.db"
        private static readonly string ConnectionString = "Data Source=leaderboard.db";

        static void Main(string[] args)
        {
            Console.Title = "Number PICKING GAME";
            Console.ForegroundColor = ConsoleColor.Cyan;

            // Create database and table if it doesn't exist
            InitializeDatabase();

            bool playAgain = true;
            int totalGamesPlayed = 0;
            int totalAttempts = 0;

            while (playAgain)
            {
                Console.Clear();
                DisplayHeader();

                // Game configuration
                int minNumber = 1;
                int maxNumber = 100;
                int maxAttempts = 10; // Maximum attempts allowed
                int secretNumber = Random.Shared.Next(minNumber, maxNumber + 1);
                int attempts = 0;
                bool isGuessed = false;
                bool gameOver = false;

                Console.WriteLine($"I'm thinking of a number between {minNumber} and {maxNumber}.");
                Console.WriteLine($"You have {maxAttempts} attempts to guess it!");
                Console.WriteLine();

                // Main guessing loop
                while (!isGuessed && !gameOver)
                {
                    int guess = GetPlayerGuess();
                    attempts++;

                    if (guess < secretNumber)
                    {
                        Console.WriteLine($" Too low! Try again. ({maxAttempts - attempts} attempts left)");
                    }
                    else if (guess > secretNumber)
                    {
                        Console.WriteLine($" Too high! Try again. ({maxAttempts - attempts} attempts left)");
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"   Congratulations! You guessed it in {attempts} attempts!");
                        Console.ResetColor();
                        isGuessed = true;
                    }

                    // Check if player ran out of attempts
                    if (!isGuessed && attempts >= maxAttempts)
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"   GAME OVER! You ran out of attempts!");
                        Console.WriteLine($"   The number was: {secretNumber}");
                        Console.ResetColor();
                        gameOver = true;
                    }
                }

                // Update statistics (only count if they guessed it)
                if (isGuessed)
                {
                    totalGamesPlayed++;
                    totalAttempts += attempts;

                    // Save to leaderboard
                    SaveToLeaderboard(attempts);

                    // Display statistics
                    DisplayStats(attempts, totalGamesPlayed, totalAttempts);
                }
                else
                {
                    // Lost the game
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n   Better luck next time!");
                    Console.ResetColor();
                    totalGamesPlayed++; // Still count the game
                }

                // Show leaderboard
                DisplayLeaderboard();

                // Ask to play again
                playAgain = AskPlayAgain();
            }

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    Thanks for playing!");
            Console.WriteLine($"   You played {totalGamesPlayed} games with {totalAttempts} total attempts.");
            if (totalGamesPlayed > 0)
            {
                Console.WriteLine($"   Average attempts per game: {(double)totalAttempts / totalGamesPlayed:F2}");
            }
            Console.ResetColor();
            Console.WriteLine("\nKindly Press any key to exit...");
            Console.ReadKey();
        }

        //DATABASE METHODS

        private static void InitializeDatabase()
        {
            try
            {
                // Check if database exists
                bool dbExists = File.Exists("leaderboard.db");

                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                // Create table if it doesn't exist
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Leaderboard (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PlayerName TEXT NOT NULL,
                        Attempts INTEGER NOT NULL,
                        DatePlayed TEXT NOT NULL,
                        Difficulty TEXT NOT NULL
                    )";
                command.ExecuteNonQuery();

                if (!dbExists)
                {
                    Console.WriteLine("Leaderboard database created successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating database: {ex.Message}");
            }
        }

        private static void SaveToLeaderboard(int attempts)
        {
            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Leaderboard (PlayerName, Attempts, DatePlayed, Difficulty)
                    VALUES (@PlayerName, @Attempts, @DatePlayed, @Difficulty)";

                // Get player name
                Console.Write("\nEnter your name for the leaderboard: ");
                string? playerName = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(playerName))
                {
                    playerName = "Anonymous";
                }

                command.Parameters.AddWithValue("@PlayerName", playerName);
                command.Parameters.AddWithValue("@Attempts", attempts);
                command.Parameters.AddWithValue("@DatePlayed", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@Difficulty", "Medium (1-100)");

                command.ExecuteNonQuery();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Score saved to leaderboard");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving score: {ex.Message}");
            }
        }

        private static void DisplayLeaderboard()
        {
            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT PlayerName, Attempts, DatePlayed 
                    FROM Leaderboard 
                    ORDER BY Attempts ASC 
                    LIMIT 10";

                using var reader = command.ExecuteReader();

                if (!reader.HasRows)
                {
                    Console.WriteLine("\nNo scores yet! Be the first to play!");
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("\n   LEADERBOARD - TOP 10");
                Console.WriteLine("   -----------------------------");
                Console.ResetColor();
                Console.WriteLine($"   {"Rank",-6} {"Player",-20} {"Attempts",-10} {"Date",-20}");
                Console.WriteLine("   -----------------------------------------------------");

                int rank = 1;
                while (reader.Read())
                {
                    string playerName = reader.GetString(0);
                    int attempts = reader.GetInt32(1);
                    string datePlayed = reader.GetString(2);

                    // Format date to show only date part
                    if (DateTime.TryParse(datePlayed, out DateTime date))
                    {
                        datePlayed = date.ToString("MMM dd, yyyy");
                    }

                    // Highlight the best score
                    if (rank == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"   {rank,-6} {playerName,-20} {attempts,-10}  {datePlayed,-20}");
                        Console.ResetColor();
                    }
                    else if (rank == 2)
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine($"   {rank,-6} {playerName,-20} {attempts,-10}  {datePlayed,-20}");
                        Console.ResetColor();
                    }
                    else if (rank == 3)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"   {rank,-6} {playerName,-20} {attempts,-10}  {datePlayed,-20}");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"   {rank,-6} {playerName,-20} {attempts,-10}    {datePlayed,-20}");
                    }
                    rank++;
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading leaderboard: {ex.Message}");
            }
        }

        //GAME METHODS

        private static void DisplayHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"   ----------------------------------------");
            Console.WriteLine(@"        NUMBER GUESSING GAME");
            Console.WriteLine(@"   ----------------------------------------");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static int GetPlayerGuess()
        {
            while (true)
            {
                Console.Write("   Enter your guess: ");
                string? input = Console.ReadLine();

                // Input validation
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("   Please enter a number");
                    continue;
                }

                if (int.TryParse(input, out int guess))
                {
                    if (guess >= 1 && guess <= 100)
                    {
                        return guess;
                    }
                    else
                    {
                        Console.WriteLine("   Please enter a number between 1 and 100");
                    }
                }
                else
                {
                    Console.WriteLine("   Invalid input! Please enter a valid number.");
                }
            }
        }

        private static void DisplayStats(int attempts, int totalGames, int totalAttempts)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"    Your Stats:");
            Console.WriteLine($"   -----------------");
            Console.WriteLine($"   * Attempts this round: {attempts}");
            Console.WriteLine($"   * Games played: {totalGames}");
            Console.WriteLine($"   * Total attempts: {totalAttempts}");
            Console.WriteLine($"   *Average attempts: {(double)totalAttempts / totalGames:F2}");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static bool AskPlayAgain()
        {
            while (true)
            {
                Console.Write("   Would you like to play again?");
                string? input = Console.ReadLine()?.ToLower().Trim();

                if (input == "y" || input == "yes")
                {
                    return true;
                }
                else if (input == "n" || input == "no")
                {
                    return false;
                }
                else
                {
                    Console.WriteLine("   Please enter 'y' for yes or 'n' for no.");
                }
            }
        }
    }
}