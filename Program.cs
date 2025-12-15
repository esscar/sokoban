using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace sokoban
{
    /*
     * Коды клеток:
     * 0 - пусто
     * 1 - стена
     * 2 - ящик на пустом
     * 3 - цель
     * 4 - герой на пустом
     * 5 - ящик на цели
     * 6 - герой на цели
     */

    class Player
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Player(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    class Box
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Box(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    class LevelStats
    {
        public string PlayerName { get; set; }
        public int LevelNumber { get; set; }
        public int Steps { get; set; }
        public TimeSpan Time { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    class GameMap
    {
        private int[,] map;
        private int offsetY = 1;

        public int DrawOffsetY => offsetY;
        public Player Player { get; private set; }
        public int StepCount { get; private set; } = 0;
        public Stopwatch Timer { get; private set; }

        public int Height => map.GetLength(0);
        public int Width => map.GetLength(1);

        public GameMap(int[,] levelMap)
        {
            map = levelMap;
            InitHeroPosition();
            Timer = Stopwatch.StartNew();
        }
        
        public void DrawMap()
        {
            for (var y = 0; y < map.GetLength(0); y++)
            {
                for (var x = 0; x < map.GetLength(1); x++)
                {
                    DrawCell(x, y);
                }
            }
            UpdateStats();
        }
        
        public void UpdateStats()
        {
            var statsRow = Height + offsetY + 2;

            if (statsRow >= Console.BufferHeight)
                statsRow = Console.BufferHeight - 1;

            Console.SetCursorPosition(0, statsRow);
            Console.Write($"Шаги: {StepCount} | Время: {Timer.Elapsed:mm\\:ss}      ");
        }


        public bool IsWall(int x, int y) => map[y, x] == 1;
        public bool IsBox(int x, int y) => map[y, x] == 2 || map[y, x] == 5;
        public bool IsEmptyOrGoal(int x, int y) => map[y, x] == 0 || map[y, x] == 3;

        public void TryMoveHero(int dx, int dy)
        {
            var targetX = Player.X + dx;
            var targetY = Player.Y + dy;
            
            if (targetX < 0 || targetX >= Width || 
                targetY < 0 || targetY >= Height)
                return;

            if (IsWall(targetX, targetY)) return;

            if (!IsBox(targetX, targetY))
            {
                MoveHeroTo(targetX, targetY);
                StepCount++;
                UpdateStats();
                return;
            }

            var boxTargetX = targetX + dx;
            var boxTargetY = targetY + dy;
            
            if (boxTargetX < 0 || boxTargetX >= Width || 
                boxTargetY < 0 || boxTargetY >= Height)
                return;

            if (!IsEmptyOrGoal(boxTargetX, boxTargetY)) return;

            MoveBox(targetX, targetY, boxTargetX, boxTargetY);
            MoveHeroTo(targetX, targetY);
            StepCount++;
            UpdateStats();
        }
        
        public bool IsWin()
        {
            for (var y = 0; y < map.GetLength(0); y++)
            {
                for (var x = 0; x < map.GetLength(1); x++)
                {
                    if (map[y, x] == 3) 
                        return false;
                }
            }
            return true;
        }
        
        private void InitHeroPosition()
        {
            for (var y = 0; y < map.GetLength(0); y++)
            {
                for (var x = 0; x < map.GetLength(1); x++)
                {
                    if (map[y, x] == 4 || map[y, x] == 6)
                    {
                        Player = new Player(x, y);
                        return;
                    }
                }
            }
        }
        
        private void DrawCell(int x, int y)
        {
            var screenY = y + offsetY;

            if (screenY < 0 || screenY >= Console.BufferHeight ||
                x < 0 || x >= Console.BufferWidth)
                return;

            Console.CursorLeft = x;
            Console.CursorTop = screenY;

            var cell = map[y, x];

            if (cell == 1)
            {
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write((char)166);
                Console.BackgroundColor = ConsoleColor.Black;
                return;
            }

            switch (cell)
            {
                case 0: Console.Write(" "); break;
                case 2: Console.Write("0"); break;
                case 3: Console.Write("#"); break;
                case 4: Console.Write("X"); break;
                case 5: Console.Write("0"); break;
                case 6: Console.Write("X"); break;
                default: Console.Write(" "); break;
            }
        }
        
        private void MoveHeroTo(int newX, int newY)
        {
            var oldCell = map[Player.Y, Player.X];
            map[Player.Y, Player.X] = (oldCell == 6) ? 3 : 0;

            var nextCell = map[newY, newX];
            map[newY, newX] = (nextCell == 3) ? 6 : 4;

            DrawCell(Player.X, Player.Y);  
            Player.X = newX;
            Player.Y = newY;
            DrawCell(Player.X, Player.Y); 
        }

        private void MoveBox(int fromX, int fromY, int toX, int toY)
        {
            var fromCell = map[fromY, fromX];
            var toCell = map[toY, toX];

            map[fromY, fromX] = (fromCell == 5) ? 3 : 0;
            map[toY, toX] = (toCell == 3) ? 5 : 2;

            DrawCell(fromX, fromY); 
            DrawCell(toX, toY);
        }
    }

    class PlayerStats
    {
        public string Name { get; set; }
        public int LevelsCompleted { get; set; }
        public DateTime LastPlayed { get; set; }
    }

    class Game
    {
        private static List<PlayerStats> playerHistory = new List<PlayerStats>();
        private static List<LevelStats> levelStats = new List<LevelStats>();
        private static string currentPlayerName = "";

        private static int[,] LoadLevelFromFile(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException($"Файл уровня не найден: {filename}");
            }

            string[] lines = File.ReadAllLines(filename);
            if (lines.Length == 0)
            {
                throw new ArgumentException($"Файл уровня пуст: {filename}");
            }

            var height = lines.Length;
            var width = lines[0].Length;

            foreach (var line in lines)
            {
                if (line.Length != width)
                {
                    throw new ArgumentException($"Строки в файле уровня имеют несогласованную длину: {filename}");
                }
            }

            int[,] map = new int[height, width];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var c = lines[y][x];
                    if (char.IsDigit(c))
                    {
                        map[y, x] = c - '0'; // '0' -> 0, '1' -> 1, и т.д.
                    }
                    else
                    {
                        throw new ArgumentException($"Недопустимый символ '{c}' в файле уровня: {filename} около [{y}, {x}]");
                    }
                }
            }

            return map;
        }

        private static string[] GetLevelFileNames()
        {
            string[] files = Directory.GetFiles(".", "level*.txt");
            Array.Sort(files);
            return files;
        }

        static void LoadPlayerHistory()
        {
            var filename = "player_history.txt";
            if (File.Exists(filename))
            {
                string[] lines = File.ReadAllLines(filename);
                foreach (var line in lines)
                {
                    string[] parts = line.Split('|');
                    if (parts.Length >= 3)
                    {
                        if (DateTime.TryParse(parts[2], out DateTime lastPlayed))
                        {
                            playerHistory.Add(new PlayerStats
                            {
                                Name = parts[0],
                                LevelsCompleted = int.Parse(parts[1]),
                                LastPlayed = lastPlayed
                            });
                        }
                    }
                }
            }
        }

        static void LoadLevelStats()
        {
            var filename = "stats_level.txt";
            if (File.Exists(filename))
            {
                string[] lines = File.ReadAllLines(filename);
                foreach (string line in lines)
                {
                    string[] parts = line.Split('|');
                    if (parts.Length >= 5)
                    {
                        if (int.TryParse(parts[1], out int levelNumber) &&
                            int.TryParse(parts[2], out int steps) &&
                            TimeSpan.TryParse(parts[3], out TimeSpan time) &&
                            DateTime.TryParse(parts[4], out DateTime completedAt))
                        {
                            levelStats.Add(new LevelStats
                            {
                                PlayerName = parts[0],
                                LevelNumber = levelNumber,
                                Steps = steps,
                                Time = time,
                                CompletedAt = completedAt
                            });
                        }
                    }
                }
            }
        }

        static void SavePlayerHistory()
        {
            var filename = "player_history.txt";
            var lines = playerHistory.Select(p => $"{p.Name}|{p.LevelsCompleted}|{p.LastPlayed}");
            File.WriteAllLines(filename, lines);
        }

        static void SaveLevelStats()
        {
            string filename = "stats_level.txt";
            var lines = levelStats.Select(s => $"{s.PlayerName}|{s.LevelNumber}|{s.Steps}|{s.Time}|{s.CompletedAt}");
            File.WriteAllLines(filename, lines);
        }

        static void ShowMenu()
        {
            Console.Clear();
            Console.WriteLine("=== Sokoban ===");
            string[] levelFiles = GetLevelFileNames();
            if (levelFiles.Length == 0)
            {
                Console.WriteLine("Уровни не найдены! Пожалуйста, добавьте файлы уровней*.txt.");
                Console.WriteLine("2. Просмотр истории игроков");
                Console.WriteLine("3. Просмотр статистики уровней");
                Console.WriteLine("4. Выход");
                Console.Write("Выберите опцию: ");
            }
            else
            {
                Console.WriteLine("1. Новая игра");
                Console.WriteLine("2. Просмотр истории игроков");
                Console.WriteLine("3. Просмотр статистики уровней");
                Console.WriteLine("4. Выход");
                Console.Write("Выберите опцию: ");
            }
        }

        static void GetPlayerName()
        {
            Console.Write("\nВведите имя: ");
            currentPlayerName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(currentPlayerName))
            {
                currentPlayerName = "Анонимный";
            }
        }

        static void PlayLevel(int levelIndex, string levelFileName)
        {
            Console.Clear();
            try
            {
                int[,] levelMap = LoadLevelFromFile(levelFileName);
                GameMap map = new GameMap(levelMap);
                map.DrawMap();

                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    var dx = 0;
                    var dy = 0;

                    if (key.Key == ConsoleKey.LeftArrow) dx = -1;
                    else if (key.Key == ConsoleKey.RightArrow) dx = 1;
                    else if (key.Key == ConsoleKey.UpArrow) dy = -1;
                    else if (key.Key == ConsoleKey.DownArrow) dy = 1;
                    else if (key.Key == ConsoleKey.Escape) 
                    {
                        map.Timer.Stop();
                        return;
                    }
                    else continue;

                    map.TryMoveHero(dx, dy);

                    if (map.IsWin())
                    {
                        map.Timer.Stop();
                        RecordLevelCompletion(levelIndex, map);

                        var winRow = map.Height + map.DrawOffsetY + 4;
                        if (winRow >= Console.BufferHeight - 1)
                            winRow = Console.BufferHeight - 2;

                        Console.SetCursorPosition(0, winRow);
                        Console.WriteLine($"Уровень {levelIndex + 1} пройден! Шаги: {map.StepCount}, Время: {map.Timer.Elapsed:mm\\:ss}");
                        Console.WriteLine("Нажмите любую клавишу...");
                        Console.ReadKey();
                        return;
                    }


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки уровня {levelIndex + 1}: {ex.Message}");
                Console.WriteLine("Нажмите любую клавишу, чтобы продолжить...");
                Console.ReadKey();
            }
        }

        static void ViewPlayerHistory()
        {
            Console.Clear();
            Console.WriteLine("=== История игрока ===");
            if (playerHistory.Count == 0)
            {
                Console.WriteLine("История игрока не найдена.");
            }
            else
            {
                foreach (var player in playerHistory)
                {
                    Console.WriteLine($"Игрок: {player.Name}, Завершенные уровни: {player.LevelsCompleted}, Последняя игра: {player.LastPlayed:yyyy-MM-dd HH:mm}");
                }
            }
            Console.WriteLine("\nНажмите любую клавишу, чтобы продолжить...");
            Console.ReadKey();
        }

        static void ViewLevelStats()
        {
            Console.Clear();
            Console.WriteLine("=== Статистика уровней ===");
            if (levelStats.Count == 0)
            {
                Console.WriteLine("Статистика уровней не найдена.");
            }
            else
            {
                foreach (var stat in levelStats)
                {
                    Console.WriteLine($"Игрок: {stat.PlayerName}, Уровень: {stat.LevelNumber}, Шаги: {stat.Steps}, Время: {stat.Time:mm\\:ss}, Завершил: {stat.CompletedAt:yyyy-MM-dd HH:mm}");
                }
            }
            Console.WriteLine("\nНажмите любую клавишу, чтобы продолжить...");
            Console.ReadKey();
        }

        static void RecordPlayerCompletion(int levelsCompleted)
        {
            var existingPlayer = playerHistory.FirstOrDefault(p => p.Name.Equals(currentPlayerName, StringComparison.OrdinalIgnoreCase));
            
            if (existingPlayer != null)
            {
                existingPlayer.LevelsCompleted = Math.Max(existingPlayer.LevelsCompleted, levelsCompleted);
                existingPlayer.LastPlayed = DateTime.Now;
            }
            else
            {
                playerHistory.Add(new PlayerStats
                {
                    Name = currentPlayerName,
                    LevelsCompleted = levelsCompleted,
                    LastPlayed = DateTime.Now
                });
            }
            SavePlayerHistory();
        }

        static void RecordLevelCompletion(int levelIndex, GameMap map)
        {
            levelStats.Add(new LevelStats
            {
                PlayerName = currentPlayerName,
                LevelNumber = levelIndex + 1,
                Steps = map.StepCount,
                Time = map.Timer.Elapsed,
                CompletedAt = DateTime.Now
            });
            SaveLevelStats();
        }

        static void Main(string[] args)
        {
            Console.CursorVisible = false;

            int minHeight = 30; 
            if (Console.BufferWidth < 80) 
            {
                Console.SetBufferSize(80, Math.Max(Console.BufferHeight, minHeight));
            }
            if (Console.BufferHeight < minHeight)
            {
                Console.SetBufferSize(Console.BufferWidth, minHeight);
            }

            LoadPlayerHistory();
            LoadLevelStats();

            while (true)
            {
                ShowMenu();
                var input = Console.ReadLine();

                if (input == "1")
                {
                    string[] levelFiles = GetLevelFileNames();
                    if (levelFiles.Length == 0)
                    {
                        Console.WriteLine("Не найдено уровней для игры! Пожалуйста, добавьте файлы уровней*.txt");
                        Console.WriteLine("Нажмите любую клавишу...");
                        Console.ReadKey();
                        continue;
                    }

                    GetPlayerName();
                    var completedLevels = 0;
                    
                    for (var i = 0; i < levelFiles.Length; i++)
                    {
                        PlayLevel(i, levelFiles[i]);
                        completedLevels++;
                        
                        if (i < levelFiles.Length - 1)
                        {
                            Console.Clear();
                            Console.WriteLine($"Пройденный уровень {i + 1}. Начало следующего уровня...");
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                    
                    RecordPlayerCompletion(completedLevels);
                    Console.WriteLine("Игра завершена! Нажмите любую клавишу...");
                    Console.ReadKey();
                }
                else if (input == "2")
                {
                    ViewPlayerHistory();
                }
                else if (input == "3")
                {
                    ViewLevelStats(); }
                else if (input == "4")
                {
                    break;
                }
            }
        }
    }
}