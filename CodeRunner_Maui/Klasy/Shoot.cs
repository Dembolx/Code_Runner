using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeRunner_Maui.Klasy
{
    public class Bullet
    {
        public float X { get; set; }
        public float Y { get; set; }
        public Direction Direction { get; set; }
        public float Speed { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFromEnemy { get; set; } // true if shot by enemy, false if shot by player

        private const int BULLET_SIZE = 6;

        public Bullet(float startX, float startY, Direction direction, float speed, bool isFromEnemy)
        {
            X = startX;
            Y = startY;
            Direction = direction;
            Speed = speed;
            IsFromEnemy = isFromEnemy;
        }

        public void Update()
        {
            if (!IsActive) return;

            switch (Direction)
            {
                case Direction.Up: Y -= Speed; break;
                case Direction.Down: Y += Speed; break;
                case Direction.Left: X -= Speed; break;
                case Direction.Right: X += Speed; break;
            }
        }

        public void Draw(ICanvas canvas, int cameraOffsetX, int cameraOffsetY)
        {
            if (!IsActive) return;

            float adjustedX = X - cameraOffsetX;
            float adjustedY = Y - cameraOffsetY;

            // Different colors for enemy vs player bullets
            canvas.FillColor = IsFromEnemy ? Colors.Red : Colors.Yellow;
            canvas.FillEllipse(adjustedX, adjustedY, BULLET_SIZE, BULLET_SIZE);

            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 1;
            canvas.DrawEllipse(adjustedX, adjustedY, BULLET_SIZE, BULLET_SIZE);
        }

        public bool CheckCollisionWithPlayer(Player player)
        {
            if (!IsActive || !IsFromEnemy) return false;

            return X < player.X + player.Width &&
                   X + BULLET_SIZE > player.X &&
                   Y < player.Y + player.Height &&
                   Y + BULLET_SIZE > player.Y;
        }

        public bool CheckCollisionWithEnemy(Enemy enemy)
        {
            if (!IsActive || IsFromEnemy) return false;

            return X < enemy.X + enemy.Width &&
                   X + BULLET_SIZE > enemy.X &&
                   Y < enemy.Y + enemy.Height &&
                   Y + BULLET_SIZE > enemy.Y;
        }
    }

    // Updated Shoot class
    public class Shoot
    {
        private Plansza _plansza;
        private List<Bullet> _bullets;

        public Shoot(Plansza plansza)
        {
            _plansza = plansza;
            _bullets = new List<Bullet>();
        }

        public void ShootBullet(float startX, float startY, Direction direction, bool isFromEnemy, float speed = 4.0f)
        {
            // Adjust starting position to center of shooter
            float bulletStartX = startX;
            float bulletStartY = startY;

            switch (direction)
            {
                case Direction.Up:
                    bulletStartX += 16; // Center horizontally
                    break;
                case Direction.Down:
                    bulletStartX += 16;
                    bulletStartY += 32; // Start at bottom of shooter
                    break;
                case Direction.Left:
                    bulletStartY += 16; // Center vertically
                    break;
                case Direction.Right:
                    bulletStartX += 32; // Start at right of shooter
                    bulletStartY += 16;
                    break;
            }

            _bullets.Add(new Bullet(bulletStartX, bulletStartY, direction, speed, isFromEnemy));
        }

        public void Update(Player player, List<Enemy> enemies)
        {
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                var bullet = _bullets[i];
                bullet.Update();

                // Check if bullet is out of bounds
                if (bullet.X < 0 || bullet.Y < 0 ||
                    bullet.X > _plansza.Cols * 32 || bullet.Y > _plansza.Rows * 32)
                {
                    _bullets.RemoveAt(i);
                    continue;
                }

                // Check wall collision
                if (!_plansza.IsPath(bullet.X, bullet.Y))
                {
                    _bullets.RemoveAt(i);
                    continue;
                }

                // Check player collision (enemy bullets)
                if (bullet.CheckCollisionWithPlayer(player))
                {
                    // Handle player hit - you can implement damage system here
                    // For now, just remove the bullet
                    _bullets.RemoveAt(i);
                    continue;
                }

                // Check enemy collision (player bullets)
                bool hitEnemy = false;
                foreach (var enemy in enemies)
                {
                    if (bullet.CheckCollisionWithEnemy(enemy))
                    {
                        enemy.IsActive = false; // Kill the enemy
                        hitEnemy = true;
                        break;
                    }
                }

                if (hitEnemy)
                {
                    _bullets.RemoveAt(i);
                }
            }
        }

        public void Draw(ICanvas canvas, int cameraOffsetX, int cameraOffsetY)
        {
            foreach (var bullet in _bullets)
            {
                bullet.Draw(canvas, cameraOffsetX, cameraOffsetY);
            }
        }

        public void ClearBullets()
        {
            _bullets.Clear();
        }
    }
}
