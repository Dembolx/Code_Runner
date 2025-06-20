﻿using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;

namespace CodeRunner_Maui.Klasy
{
    public enum CellType
    {
        Path,
        Wall
    }

    public class Plansza : IDrawable
    {
        public int Rows { get; private set; }
        public int Cols { get; private set; }
        private int CellSize { get; set; }
        private Player Player { get; set; }
        public int EnemiesCount => _enemies.Count;


        // Tablica przechowująca typ każdej komórki (ściana lub ścieżka)
        private CellType[,] Grid { get; set; }

        // Dodane offsety kamery
        public int CameraOffsetX { get; set; } = 0;
        public int CameraOffsetY { get; set; } = 0;

        // Rozmiary widocznego obszaru (viewport)
        private int _viewportWidth = 800;  // Domyślna wartość
        private int _viewportHeight = 600; // Domyślna wartość

        // Współczynnik szerokości ścieżki (ile komórek zajmuje jedna ścieżka)
        private int PathWidth { get; set; } = 1;

        private Random Random { get; set; }


        private Shoot _shootSystem;

        public Plansza(int rows, int cols, int cellSize, int pathWidth = 1)
        {
            Rows = rows;
            Cols = cols;
            CellSize = cellSize;
            PathWidth = pathWidth;
            Grid = new CellType[rows, cols];
            Random = new Random();

            // Initialize shooting system
            _shootSystem = new Shoot(this);

            // Generowanie planszy
            GenerateMaze();
        }

        public void SpawnEnemies(int maxEnemies)
        {
            _enemies.Clear();
            int enemyCount = Random.Next(3, maxEnemies + 2);

            for (int i = 0; i < 2; i++)
            {
                var position = FindRandomPathPosition();
                if (!position.HasValue) continue;

                Enemy enemy;
                int enemyType = Random.Next(1, 100);

                if (enemyType <= 0) // 40% chance for basic enemy
                {
                    enemy = new BasicEnemy(this, position.Value.X, position.Value.Y, Player);
                }
                else if (enemyType <= 1) // 30% chance for shooting woman
                {
                    // Pass the shooting system to the ShootingWomanEnemy
                    enemy = new ShootingWomanEnemy(this, position.Value.X, position.Value.Y, Player, _shootSystem);
                }
                else // 30% chance for chasing dog
                {
                    enemy = new ChasingDogEnemy(this, position.Value.X, position.Value.Y, Player);
                }

                _enemies.Add(enemy);
            }
        }

        public void UpdateAllEnemies()
        {
            foreach (var enemy in _enemies.ToList())
            {
                enemy.Update();
            }
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // ... existing drawing code ...

            // Draw maze, player, enemies as before
            if (_viewportWidth != (int)dirtyRect.Width || _viewportHeight != (int)dirtyRect.Height)
            {
                SetViewportSize((int)dirtyRect.Width, (int)dirtyRect.Height);
                if (Player != null)
                {
                    UpdateCameraPosition();
                }
            }

            DrawBackground(canvas, dirtyRect);
            DrawMaze(canvas, dirtyRect);
            DrawEnemies(canvas);
            DrawPlayer(canvas);

            // Draw bullets on top of everything
            _shootSystem.Draw(canvas, CameraOffsetX, CameraOffsetY);
        }

        // Add this method to allow player shooting (optional)
        public void PlayerShoot(Direction direction)
        {
            if (Player != null)
            {
                _shootSystem.ShootBullet(Player.X, Player.Y, direction, false, 5.0f);
            }
        }

        // Ustawienie wymiarów widocznego obszaru (viewport)
        public void SetViewportSize(int width, int height)
        {
            _viewportWidth = width;
            _viewportHeight = height;
        }

        public void SetPlayer(Player player)
        {
            Player = player;
            PlacePlayerOnPath();
            UpdateCameraPosition();
            SpawnEnemies(3); // Dodaj 3 przeciwników
        }

        private (float X, float Y)? FindRandomPathPosition()
        {
            List<(int row, int col)> pathCells = new List<(int row, int col)>();

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    if (Grid[row, col] == CellType.Path)
                    {
                        pathCells.Add((row, col));
                    }
                }
            }

            if (pathCells.Count == 0) return null;

