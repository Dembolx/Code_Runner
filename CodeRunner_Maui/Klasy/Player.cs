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

        private Plansza _plansza;

        public Player(Plansza plansza, int width = 32, int height = 32)
        {
            _plansza = plansza;
            Width = width;
            Height = height;

            // Początkowa pozycja zostanie ustawiona przez Plansza.PlacePlayerOnPath()
            X = 0;
            Y = 0;
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