using System;

namespace BerlinClock
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Berlin Clock (Mengenlehre-Uhr) Simulator ===\n");

            while (true)
            {
                Console.Write("Enter time (HH:MM:SS) or 'exit' to quit: ");
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "exit")
                    break;

                if (TryParseTime(input, out int hours, out int minutes, out int seconds))
                {
                    DisplayBerlinClock(hours, minutes, seconds);
                }
                else
                {
                    Console.WriteLine("Invalid time format. Please use HH:MM:SS (e.g., 13:45:30)\n");
                }
            }

            Console.WriteLine("\nGoodbye!");
        }

        static bool TryParseTime(string input, out int hours, out int minutes, out int seconds)
        {
            hours = minutes = seconds = 0;
            string[] parts = input.Split(':');

            if (parts.Length != 3)
                return false;

            if (!int.TryParse(parts[0], out hours) || hours < 0 || hours > 23)
                return false;

            if (!int.TryParse(parts[1], out minutes) || minutes < 0 || minutes > 59)
                return false;

            if (!int.TryParse(parts[2], out seconds) || seconds < 0 || seconds > 59)
                return false;

            return true;
        }

        static void DisplayBerlinClock(int hours, int minutes, int seconds)
        {
            Console.WriteLine($"\n--- Time: {hours:D2}:{minutes:D2}:{seconds:D2} ---\n");

            // Top lamp (seconds indicator)
            DisplaySecondsLamp(seconds);
            Console.WriteLine();

            // First row: 5-hour blocks (4 red lamps)
            DisplayFiveHourRow(hours);
            Console.WriteLine();

            // Second row: 1-hour blocks (4 red lamps)
            DisplayOneHourRow(hours);
            Console.WriteLine();

            // Third row: 5-minute blocks (11 lamps, every 3rd is red for quarter hours)
            DisplayFiveMinuteRow(minutes);
            Console.WriteLine();

            // Fourth row: 1-minute blocks (4 yellow lamps)
            DisplayOneMinuteRow(minutes);
            Console.WriteLine();
        }

        static void DisplaySecondsLamp(int seconds)
        {
            Console.Write("Seconds: ");
            if (seconds % 2 == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("● ");
                Console.ResetColor();
                Console.WriteLine("(Y - ON)");
            }
            else
            {
                Console.Write("○ ");
                Console.WriteLine("(O - OFF)");
            }
        }

        static void DisplayFiveHourRow(int hours)
        {
            Console.Write("5-Hours: ");
            int litLamps = hours / 5;

            for (int i = 0; i < 4; i++)
            {
                if (i < litLamps)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("● ");
                    Console.ResetColor();
                }
                else
                {
                    Console.Write("○ ");
                }
            }

            Console.Write($"({new string('R', litLamps)}{new string('O', 4 - litLamps)})");
            Console.WriteLine();
        }

        static void DisplayOneHourRow(int hours)
        {
            Console.Write("1-Hours: ");
            int litLamps = hours % 5;

            for (int i = 0; i < 4; i++)
            {
                if (i < litLamps)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("● ");
                    Console.ResetColor();
                }
                else
                {
                    Console.Write("○ ");
                }
            }

            Console.Write($"({new string('R', litLamps)}{new string('O', 4 - litLamps)})");
            Console.WriteLine();
        }

        static void DisplayFiveMinuteRow(int minutes)
        {
            Console.Write("5-Mins:  ");
            int litLamps = minutes / 5;

            for (int i = 0; i < 11; i++)
            {
                if (i < litLamps)
                {
                    // Every 3rd lamp (positions 2, 5, 8) is red for quarter hours
                    if ((i + 1) % 3 == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("● ");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("● ");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.Write("○ ");
                }
            }

            Console.Write("(");
            for (int i = 0; i < 11; i++)
            {
                if (i < litLamps)
                {
                    Console.Write((i + 1) % 3 == 0 ? "R" : "Y");
                }
                else
                {
                    Console.Write("O");
                }
            }
            Console.Write(")");
            Console.WriteLine();
        }

        static void DisplayOneMinuteRow(int minutes)
        {
            Console.Write("1-Mins:  ");
            int litLamps = minutes % 5;

            for (int i = 0; i < 4; i++)
            {
                if (i < litLamps)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("● ");
                    Console.ResetColor();
                }
                else
                {
                    Console.Write("○ ");
                }
            }

            Console.Write($"({new string('Y', litLamps)}{new string('O', 4 - litLamps)})");
            Console.WriteLine();
        }
    }
}
