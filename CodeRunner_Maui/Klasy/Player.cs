using Microsoft.Maui.Graphics;
using System;

namespace CodeRunner_Maui.Klasy
{
    public class Player
    {
        public float X { get; set; }
        public float Y { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Lives {  get; set; }

        private Plansza _plansza;

        public int MaxLives { get; private set; }
        public bool IsInvulnerable { get; private set; }
        public int InvulnerabilityTimer { get; private set; }

        private const int INVULNERABILITY_DURATION = 120; // 2 sekundy przy 60 FPS

        public ICanvas _canvas;

        public Player(Plansza plansza, int width = 32, int height = 32, ICanvas canvas = null)
        {
            _plansza = plansza;
            Width = width;
            Height = height;
            MaxLives = 5;
            Lives = MaxLives;
            IsInvulnerable = false;
            InvulnerabilityTimer = 0;
            X = 0;
            Y = 0;
            _canvas = canvas;
        }

        public void TakeDamage(int damage = 1)
        {
            Lives -= damage;

            // Efekt wizualny
            if (_canvas != null)
            {
                _canvas.FillColor = Colors.Red;
                _canvas.FillRectangle(X, Y, Width, Height);
            }
        }

        public void Update()
        {
            if (IsInvulnerable)
            {
                InvulnerabilityTimer--;
                if (InvulnerabilityTimer <= 0)
                {
                    IsInvulnerable = false;
                }
            }
        }

        public bool IsDead()
        {
            return Lives <= 0;
        }

        public void Heal(int amount = 1)
        {
            Lives += amount;
            if (Lives > MaxLives) Lives = MaxLives;
        }

        // Sprawdza kolizję z prostokątem (np. wrogiem)
        public bool CollidesWith(float otherX, float otherY, int otherWidth, int otherHeight)
        {
            return X < otherX + otherWidth &&
                   X + Width > otherX &&
                   Y < otherY + otherHeight &&
                   Y + Height > otherY;
        }

        // Metoda próbująca poruszyć gracza w podanym kierunku
        public bool Move(float deltaX, float deltaY)
        {
            float newX = X + deltaX;
            float newY = Y + deltaY;

            // Sprawdź, czy nowa pozycja jest dozwolona - na ścieżce, nie na ścianie
            if (_plansza.CanPlayerMoveTo(newX, newY))
            {
                X = newX;
                Y = newY;
                return true; // Ruch się powiódł
            }

            // Jeśli pełny ruch nie jest możliwy, spróbuj ruch tylko w jednej osi
            // To pozwala na łatwiejsze poruszanie się wzdłuż ścian
            if (deltaX != 0 && deltaY != 0)
            {
                // Próba ruchu tylko w osi X
                if (_plansza.CanPlayerMoveTo(newX, Y))
                {
                    X = newX;
                    return true;
                }

                // Próba ruchu tylko w osi Y
                if (_plansza.CanPlayerMoveTo(X, newY))
                {
                    Y = newY;
                    return true;
                }
            }
            else if (deltaX != 0)
            {
                // Próba "ślizgania się" wzdłuż ściany w kierunku X
                // Znajdź maksymalną dozwoloną odległość ruchu
                float maxDeltaX = 0;
                float sign = Math.Sign(deltaX);

                for (float i = sign; Math.Abs(i) <= Math.Abs(deltaX); i += sign)
                {
                    if (_plansza.CanPlayerMoveTo(X + i, Y))
                    {
                        maxDeltaX = i;
                    }
                    else
                    {
                        break;
                    }
                }

                if (maxDeltaX != 0)
                {
                    X += maxDeltaX;
                    return true;
                }
            }
            else if (deltaY != 0)
            {
                // Próba "ślizgania się" wzdłuż ściany w kierunku Y
                float maxDeltaY = 0;
                float sign = Math.Sign(deltaY);

                for (float i = sign; Math.Abs(i) <= Math.Abs(deltaY); i += sign)
                {
                    if (_plansza.CanPlayerMoveTo(X, Y + i))
                    {
                        maxDeltaY = i;
                    }
                    else
                    {
                        break;
                    }
                }

                if (maxDeltaY != 0)
                {
                    Y += maxDeltaY;
                    return true;
                }
            }

            return false; // Nie można wykonać ruchu
        }
    }
}