            var cell = pathCells[Random.Next(pathCells.Count)];
            float x = cell.col * CellSize + (CellSize - 32) / 2;
            float y = cell.row * CellSize + (CellSize - 32) / 2;

            return (x, y);
        }
        public bool IsPath(float x, float y)
        {
            // Konwersja współrzędnych świata na indeksy siatki
            int col = (int)Math.Floor(x / CellSize);
            int row = (int)Math.Floor(y / CellSize);

            // Sprawdzenie czy pozycja jest w granicach planszy
            if (col < 0 || col >= Cols || row < 0 || row >= Rows)
                return false;

            return Grid[row, col] == CellType.Path;
        }

        // Sprawdza czy gracz może przesunąć się na daną pozycję
        public bool CanPlayerMoveTo(float newX, float newY)
        {
            // Sprawdź cztery rogi gracza po przesunięciu
            return IsPath(newX, newY) &&
                   IsPath(newX + Player.Width - 1, newY) &&
                   IsPath(newX, newY + Player.Height - 1) &&
                   IsPath(newX + Player.Width - 1, newY + Player.Height - 1);
        }

        // Metoda aktualizująca pozycję gracza z walidacją kolizji ze ścianami
        public void UpdatePlayerPosition(float newX, float newY)
        {
            // Sprawdź czy nowa pozycja jest dozwolona
            if (CanPlayerMoveTo(newX, newY))
            {
                Player.X = newX;
                Player.Y = newY;

                // Aktualizuj pozycję kamery, aby gracz był na środku
                UpdateCameraPosition();
            }
        }

        // Aktualizacja pozycji kamery, aby śledzić gracza
        private void UpdateCameraPosition()
        {
            // Oblicz pozycję kamery tak, aby gracz był na środku ekranu
            // Zakładamy, że znamy wymiary widocznego obszaru (ViewportWidth, ViewportHeight)
            if (Player != null && _viewportWidth > 0 && _viewportHeight > 0)
            {
                // Docelowa pozycja kamery - gracz na środku ekranu
                int targetOffsetX = (int)(Player.X + Player.Width / 2 - _viewportWidth / 2);
                int targetOffsetY = (int)(Player.Y + Player.Height / 2 - _viewportHeight / 2);

                // Ogranicz kamerę do granic planszy
                targetOffsetX = Math.Max(0, Math.Min(targetOffsetX, Cols * CellSize - _viewportWidth));
                targetOffsetY = Math.Max(0, Math.Min(targetOffsetY, Rows * CellSize - _viewportHeight));

                // Zastosuj płynne przesunięcie kamery (interpolacja)
                CameraOffsetX = (int)Math.Round(CameraOffsetX * 0.9 + targetOffsetX * 0.1);
                CameraOffsetY = (int)Math.Round(CameraOffsetY * 0.9 + targetOffsetY * 0.1);
            }
        }

        // Metoda znajdująca najbliższą pozycję na ścieżce dla gracza
        private void PlacePlayerOnPath()
        {
            // Zaczynamy od środka planszy i szukamy najbliższej ścieżki
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    if (Grid[row, col] == CellType.Path)
                    {
                        // Znaleziono ścieżkę, ustaw gracza na środku komórki
                        Player.X = col * CellSize + (CellSize - Player.Width) / 2;
                        Player.Y = row * CellSize + (CellSize - Player.Height) / 2;
                        return;
                    }
                }
            }
        }

        // Algorytm generowania labiryntu
        private void GenerateMaze()
        {
            // Najpierw wypełnij wszystko ścianami
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    Grid[row, col] = CellType.Wall;
                }
            }

            // Implementacja algorytmu rekurencyjnego podziału (recursive division)
            GeneratePathsRecursive(1, 1, Rows - 2, Cols - 2);

            // Poszerzenie ścieżek o zadaną szerokość
            WidenPaths();

            // Dodaj losowe przejścia, aby labirynt był mniej liniowy
            AddRandomPaths(Rows * Cols / 20); // około 5% dodatkowych ścieżek
        }

        // Rekurencyjny algorytm podziału do generowania ścieżek
        private void GeneratePathsRecursive(int startRow, int startCol, int endRow, int endCol)
        {
            int width = endCol - startCol + 1;
            int height = endRow - startRow + 1;

            // Warunek zakończenia rekurencji - obszar jest za mały
            if (width < 3 || height < 3)
                return;

            // Wybierz losowy punkt podziału
            int divRow = startRow + Random.Next(1, height - 1);
            int divCol = startCol + Random.Next(1, width - 1);

            // Utwórz ścieżki krzyżowe
            // Pozioma ścieżka
            for (int col = startCol; col <= endCol; col++)
            {
                Grid[divRow, col] = CellType.Path;
            }

            // Pionowa ścieżka
            for (int row = startRow; row <= endRow; row++)
            {
                Grid[row, divCol] = CellType.Path;
            }

            // Rekurencyjne wywołanie dla czterech powstałych kwadrantów
            GeneratePathsRecursive(startRow, startCol, divRow - 1, divCol - 1);     // Lewy górny
            GeneratePathsRecursive(startRow, divCol + 1, divRow - 1, endCol);       // Prawy górny
            GeneratePathsRecursive(divRow + 1, startCol, endRow, divCol - 1);       // Lewy dolny
            GeneratePathsRecursive(divRow + 1, divCol + 1, endRow, endCol);         // Prawy dolny
        }

        // Metoda poszerzająca ścieżki
        private void WidenPaths()
        {
            if (PathWidth <= 1) return; // Brak potrzeby poszerzania

            // Tworzymy tymczasową kopię siatki
            CellType[,] tempGrid = new CellType[Rows, Cols];
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    tempGrid[row, col] = Grid[row, col];
                }
            }

            // Poszerzanie ścieżek
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    if (Grid[row, col] == CellType.Path)
                    {
                        // Poszerz ścieżkę we wszystkich kierunkach
                        for (int dr = -PathWidth / 2; dr <= PathWidth / 2; dr++)
                        {
                            for (int dc = -PathWidth / 2; dc <= PathWidth / 2; dc++)
                            {
                                int newRow = row + dr;
                                int newCol = col + dc;

                                // Sprawdź czy nowa pozycja jest w granicach
                                if (newRow >= 0 && newRow < Rows && newCol >= 0 && newCol < Cols)
                                {
                                    tempGrid[newRow, newCol] = CellType.Path;
                                }
                            }
                        }
                    }
                }
            }

            // Zastąp oryginalną siatkę
            Grid = tempGrid;
        }

        // Dodawanie losowych przejść dla urozmaicenia labiryntu
        private void AddRandomPaths(int count)
        {
            for (int i = 0; i < count; i++)
            {
                int row = Random.Next(1, Rows - 1);
                int col = Random.Next(1, Cols - 1);

                // Sprawdź czy sąsiednie komórki są ścieżkami
                bool hasPathNeighbor = false;
                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;

                        int newRow = row + dr;
                        int newCol = col + dc;

                        if (newRow >= 0 && newRow < Rows && newCol >= 0 && newCol < Cols &&
                            Grid[newRow, newCol] == CellType.Path)
                        {
                            hasPathNeighbor = true;
                            break;
                        }
                    }
                    if (hasPathNeighbor) break;
                }

                // Dodaj ścieżkę tylko jeśli ma sąsiada będącego ścieżką
                if (hasPathNeighbor)
                {
                    Grid[row, col] = CellType.Path;
                }
            }
        }

        private void DrawEnemies(ICanvas canvas)
        {
            foreach (var enemy in _enemies)
            {
                if (enemy.IsActive)
                    enemy.Draw(canvas, CameraOffsetX, CameraOffsetY);
            }
        }

        private void DrawBackground(ICanvas canvas, RectF dirtyRect)
        {
            // Kolor tła
            canvas.FillColor = Colors.LightGray;
            canvas.FillRectangle(0, 0, dirtyRect.Width, dirtyRect.Height);
        }

        private void DrawMaze(ICanvas canvas, RectF dirtyRect)
        {
            // Określenie widocznego obszaru siatki z uwzględnieniem kamery
            int startCol = Math.Max(0, CameraOffsetX / CellSize);
            int startRow = Math.Max(0, CameraOffsetY / CellSize);

            int endCol = Math.Min(Cols - 1, startCol + (int)(dirtyRect.Width / CellSize) + 2);
            int endRow = Math.Min(Rows - 1, startRow + (int)(dirtyRect.Height / CellSize) + 2);

            // Rysowanie komórek labiryntu
            for (int row = startRow; row <= endRow; row++)
            {
                for (int col = startCol; col <= endCol; col++)
                {
                    float x = col * CellSize - CameraOffsetX;
                    float y = row * CellSize - CameraOffsetY;

                    if (Grid[row, col] == CellType.Wall)
                    {
                        // Rysuj ścianę
                        canvas.FillColor = Colors.DarkGray;
                        canvas.FillRectangle(x, y, CellSize, CellSize);

                        // Dodaj teksturę lub efekt ściany
                        canvas.StrokeColor = Colors.Black;
                        canvas.StrokeSize = 1;
                        canvas.DrawRectangle(x, y, CellSize, CellSize);
                    }
                    else
                    {
                        // Rysuj ścieżkę
                        canvas.FillColor = Colors.White;
                        canvas.FillRectangle(x, y, CellSize, CellSize);
                    }
                }
            }

            // Dla celów debugowania - oznaczenie środka ekranu
            canvas.StrokeColor = Colors.Red;
            float centerX = dirtyRect.Width / 2;
            float centerY = dirtyRect.Height / 2;
            canvas.DrawLine(centerX - 10, centerY, centerX + 10, centerY);
            canvas.DrawLine(centerX, centerY - 10, centerX, centerY + 10);
        }

        private void DrawPlayer(ICanvas canvas)
        {
            if (Player == null) return;

            float adjustedX = Player.X - CameraOffsetX;
            float adjustedY = Player.Y - CameraOffsetY;

            // Efekt nietykalności - miganie
            if (!Player.IsInvulnerable || DateTime.Now.Millisecond % 200 < 100)
            {
                canvas.FillColor = Colors.Blue;
                canvas.FillRectangle(adjustedX, adjustedY, Player.Width, Player.Height);
            }

            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2;
            canvas.DrawRectangle(adjustedX, adjustedY, Player.Width, Player.Height);

            DrawHealthBar(canvas, adjustedX, adjustedY);
        }

        public void DrawHealthBar(ICanvas canvas, float playerX, float playerY)
        {
            if (Player == null) return;

            // Konfiguracja paska życia
            float healthBarWidth = Player.Width;
            float healthBarHeight = 6;
            float healthBarOffsetY = 8;

            // Pozycja paska życia
            float healthBarX = playerX;
            float healthBarY = playerY - healthBarOffsetY - healthBarHeight;

            // Tło paska życia (czerwone)
            canvas.FillColor = Colors.DarkRed;
            canvas.FillRectangle(healthBarX, healthBarY, healthBarWidth, healthBarHeight);

            // Obramowanie
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 1;
            canvas.DrawRectangle(healthBarX, healthBarY, healthBarWidth, healthBarHeight);

            // Aktualne życie
            if (Player.Lives > 0)
            {
                float healthPercentage = (float)Player.Lives / Player.MaxLives;
                float currentHealthWidth = healthPercentage * healthBarWidth;

                // Zmiana koloru w zależności od ilości życia
                canvas.FillColor = healthPercentage > 0.6f ? Colors.Green :
                                  healthPercentage > 0.3f ? Colors.Orange :
                                  Colors.Red;

                canvas.FillRectangle(healthBarX, healthBarY, currentHealthWidth, healthBarHeight);
            }

            // Tekst z ilością życia
            canvas.FontColor = Colors.White;
            canvas.FontSize = 10;
            canvas.DrawString($"{Player.Lives}/{Player.MaxLives}",
                             healthBarX + healthBarWidth / 2,
                             healthBarY - 2,
                             HorizontalAlignment.Center);
        }

        private List<Enemy> _enemies = new List<Enemy>();

        // Dodaj tę metodę do sprawdzania ruchu dla przeciwników
        public bool CanMoveTo(float x, float y, int width, int height)
        {
            return IsPath(x, y) &&
                   IsPath(x + width - 1, y) &&
                   IsPath(x, y + height - 1) &&
                   IsPath(x + width - 1, y + height - 1);
        }

        public event Action EnemiesChanged;

        public void RemoveEnemy(Enemy enemy)
        {
            _enemies.Remove(enemy);
            EnemiesChanged?.Invoke();
        }

    }
}