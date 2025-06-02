using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeRunner_Maui.Klasy
{
    public enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    public enum EnemyType
    {
        Basic,
        ShootingWoman,
        ChasingDog
    }

    public struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public float DistanceTo(Point other)
        {
            return (float)Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));
        }
    }

    public abstract class Enemy
    {
        public float X { get; set; }
        public float Y { get; set; }
        public int Width { get; protected set; } = 32;
        public int Height { get; protected set; } = 32;
        public float Speed { get; protected set; } = 1.5f;
        public Direction CurrentDirection { get; protected set; }
        public EnemyType Type { get; protected set; }
        public bool IsActive { get; set; } = true;

        protected readonly Plansza _plansza;
        protected readonly Random _random;
        protected int _directionChangeCooldown = 0;
        protected Player _targetPlayer;

        public Enemy(Plansza plansza, float startX, float startY, Player targetPlayer = null)
        {
            _plansza = plansza;
            _random = new Random();
            X = startX;
            Y = startY;
            _targetPlayer = targetPlayer;
            CurrentDirection = GetRandomDirection();
        }

        public virtual void Update()
        {
            if (!IsActive) return;

            if (_directionChangeCooldown > 0)
            {
                _directionChangeCooldown--;
            }
            else if (!CanMoveInCurrentDirection() || _random.Next(100) < 5)
            {
                CurrentDirection = GetRandomDirection();
                _directionChangeCooldown = 30;
            }

            Move();
        }

        protected virtual bool CanMoveInCurrentDirection()
        {
            float newX = X;
            float newY = Y;

            switch (CurrentDirection)
            {
                case Direction.Up: newY -= Speed; break;
                case Direction.Down: newY += Speed; break;
                case Direction.Left: newX -= Speed; break;
                case Direction.Right: newX += Speed; break;
            }

            return _plansza.CanMoveTo(newX, newY, Width, Height);
        }

        protected virtual void Move()
        {
            float deltaX = 0, deltaY = 0;

            switch (CurrentDirection)
            {
                case Direction.Up: deltaY = -Speed; break;
                case Direction.Down: deltaY = Speed; break;
                case Direction.Left: deltaX = -Speed; break;
                case Direction.Right: deltaX = Speed; break;
            }

            if (_plansza.CanMoveTo(X + deltaX, Y + deltaY, Width, Height))
            {
                X += deltaX;
                Y += deltaY;
            }
            else
            {
                CurrentDirection = GetRandomDirection();
            }
        }

        protected Direction GetRandomDirection()
        {
            Array values = Enum.GetValues(typeof(Direction));
            Direction randomDirection;

            do
            {
                randomDirection = (Direction)values.GetValue(_random.Next(values.Length));
            } while (randomDirection == Direction.None);

            return randomDirection;
        }

        protected Direction GetDirectionTowardsPlayer()
        {
            if (_targetPlayer == null) return GetRandomDirection();

            float diffX = _targetPlayer.X - X;
            float diffY = _targetPlayer.Y - Y;

            if (Math.Abs(diffX) > Math.Abs(diffY))
            {
                return diffX > 0 ? Direction.Right : Direction.Left;
            }
            else
            {
                return diffY > 0 ? Direction.Down : Direction.Up;
            }
        }

        public abstract void Draw(ICanvas canvas, int cameraOffsetX, int cameraOffsetY);
    }

    public class BasicEnemy : Enemy
    {
        public BasicEnemy(Plansza plansza, float startX, float startY, Player target)
            : base(plansza, startX, startY, target)
        {
            Type = EnemyType.Basic;
            Speed = 1.2f;
            Width = 32;
            Height = 32;
        }

        public override void Draw(ICanvas canvas, int cameraOffsetX, int cameraOffsetY)
        {
            if (!IsActive) return;

            float adjustedX = X - cameraOffsetX;
            float adjustedY = Y - cameraOffsetY;

            canvas.FillColor = Colors.Red;
            canvas.FillRectangle(adjustedX, adjustedY, Width, Height);

            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2;
            canvas.DrawRectangle(adjustedX, adjustedY, Width, Height);
        }
    }

    public class ShootingWomanEnemy : Enemy
    {
        private int _shootCooldown = 0;
        private Shoot _shootSystem;

        public ShootingWomanEnemy(Plansza plansza, float startX, float startY, Player targetPlayer, Shoot shootSystem)
            : base(plansza, startX, startY, targetPlayer)
        {
            Type = EnemyType.ShootingWoman;
            Width = 28;
            Height = 40;
            Speed = 0.8f;
            _shootSystem = shootSystem;
        }

        public override void Update()
        {
            base.Update();

            if (_shootCooldown > 0)
            {
                _shootCooldown--;
            }
            else if (_targetPlayer != null && _random.Next(100) < 3) // 3% chance to shoot each frame
            {
                Shoot();
                _shootCooldown = 60; // 1 second cooldown (at 60 FPS)
            }
        }

        private void Shoot()
        {
            if (_targetPlayer == null || _shootSystem == null) return;

            // Calculate direction towards player
            Direction shootDirection = GetDirectionTowardsPlayer();

            // Only shoot if we have a clear direction
            if (shootDirection != Direction.None)
            {
                _shootSystem.ShootBullet(X, Y, shootDirection, true, 3.0f);
            }
        }

        public override void Draw(ICanvas canvas, int cameraOffsetX, int cameraOffsetY)
        {
            if (!IsActive) return;

            float adjustedX = X - cameraOffsetX;
            float adjustedY = Y - cameraOffsetY;

            canvas.FillColor = Colors.Purple;
            canvas.FillRectangle(adjustedX, adjustedY, Width, Height);

            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2;
            canvas.DrawRectangle(adjustedX, adjustedY, Width, Height);

            // Draw shooting indicator when in cooldown
            if (_shootCooldown > 0)
            {
                canvas.FillColor = Colors.Orange;
                float centerX = adjustedX + Width / 2 - 3;
                float centerY = adjustedY + Height / 2 - 3;
                canvas.FillEllipse(centerX, centerY, 6, 6);
            }
        }
    }

    public class ChasingDogEnemy : Enemy
    {
        private Queue<Direction> _pathQueue;
        private int _pathUpdateCooldown = 0;
        private const int PATHFINDING_UPDATE_INTERVAL = 20; // Częstsze aktualizacje ścieżki
        private const int TILE_SIZE = 32;
        private Point _lastPlayerPosition;
        private bool _hasValidPath = false;

        public ChasingDogEnemy(Plansza plansza, float startX, float startY, Player targetPlayer)
            : base(plansza, startX, startY, targetPlayer)
        {
            Type = EnemyType.ChasingDog;
            Width = 24;
            Height = 24;
            Speed = 2.0f; // Nieco wolniejsze dla bardziej kontrolowanego ruchu
            _pathQueue = new Queue<Direction>();
            _lastPlayerPosition = new Point(-1, -1);
        }

        public override void Update()
        {
            if (!IsActive || _targetPlayer == null) return;

            Point currentPlayerPosition = new Point(
                (int)(_targetPlayer.X / TILE_SIZE),
                (int)(_targetPlayer.Y / TILE_SIZE)
            );

            // Aktualizuj ścieżkę jeśli gracz się przesunął lub minął cooldown
            bool playerMoved = !currentPlayerPosition.Equals(_lastPlayerPosition);

            if (_pathUpdateCooldown <= 0 || playerMoved || !_hasValidPath)
            {
                UpdatePath();
                _lastPlayerPosition = currentPlayerPosition;
                _pathUpdateCooldown = PATHFINDING_UPDATE_INTERVAL;
            }
            else
            {
                _pathUpdateCooldown--;
            }

            // Wykonaj ruch
            ExecuteMovement();
        }

        private void ExecuteMovement()
        {
            if (_pathQueue.Count > 0 && _hasValidPath)
            {
                // Używaj A* pathfinding
                Direction nextDirection = _pathQueue.Peek();

                if (CanMoveInDirection(nextDirection))
                {
                    CurrentDirection = nextDirection;
                    MoveInDirection(nextDirection);

                    // Sprawdź czy dotarliśmy do następnego punktu na ścieżce
                    if (IsCloseToGridCenter())
                    {
                        _pathQueue.Dequeue();
                    }
                }
                else
                {
                    // Ścieżka jest zablokowana, wyczyść i znajdź nową
                    _pathQueue.Clear();
                    _hasValidPath = false;
                    FallbackMovement();
                }
            }
            else
            {
                // Brak ścieżki A* - użyj prostego ruchu w kierunku gracza
                FallbackMovement();
            }
        }

        private void FallbackMovement()
        {
            Direction directionToPlayer = GetDirectionTowardsPlayer();
            if (CanMoveInDirection(directionToPlayer))
            {
                CurrentDirection = directionToPlayer;
                MoveInDirection(directionToPlayer);
            }
            else
            {
                // Jeśli nie można iść bezpośrednio, spróbuj innych kierunków
                var availableDirections = GetAvailableDirections();
                if (availableDirections.Count > 0)
                {
                    CurrentDirection = availableDirections[_random.Next(availableDirections.Count)];
                    MoveInDirection(CurrentDirection);
                }
            }
        }

        private void UpdatePath()
        {
            if (_targetPlayer == null) return;

            Point start = new Point((int)(X / TILE_SIZE), (int)(Y / TILE_SIZE));
            Point target = new Point((int)(_targetPlayer.X / TILE_SIZE), (int)(_targetPlayer.Y / TILE_SIZE));

            // Nie szukaj ścieżki jeśli jesteśmy bardzo blisko gracza
            if (start.DistanceTo(target) <= 1.5f)
            {
                _pathQueue.Clear();
                _hasValidPath = false;
                return;
            }

            List<Point> path = FindPathAStar(start, target);

            if (path.Count > 1)
            {
                _pathQueue.Clear();
                _hasValidPath = true;

                // Konwertuj ścieżkę na kolejkę kierunków
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Direction direction = GetDirectionFromPoints(path[i], path[i + 1]);
                    if (direction != Direction.None)
                    {
                        _pathQueue.Enqueue(direction);
                    }
                }
            }
            else
            {
                _hasValidPath = false;
            }
        }

        private List<Point> FindPathAStar(Point start, Point target)
        {
            var openSet = new PriorityQueue<AStarNode>();
            var closedSet = new HashSet<Point>();
            var allNodes = new Dictionary<Point, AStarNode>();

            var startNode = new AStarNode(start, 0, ManhattanDistance(start, target), null);
            openSet.Enqueue(startNode, startNode.F);
            allNodes[start] = startNode;

            int maxIterations = 500; // Ograniczenie aby uniknąć nieskończonych pętli
            int iterations = 0;

            while (openSet.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                var currentNode = openSet.Dequeue();

                if (closedSet.Contains(currentNode.Position))
                    continue;

                closedSet.Add(currentNode.Position);

                // Sprawdź czy dotarliśmy do celu
                if (currentNode.Position.X == target.X && currentNode.Position.Y == target.Y)
                {
                    return ReconstructPath(currentNode);
                }

                // Sprawdź wszystkich sąsiadów
                foreach (var neighbor in GetNeighbors(currentNode.Position))
                {
                    if (closedSet.Contains(neighbor) || !IsWalkable(neighbor))
                        continue;

                    float tentativeG = currentNode.G + 1;
                    float h = ManhattanDistance(neighbor, target);

                    if (!allNodes.ContainsKey(neighbor))
                    {
                        var neighborNode = new AStarNode(neighbor, tentativeG, h, currentNode);
                        allNodes[neighbor] = neighborNode;
                        openSet.Enqueue(neighborNode, neighborNode.F);
                    }
                    else if (tentativeG < allNodes[neighbor].G)
                    {
                        var neighborNode = allNodes[neighbor];
                        neighborNode.G = tentativeG;
                        neighborNode.Parent = currentNode;
                        openSet.Enqueue(neighborNode, neighborNode.F);
                    }
                }
            }

            return new List<Point>(); // Brak ścieżki
        }

        private float ManhattanDistance(Point a, Point b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        private List<Point> ReconstructPath(AStarNode node)
        {
            var path = new List<Point>();
            var current = node;

            while (current != null)
            {
                path.Add(current.Position);
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }

        private List<Point> GetNeighbors(Point point)
        {
            return new List<Point>
            {
                new Point(point.X, point.Y - 1), // Up
                new Point(point.X, point.Y + 1), // Down
                new Point(point.X - 1, point.Y), // Left
                new Point(point.X + 1, point.Y)  // Right
            };
        }

        private bool IsWalkable(Point point)
        {
            // Sprawdź granice mapy
            if (point.X < 0 || point.Y < 0) return false;

            float worldX = point.X * TILE_SIZE;
            float worldY = point.Y * TILE_SIZE;

            return _plansza.CanMoveTo(worldX, worldY, Width, Height);
        }

        private Direction GetDirectionFromPoints(Point from, Point to)
        {
            int deltaX = to.X - from.X;
            int deltaY = to.Y - from.Y;

            if (deltaX == 0 && deltaY == -1) return Direction.Up;
            if (deltaX == 0 && deltaY == 1) return Direction.Down;
            if (deltaX == -1 && deltaY == 0) return Direction.Left;
            if (deltaX == 1 && deltaY == 0) return Direction.Right;

            return Direction.None;
        }

        private bool CanMoveInDirection(Direction direction)
        {
            float newX = X;
            float newY = Y;

            switch (direction)
            {
                case Direction.Up: newY -= Speed; break;
                case Direction.Down: newY += Speed; break;
                case Direction.Left: newX -= Speed; break;
                case Direction.Right: newX += Speed; break;
            }

            return _plansza.CanMoveTo(newX, newY, Width, Height);
        }

        private void MoveInDirection(Direction direction)
        {
            float deltaX = 0, deltaY = 0;

            switch (direction)
            {
                case Direction.Up: deltaY = -Speed; break;
                case Direction.Down: deltaY = Speed; break;
                case Direction.Left: deltaX = -Speed; break;
                case Direction.Right: deltaX = Speed; break;
            }

            if (_plansza.CanMoveTo(X + deltaX, Y + deltaY, Width, Height))
            {
                X += deltaX;
                Y += deltaY;
            }
        }

        private List<Direction> GetAvailableDirections()
        {
            var directions = new List<Direction>();

            if (CanMoveInDirection(Direction.Up)) directions.Add(Direction.Up);
            if (CanMoveInDirection(Direction.Down)) directions.Add(Direction.Down);
            if (CanMoveInDirection(Direction.Left)) directions.Add(Direction.Left);
            if (CanMoveInDirection(Direction.Right)) directions.Add(Direction.Right);

            return directions;
        }

        private bool IsCloseToGridCenter()
        {
            int gridCenterX = ((int)(X / TILE_SIZE)) * TILE_SIZE + TILE_SIZE / 2;
            int gridCenterY = ((int)(Y / TILE_SIZE)) * TILE_SIZE + TILE_SIZE / 2;

            return Math.Abs(X - gridCenterX) < Speed * 1.5f && Math.Abs(Y - gridCenterY) < Speed * 1.5f;
        }

        public override void Draw(ICanvas canvas, int cameraOffsetX, int cameraOffsetY)
        {
            if (!IsActive) return;

            float adjustedX = X - cameraOffsetX;
            float adjustedY = Y - cameraOffsetY;

            // Rysuj psa w kolorze brązowym
            canvas.FillColor = Colors.Brown;
            canvas.FillRectangle(adjustedX, adjustedY, Width, Height);

            // Dodaj obramowanie
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2;
            canvas.DrawRectangle(adjustedX, adjustedY, Width, Height);

            // Dodaj wskaźnik kierunku (opcjonalnie)
            if (_hasValidPath && _pathQueue.Count > 0)
            {
                canvas.FillColor = Colors.Yellow;
                float centerX = adjustedX + Width / 2;
                float centerY = adjustedY + Height / 2;

                switch (CurrentDirection)
                {
                    case Direction.Up:
                        canvas.FillRectangle(centerX - 2, adjustedY, 4, 8);
                        break;
                    case Direction.Down:
                        canvas.FillRectangle(centerX - 2, adjustedY + Height - 8, 4, 8);
                        break;
                    case Direction.Left:
                        canvas.FillRectangle(adjustedX, centerY - 2, 8, 4);
                        break;
                    case Direction.Right:
                        canvas.FillRectangle(adjustedX + Width - 8, centerY - 2, 8, 4);
                        break;
                }
            }
        }
    }

    // Klasa pomocnicza dla algorytmu A* z implementacją IComparable dla PriorityQueue
    public class AStarNode : IComparable<AStarNode>
    {
        public Point Position { get; set; }
        public float G { get; set; } // Koszt od startu
        public float H { get; set; } // Heurystyka (odległość do celu)
        public float F => G + H;     // Całkowity koszt
        public AStarNode Parent { get; set; }

        public AStarNode(Point position, float g, float h, AStarNode parent)
        {
            Position = position;
            G = g;
            H = h;
            Parent = parent;
        }

        public int CompareTo(AStarNode other)
        {
            return F.CompareTo(other.F);
        }
    }

    // Prosta implementacja PriorityQueue jeśli nie jest dostępna
    public class PriorityQueue<T> where T : IComparable<T>
    {
        private List<(T item, float priority)> _elements = new List<(T, float)>();

        public int Count => _elements.Count;

        public void Enqueue(T item, float priority)
        {
            _elements.Add((item, priority));
        }

        public T Dequeue()
        {
            if (_elements.Count == 0)
                throw new InvalidOperationException("Queue is empty");

            var bestIndex = 0;
            for (int i = 1; i < _elements.Count; i++)
            {
                if (_elements[i].priority < _elements[bestIndex].priority)
                {
                    bestIndex = i;
                }
            }

            var bestItem = _elements[bestIndex].item;
            _elements.RemoveAt(bestIndex);
            return bestItem;
        }
    }

    // Klasa pomocnicza dla algorytmu A*
    public class PathNode
    {
        public Point Position { get; set; }
        public float G { get; set; } // Koszt od startu
        public float H { get; set; } // Heurystyka (odległość do celu)
        public float F => G + H;     // Całkowity koszt
        public PathNode Parent { get; set; }

        public PathNode(Point position, float g, float h, PathNode parent)
        {
            Position = position;
            G = g;
            H = h;
            Parent = parent;
        }
    }
}