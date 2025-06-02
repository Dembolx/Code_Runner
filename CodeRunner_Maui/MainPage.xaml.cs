using Microsoft.Maui.Controls;
using CodeRunner_Maui.Klasy;
using System.Timers;
using Microsoft.Maui.Platform;
#if ANDROID
using Android.Content.PM;
using Android.Views;
#endif

namespace CodeRunner_Maui
{
    public partial class MainPage : ContentPage
    {
        private readonly Plansza _plansza;
        private readonly Player _player;
        private System.Timers.Timer _movementTimer;
        private Direction _currentDirection = Direction.None;
        private const int MovementSpeed = 2; // pikseli na odświeżenie

        // Parametry kamery
        private float _cameraOffsetX = 0;
        private float _cameraOffsetY = 0;
        private readonly float _cameraSmoothing = 0.1f; // Wartość od 0 do 1, gdzie 1 to natychmiastowe śledzenie
        private float _viewportWidth;
        private float _viewportHeight;

        public MainPage()
        {
            InitializeComponent();
            ForceLandscapeOrientation();

            // Inicjalizacja planszy i gracza
            _plansza = new Plansza(20, 40, 60);
            _player = new Player(_plansza);
            _plansza.SetPlayer(_player);

            // Ustawienie rozmiaru
            graphicsView.WidthRequest = _plansza.Cols * 20;
            graphicsView.HeightRequest = _plansza.Rows * 20;
            graphicsView.Drawable = _plansza;

            // Inicjalizacja rozmiaru widoku kamery
            _viewportWidth = (float)Width;
            _viewportHeight = (float)Height;

            // Ustaw początkowy offset kamery na pozycję gracza
            UpdateCameraPosition(true);

            // Timer dla płynnego ruchu
            _movementTimer = new System.Timers.Timer(16); // ~60 FPS
            _movementTimer.Elapsed += OnMovementTimerTick;
            _movementTimer.AutoReset = true;

            // Nasłuchuj zmian rozmiaru okna/ekranu
            SizeChanged += MainPage_SizeChanged;
        }

        private void MainPage_SizeChanged(object sender, EventArgs e)
        {
            _viewportWidth = (float)Width;
            _viewportHeight = (float)Height;
            UpdateCameraPosition(true);
        }

        private void ForceLandscapeOrientation()
        {
#if ANDROID
            // Wymuś orientację poziomą na Androidzie
            if (Platform.CurrentActivity != null)
            {
                Platform.CurrentActivity.RequestedOrientation = ScreenOrientation.Landscape;
            }
#endif
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _movementTimer.Start();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _movementTimer.Stop();
        }

        private void OnMovementTimerTick(object sender, ElapsedEventArgs e)
        {
            if (_currentDirection != Direction.None)
            {
                Dispatcher.Dispatch(() =>
                {
                    bool moved = false;

                    _plansza.UpdateAllEnemies();
                    graphicsView.Invalidate();

                    switch (_currentDirection)
                    {
                        case Direction.Up:
                            moved = _player.Move(0, -MovementSpeed);
                            break;
                        case Direction.Down:
                            moved = _player.Move(0, MovementSpeed);
                            break;
                        case Direction.Left:
                            moved = _player.Move(-MovementSpeed, 0);
                            break;
                        case Direction.Right:
                            moved = _player.Move(MovementSpeed, 0);
                            break;
                    }

                    if (moved)
                    {
                        // Aktualizuj pozycję kamery po każdym ruchu
                        UpdateCameraPosition();

                        // Ustaw offsety kamery w planszy do rysowania
                        _plansza.CameraOffsetX = (int)_cameraOffsetX;
                        _plansza.CameraOffsetY = (int)_cameraOffsetY;

                        graphicsView.Invalidate();
                    }
                });
            }
        }

        // Aktualizacja pozycji kamery
        private void UpdateCameraPosition(bool immediate = false)
        {
            // Oblicz docelowy offset kamery, aby gracz był ZAWSZE na środku ekranu
            float targetOffsetX = _player.X - (_viewportWidth / 2);
            float targetOffsetY = _player.Y - (_viewportHeight / 2);

            // Usunięto ograniczenia offsetu kamery, aby kamera mogła wyjechać poza planszę
            // gdy gracz jest blisko krawędzi

            if (immediate)
            {
                // Natychmiastowe ustawienie kamery
                _cameraOffsetX = targetOffsetX;
                _cameraOffsetY = targetOffsetY;
            }
            else
            {
                // Zwiększono współczynnik smoothingu dla szybszego śledzenia
                float smoothing = 0.3f; // Zwiększono z 0.1f na 0.3f
                _cameraOffsetX += (targetOffsetX - _cameraOffsetX) * smoothing;
                _cameraOffsetY += (targetOffsetY - _cameraOffsetY) * smoothing;
            }
        }

        // Obsługa przycisków kierunkowych
        private void OnUpClicked(object sender, EventArgs e) => _currentDirection = Direction.Up;
        private void OnDownClicked(object sender, EventArgs e) => _currentDirection = Direction.Down;
        private void OnLeftClicked(object sender, EventArgs e) => _currentDirection = Direction.Left;
        private void OnRightClicked(object sender, EventArgs e) => _currentDirection = Direction.Right;

        // Reset kierunku po puszczeniu przycisku (dla wersji z przytrzymywaniem)
        private void OnButtonReleased(object sender, EventArgs e) => _currentDirection = Direction.None;

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

#if ANDROID
            if (Handler?.PlatformView is Android.Views.View view)
            {
                view.KeyPress += OnAndroidKeyUp;
                view.Focusable = true;
                view.FocusableInTouchMode = true;
                view.RequestFocus();
            }
#endif
        }

#if ANDROID
        private void OnAndroidKeyDown(object sender, Android.Views.View.KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Android.Views.Keycode.W:
                case Android.Views.Keycode.DpadUp:
                    _currentDirection = Direction.Up;
                    e.Handled = true;
                    break;
                case Android.Views.Keycode.S:
                case Android.Views.Keycode.DpadDown:
                    _currentDirection = Direction.Down;
                    e.Handled = true;
                    break;
                case Android.Views.Keycode.A:
                case Android.Views.Keycode.DpadLeft:
                    _currentDirection = Direction.Left;
                    e.Handled = true;
                    break;
                case Android.Views.Keycode.D:
                case Android.Views.Keycode.DpadRight:
                    _currentDirection = Direction.Right;
                    e.Handled = true;
                    break;
            }
        }

        private void OnAndroidKeyUp(object sender, Android.Views.View.KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Android.Views.Keycode.W:
                case Android.Views.Keycode.S:
                case Android.Views.Keycode.A:
                case Android.Views.Keycode.D:
                case Android.Views.Keycode.DpadUp:
                case Android.Views.Keycode.DpadDown:
                case Android.Views.Keycode.DpadLeft:
                case Android.Views.Keycode.DpadRight:
                    _currentDirection = Direction.None;
                    e.Handled = true;
                    break;
            }
        }
#endif
    }

    public enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right
    }
}