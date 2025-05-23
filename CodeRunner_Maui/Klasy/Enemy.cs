using Microsoft.Maui.Graphics;
using System;

namespace CodeRunner_Maui.Klasy
{
    public class Enemy
    {
        public float X { get; set; }
        public float Y { get; set; }
        public int Width { get; private set; } = 32;
        public int Height { get; private set; } = 32;
        public float Speed { get; private set; } = 1.5f;
        public Direction CurrentDirection { get; private set; }

        private readonly Plansza _plansza;
        private readonly Random _random;
        private int _directionChangeCooldown = 0;

        public Enemy(Plansza plansza, float startX, float startY)
        {
            _plansza = plansza;
            _random = new Random();
            X = startX;
            Y = startY;
            CurrentDirection = GetRandomDirection();
        }

        public void Update()
        {
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

        private bool CanMoveInCurrentDirection()
        {
            float newX = X;
            float newY = Y;

            switch (CurrentDirection)
            {
                case Direction.Up:
                    newY -= Speed;
                    break;
                case Direction.Down:
                    newY += Speed;
                    break;
                case Direction.Left:
                    newX -= Speed;
                    break;
                case Direction.Right:
                    newX += Speed;
                    break;
            }

            return _plansza.CanMoveTo(newX, newY, Width, Height);
        }

        private void Move()
        {
            float deltaX = 0, deltaY = 0;

            switch (CurrentDirection)
            {
                case Direction.Up:
                    deltaY = -Speed;
                    break;
                case Direction.Down:
                    deltaY = Speed;
                    break;
                case Direction.Left:
                    deltaX = -Speed;
                    break;
                case Direction.Right:
                    deltaX = Speed;
                    break;
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

        private Direction GetRandomDirection()
        {
            Array values = Enum.GetValues(typeof(Direction));
            Direction randomDirection;

            do
            {
                randomDirection = (Direction)values.GetValue(_random.Next(values.Length));
            } while (randomDirection == Direction.None);

            return randomDirection;
        }

        public void Draw(ICanvas canvas, int cameraOffsetX, int cameraOffsetY)
        {
            float adjustedX = X - cameraOffsetX;
            float adjustedY = Y - cameraOffsetY;

            canvas.FillColor = Colors.Red;
            canvas.FillRectangle(adjustedX, adjustedY, Width, Height);

            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2;
            canvas.DrawRectangle(adjustedX, adjustedY, Width, Height);
        }
    }
}