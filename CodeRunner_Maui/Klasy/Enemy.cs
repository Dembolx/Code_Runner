using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public int Damage { get; protected set; } = 1;
        public int TouchDamageCooldown { get; protected set; } = 0;

        private const int TOUCH_DAMAGE_COOLDOWN = 60; // 1 sekunda przy 60 FPS

        public virtual void Update()
        {
            if (!IsActive) return;

            // Aktualizuj cooldown dotykowych obrażeń
            if (TouchDamageCooldown > 0)
            {
                TouchDamageCooldown--;
            }

            // Sprawdź kolizję z graczem
            CheckPlayerCollision();

            if (!CanMoveInCurrentDirection())
            {
                CurrentDirection = GetRandomDirection();
            }

            Move();
        }

        protected virtual void CheckPlayerCollision()
        {
            if (_targetPlayer == null || !IsActive) return;

            // Sprawdź czy wróg dotyka gracza
            if (CollidesWith(_targetPlayer.X, _targetPlayer.Y, _targetPlayer.Width, _targetPlayer.Height))
            {
                // Zadaj obrażenia tylko jeśli minął cooldown
                if (TouchDamageCooldown <= 0)
                {
                    _targetPlayer.TakeDamage(Damage);
                    TouchDamageCooldown = TOUCH_DAMAGE_COOLDOWN;
                    OnPlayerHit(); // Wywołaj event gdy gracz zostanie trafiony
                }
            }
        }

        protected virtual void OnPlayerHit()
        {
            // Metoda do przesłonięcia w klasach potomnych
            // Można tutaj dodać efekty specjalne po trafieniu gracza
        }

        protected bool CollidesWith(float otherX, float otherY, float otherWidth, float otherHeight)
        {
            return X < otherX + otherWidth &&
                   X + Width > otherX &&
                   Y < otherY + otherHeight &&
                   Y + Height > otherY;
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

            CheckPlayerCollision();
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

        // Metoda sprawdzająca czy gracz jest w zasięgu i w linii wzroku
        protected bool CanSeePlayer(float maxDistance = 100f)
        {
            if (_targetPlayer == null) return false;

            // Sprawdź odległość
            float distance = (float)Math.Sqrt(Math.Pow(_targetPlayer.X - X, 2) + Math.Pow(_targetPlayer.Y - Y, 2));
            if (distance > maxDistance) return false;

            // Sprawdź linię wzroku
            return HasClearLineOfSight(_targetPlayer.X + _targetPlayer.Width / 2, _targetPlayer.Y + _targetPlayer.Height / 2);
        }

        // Sprawdzanie czy jest czysta linia wzroku do gracza
        protected bool HasClearLineOfSight(float targetX, float targetY)
        {
            float startX = X + Width / 2;
            float startY = Y + Height / 2;

            float deltaX = targetX - startX;
            float deltaY = targetY - startY;
            float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            // Normalizuj wektor kierunku
            float stepX = deltaX / distance;
            float stepY = deltaY / distance;

            // Sprawdź punkty wzdłuż linii co 4 piksele
            for (float i = 0; i < distance; i += 4)
            {
                float checkX = startX + stepX * i;
                float checkY = startY + stepY * i;

                if (!_plansza.IsPath(checkX, checkY))
                {
                    return false; // Napotkano ścianę
                }
            }

            return true;
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
            Damage = 1; // Podstawowe obrażenia
        }

        protected override void OnPlayerHit()
        {
            // BasicEnemy może mieć specjalny efekt po trafieniu gracza
            // Np. krótkie spowolnienie lub zmiana kierunku
            CurrentDirection = GetRandomDirection();
        }

        public override void Draw(ICanvas canvas, int cameraOffsetX, int cameraOffsetY)
        {
            if (!IsActive) return;

            float adjustedX = X - cameraOffsetX;
            float adjustedY = Y - cameraOffsetY;

            // Migotanie gdy może zadać obrażenia
            if (TouchDamageCooldown > 0)
            {
                canvas.FillColor = Colors.Orange;
            }
            else
            {
                canvas.FillColor = Colors.Red;
            }

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
        private const float SHOOTING_RANGE = 100f;

        public ShootingWomanEnemy(Plansza plansza, float startX, float startY, Player targetPlayer, Shoot shootSystem)
        : base(plansza, startX, startY, targetPlayer)
        {
            Type = EnemyType.ShootingWoman;
            Width = 28;
            Height = 40;
            Speed = 0.8f;
            Damage = 2; // Większe obrażenia od kontaktu
            _shootSystem = shootSystem;
        }

        protected override void OnPlayerHit()
        {
            // BasicEnemy może mieć specjalny efekt po trafieniu gracza
            // Np. krótkie spowolnienie lub zmiana kierunku
            CurrentDirection = GetRandomDirection();
        }

        public override void Update()
        {
            base.Update();

            if (_shootCooldown > 0)
            {
                _shootCooldown--;
            }

            // Sprawdź czy gracz jest w zasięgu
            if (IsPlayerInRange())
            {
                // Jeśli cooldown skończył się, strzelaj
                if (_shootCooldown <= 0)
                {
                    Shoot();
                    _shootCooldown = 60; // 1 sekunda cooldownu (przy 60 FPS)
                }
            }
        }

        // Uproszczona metoda sprawdzania zasięgu
        private bool IsPlayerInRange()
        {
            if (_targetPlayer == null) return false;

            // Oblicz odległość do gracza
            float playerCenterX = _targetPlayer.X + _targetPlayer.Width / 2;
            float playerCenterY = _targetPlayer.Y + _targetPlayer.Height / 2;
            float enemyCenterX = X + Width / 2;
            float enemyCenterY = Y + Height / 2;

            float distance = (float)Math.Sqrt(
                Math.Pow(playerCenterX - enemyCenterX, 2) +
                Math.Pow(playerCenterY - enemyCenterY, 2)
            );

            if (distance > SHOOTING_RANGE) return false;

            // Sprawdź linię wzroku (uproszczona wersja)
            return HasClearLineOfSightToPlayer();
        }

        // Uproszczona metoda sprawdzania linii wzroku
        private bool HasClearLineOfSightToPlayer()
        {
            if (_targetPlayer == null) return false;

            float startX = X + Width / 2;
            float startY = Y + Height / 2;
            float targetX = _targetPlayer.X + _targetPlayer.Width / 2;
            float targetY = _targetPlayer.Y + _targetPlayer.Height / 2;

            float deltaX = targetX - startX;
            float deltaY = targetY - startY;
            float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            if (distance == 0) return true;

            // Normalizuj wektor kierunku
            float stepX = (deltaX / distance) * 8; // Co 8 pikseli
            float stepY = (deltaY / distance) * 8;

            // Sprawdź punkty wzdłuż linii
            int steps = (int)(distance / 8);
            for (int i = 1; i < steps; i++)
            {
                float checkX = startX + stepX * i;
                float checkY = startY + stepY * i;

                // Sprawdź czy punkt jest w ścianie
                if (!_plansza.IsPath(checkX, checkY))
                {
                    return false; // Napotkano ścianę
                }
            }

            return true;
        }

        private void Shoot()
        {
            if (_targetPlayer == null || _shootSystem == null) return;

            // Oblicz kierunek do gracza
            Direction shootDirection = GetDirectionTowardsPlayer();

            // Strzelaj w kierunku gracza
            if (shootDirection != Direction.None)
            {
                _shootSystem.ShootBullet(X, Y, shootDirection, true, 3.0f);
            }
        }

        // Poprawiona metoda obliczania kierunku
        protected new Direction GetDirectionTowardsPlayer()
        {
            if (_targetPlayer == null) return Direction.None;

            float playerCenterX = _targetPlayer.X + _targetPlayer.Width / 2;
            float playerCenterY = _targetPlayer.Y + _targetPlayer.Height / 2;
            float enemyCenterX = X + Width / 2;
            float enemyCenterY = Y + Height / 2;

            float diffX = playerCenterX - enemyCenterX;
            float diffY = playerCenterY - enemyCenterY;

            // Określ główny kierunek na podstawie większej różnicy
            if (Math.Abs(diffX) > Math.Abs(diffY))
            {
                return diffX > 0 ? Direction.Right : Direction.Left;
            }
            else
            {
                return diffY > 0 ? Direction.Down : Direction.Up;
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

            // Rysuj wskaźnik strzelania gdy w cooldownie
            if (_shootCooldown > 0)
            {
                canvas.FillColor = Colors.Orange;
                float centerX = adjustedX + Width / 2 - 3;
                float centerY = adjustedY + Height / 2 - 3;
                canvas.FillEllipse(centerX, centerY, 6, 6);
            }

            // Rysuj zasięg strzelania gdy gracz jest w zasięgu
            if (IsPlayerInRange())
            {
                canvas.StrokeColor = Colors.Yellow;
                canvas.StrokeSize = 2;
                float centerX = adjustedX + Width / 2;
                float centerY = adjustedY + Height / 2;
                canvas.DrawEllipse(centerX - SHOOTING_RANGE, centerY - SHOOTING_RANGE, SHOOTING_RANGE * 2, SHOOTING_RANGE * 2);

                // Dodaj czerwoną ramkę gdy można strzelać
                if (_shootCooldown <= 0)
                {
                    canvas.StrokeColor = Colors.Red;
                    canvas.StrokeSize = 3;
                    canvas.DrawRectangle(adjustedX - 2, adjustedY - 2, Width + 4, Height + 4);
                }
            }
        }
    }
    public class ChasingDogEnemy : Enemy
    {
        private Queue<Direction> _pathQueue;
        private int _pathUpdateCooldown = 0;
        private const int PATHFINDING_UPDATE_INTERVAL = 180; // Zwiększone z 20 do 90 klatek
        private const int TILE_SIZE = 32;
        private Point _lastPlayerPosition;
        private bool _hasValidPath = false;

        // Nowe zmienne optymalizacyjne
        private const float MAX_PATHFINDING_DISTANCE = 5.0f; // Maksymalna odległość w tile'ach dla pathfindingu
        private const float DIRECT_CHASE_DISTANCE = 3.0f; // Odległość przy której pies idzie prosto do gracza
        private const int MAX_PATH_LENGTH = 15; // Maksymalna długość ścieżki
        private int _stuckCounter = 0;
        private const int MAX_STUCK_FRAMES = 30;
        private Point _lastPosition;

        public ChasingDogEnemy(Plansza plansza, float startX, float startY, Player targetPlayer)
            : base(plansza, startX, startY, targetPlayer)
        {
            Type = EnemyType.ChasingDog;
            Width = 24;
            Height = 24;
            Speed = 2.0f;
            _pathQueue = new Queue<Direction>();
            _lastPlayerPosition = new Point(-1, -1);
            _lastPosition = new Point((int)(startX / TILE_SIZE), (int)(startY / TILE_SIZE));
        }

        protected override void OnPlayerHit()
        {
            _plansza.RemoveEnemy(this);
            IsActive = false;
            Console.WriteLine("Usuniety dziad");
        }

        // Oraz zastąp metodę CheckPlayerCollision w klasie ChasingDogEnemy tym kodem:
        protected override void CheckPlayerCollision()
        {
            if (_targetPlayer == null || !IsActive) return;

            Debug.WriteLine($"Pies: X={X}, Y={Y}, W={Width}, H={Height}");
            Debug.WriteLine($"Gracz: X={_targetPlayer.X}, Y={_targetPlayer.Y}, W={_targetPlayer.Width}, H={_targetPlayer.Height}");

            if (CollidesWith(_targetPlayer.X, _targetPlayer.Y, _targetPlayer.Width, _targetPlayer.Height))
            {
                Debug.WriteLine("KOLIZJA!");
                if (TouchDamageCooldown <= 0)
                {
                    _targetPlayer.TakeDamage(Damage);
                    TouchDamageCooldown = 60;
                    OnPlayerHit();
                }
            }
        }


        public override void Update()
        {
            if (!IsActive || _targetPlayer == null) return;

            Point currentPosition = new Point((int)(X / TILE_SIZE), (int)(Y / TILE_SIZE));
            Point currentPlayerPosition = new Point(
                (int)(_targetPlayer.X / TILE_SIZE),
                (int)(_targetPlayer.Y / TILE_SIZE)
            );

            // Sprawdź czy pies się nie zacina
            if (currentPosition.X == _lastPosition.X && currentPosition.Y == _lastPosition.Y)
            {
                _stuckCounter++;
                if (_stuckCounter > MAX_STUCK_FRAMES)
                {
                    _pathQueue.Clear();
                    _hasValidPath = false;
                    _stuckCounter = 0;
                }
            }
            else
            {
                _stuckCounter = 0;
                _lastPosition = currentPosition;
            }

            float distanceToPlayer = currentPosition.DistanceTo(currentPlayerPosition);
            bool playerMoved = !currentPlayerPosition.Equals(_lastPlayerPosition);

            // Jeśli gracz jest bardzo blisko, idź prosto do niego (najszybsze)
            if (distanceToPlayer <= DIRECT_CHASE_DISTANCE)
            {
                DirectChaseMovement();
                return;
            }

            // Jeśli gracz jest za daleko, nie używaj pathfindingu (oszczędność CPU)
            if (distanceToPlayer > MAX_PATHFINDING_DISTANCE)
            {
                SimpleChaseMovement();
                return;
            }

            // Aktualizuj ścieżkę tylko gdy to konieczne
            if (_pathUpdateCooldown <= 0 || playerMoved || !_hasValidPath)
            {
                UpdatePathOptimized();
                _lastPlayerPosition = currentPlayerPosition;
                _pathUpdateCooldown = PATHFINDING_UPDATE_INTERVAL;
            }
            else
            {
                _pathUpdateCooldown--;
            }

            ExecuteMovement();

            CheckPlayerCollision();
        }

        private void DirectChaseMovement()
        {
            Direction directionToPlayer = GetDirectionTowardsPlayer();
            if (CanMoveInDirection(directionToPlayer))
            {
                CurrentDirection = directionToPlayer;
                MoveInDirection(directionToPlayer);
            }
            else
            {
                // Jeśli nie może iść prosto, spróbuj alternatywnych kierunków
                var availableDirections = GetAvailableDirections();
                if (availableDirections.Count > 0)
                {
                    // Wybierz kierunek najbliższy do gracza
                    Direction bestDirection = GetBestAlternativeDirection(directionToPlayer, availableDirections);
                    CurrentDirection = bestDirection;
                    MoveInDirection(bestDirection);
                }
            }
        }

        private void SimpleChaseMovement()
        {
            // Prosty ruch w kierunku gracza bez pathfindingu
            Direction directionToPlayer = GetDirectionTowardsPlayer();

            if (CanMoveInDirection(directionToPlayer))
            {
                CurrentDirection = directionToPlayer;
                MoveInDirection(directionToPlayer);
            }
            else
            {
                // Spróbuj alternatywnych kierunków
                var availableDirections = GetAvailableDirections();
                if (availableDirections.Count > 0)
                {
                    Direction bestDirection = GetBestAlternativeDirection(directionToPlayer, availableDirections);
                    CurrentDirection = bestDirection;
                    MoveInDirection(bestDirection);
                }
            }
        }

        private Direction GetBestAlternativeDirection(Direction preferredDirection, List<Direction> availableDirections)
        {
            // Priorytet kierunków względem preferowanego
            var priorities = new Dictionary<Direction, int>();

            switch (preferredDirection)
            {
                case Direction.Up:
                    priorities[Direction.Left] = 1;
                    priorities[Direction.Right] = 1;
                    priorities[Direction.Down] = 2;
                    break;
                case Direction.Down:
                    priorities[Direction.Left] = 1;
                    priorities[Direction.Right] = 1;
                    priorities[Direction.Up] = 2;
                    break;
                case Direction.Left:
                    priorities[Direction.Up] = 1;
                    priorities[Direction.Down] = 1;
                    priorities[Direction.Right] = 2;
                    break;
                case Direction.Right:
                    priorities[Direction.Up] = 1;
                    priorities[Direction.Down] = 1;
                    priorities[Direction.Left] = 2;
                    break;
            }

            return availableDirections
                .OrderBy(d => priorities.ContainsKey(d) ? priorities[d] : 3)
                .First();
        }

        private void ExecuteMovement()
        {
            if (_pathQueue.Count > 0 && _hasValidPath)
            {
                Direction nextDirection = _pathQueue.Peek();

                if (CanMoveInDirection(nextDirection))
                {
                    CurrentDirection = nextDirection;
                    MoveInDirection(nextDirection);

                    if (IsCloseToGridCenter())
                    {
                        _pathQueue.Dequeue();
                    }
                }
                else
                {
                    _pathQueue.Clear();
                    _hasValidPath = false;
                    FallbackMovement();
                }
            }
            else
            {
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
                var availableDirections = GetAvailableDirections();
                if (availableDirections.Count > 0)
                {
                    CurrentDirection = availableDirections[_random.Next(availableDirections.Count)];
                    MoveInDirection(CurrentDirection);
                }
            }
        }

        private void UpdatePathOptimized()
        {
            if (_targetPlayer == null) return;

            Point start = new Point((int)(X / TILE_SIZE), (int)(Y / TILE_SIZE));
            Point target = new Point((int)(_targetPlayer.X / TILE_SIZE), (int)(_targetPlayer.Y / TILE_SIZE));

            // Jeśli gracz jest bardzo blisko, nie używaj pathfindingu
            if (start.DistanceTo(target) <= 1.5f)
            {
                _pathQueue.Clear();
                _hasValidPath = false;
                return;
            }

            List<Point> path = FindPathAStarOptimized(start, target);

            if (path.Count > 1)
            {
                _pathQueue.Clear();
                _hasValidPath = true;

                // Ogranicz długość ścieżki
                int pathLength = Math.Min(path.Count - 1, MAX_PATH_LENGTH);

                for (int i = 0; i < pathLength; i++)
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

        private List<Point> FindPathAStarOptimized(Point start, Point target)
        {
            var openSet = new SortedSet<AStarNodeOptimized>(new AStarNodeComparer());
            var closedSet = new HashSet<Point>();
            var allNodes = new Dictionary<Point, AStarNodeOptimized>();

            var startNode = new AStarNodeOptimized(start, 0, ManhattanDistance(start, target), null);
            openSet.Add(startNode);
            allNodes[start] = startNode;

            // Znacznie zredukowane limity
            int maxIterations = 70; // Zmniejszone z 500
            int iterations = 0;
            float maxDistance = MAX_PATHFINDING_DISTANCE;

            while (openSet.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                var currentNode = openSet.Min;
                openSet.Remove(currentNode);

                if (closedSet.Contains(currentNode.Position))
                    continue;

                closedSet.Add(currentNode.Position);

                if (currentNode.Position.X == target.X && currentNode.Position.Y == target.Y)
                {
                    return ReconstructPath(currentNode);
                }

                // Ograniczenie odległości przeszukiwania
                if (currentNode.Position.DistanceTo(start) > maxDistance)
                    continue;

                foreach (var neighbor in GetNeighbors(currentNode.Position))
                {
                    if (closedSet.Contains(neighbor) || !IsWalkable(neighbor))
                        continue;

                    float tentativeG = currentNode.G + 1;
                    float h = ManhattanDistance(neighbor, target);

                    if (!allNodes.ContainsKey(neighbor))
                    {
                        var neighborNode = new AStarNodeOptimized(neighbor, tentativeG, h, currentNode);
                        allNodes[neighbor] = neighborNode;
                        openSet.Add(neighborNode);
                    }
                    else if (tentativeG < allNodes[neighbor].G)
                    {
                        var neighborNode = allNodes[neighbor];
                        openSet.Remove(neighborNode);
                        neighborNode.G = tentativeG;
                        neighborNode.Parent = currentNode;
                        openSet.Add(neighborNode);
                    }
                }
            }

            return new List<Point>();
        }

        private float ManhattanDistance(Point a, Point b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        private List<Point> ReconstructPath(AStarNodeOptimized node)
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
            new Point(point.X, point.Y - 1),
            new Point(point.X, point.Y + 1),
            new Point(point.X - 1, point.Y),
            new Point(point.X + 1, point.Y)
        };
        }

        private bool IsWalkable(Point point)
        {
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
            if(!IsActive) return;

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

            canvas.FillColor = Colors.Brown;
            canvas.FillRectangle(adjustedX, adjustedY, Width, Height);

            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2;
            canvas.DrawRectangle(adjustedX, adjustedY, Width, Height);

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

    // Zoptymalizowana klasa węzła A*
    public class AStarNodeOptimized
    {
        public Point Position { get; set; }
        public float G { get; set; }
        public float H { get; set; }
        public float F => G + H;
        public AStarNodeOptimized Parent { get; set; }

        public AStarNodeOptimized(Point position, float g, float h, AStarNodeOptimized parent)
        {
            Position = position;
            G = g;
            H = h;
            Parent = parent;
        }
    }

    // Komparator dla SortedSet
    public class AStarNodeComparer : IComparer<AStarNodeOptimized>
    {
        public int Compare(AStarNodeOptimized x, AStarNodeOptimized y)
        {
            int result = x.F.CompareTo(y.F);
            if (result == 0)
            {
                // Jeśli F jest równe, porównaj pozycje dla unikalności
                result = x.Position.X.CompareTo(y.Position.X);
                if (result == 0)
                {
                    result = x.Position.Y.CompareTo(y.Position.Y);
                }
            }
            return result;
        }
    }

    public class AStarNode : IComparable<AStarNode>
    {
        public Point Position { get; set; }
        public float G { get; set; }
        public float H { get; set; }
        public float F => G + H;
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

    public class PathNode
    {
        public Point Position { get; set; }
        public float G { get; set; }
        public float H { get; set; }
        public float F => G + H;
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