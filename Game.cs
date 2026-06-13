using Raylib_cs;

namespace GravityDefied
{
    // Port of class 'Micro' (the MIDlet): owns the bike, level, renderer and menu,
    // and runs the game loop. The original J2ME threading (game thread + menu loop
    // + repaint thread) is flattened into one raylib frame loop driven by Frame().
    internal sealed class Game
    {
        public static bool InMenu = true;   // Micro.b

        private enum St { Splash, Menu, Play }

        private readonly Renderer rd;
        private readonly Bike bike;
        private readonly Levels lev;
        private readonly Menu menu;

        private St state = St.Splash;
        private const int NumLoops = 2;     // Micro.null

        private long gameTime = 0;          // Micro.goto
        private bool moving = false;        // Micro.if
        private bool started = false;       // realStart != 0
        private double crashUntil = 0;      // Micro.byte (wall clock seconds)
        private bool crashed = false;
        private string message = null;
        private double msgUntil = 0;
        private bool pauseFrozen = false;

        private int splashPhase = 0;
        private double splashUntil;

        public bool Running = true;
        public bool AutoAccel = false;   // for headless screenshots

        public Game(int width, int height)
        {
            lev = new Levels();
            rd = new Renderer(width, height);
            bike = new Bike(lev);
            rd.SetBike(bike);
            menu = new Menu(this, bike, lev, rd);
            bike.SetMode(1);     // init gravity/engine, reset bike
            InMenu = true;
            bike.Pause();        // auto-balance in menu background
            splashUntil = Raylib.GetTime() + 1.5;
            state = St.Splash;
        }

        // ===== transitions =====

        public void MenuToGame()
        {
            InMenu = false;
            state = St.Play;
            pauseFrozen = false;
            if (menu.ConsumeStart()) Restart(true);
        }

        public void GameToMenu()   // pause
        {
            InMenu = true;
            state = St.Menu;
            pauseFrozen = true;     // freeze the simulation behind the pause menu
            menu.ShowIngame();
        }

        public void GameToMenuFinished()
        {
            InMenu = true;
            state = St.Menu;
            pauseFrozen = true;
        }

        public void Exit() => Running = false;

        // for headless screenshots: jump straight into a level
        public void DebugStart(int group, int track, int league)
        {
            lev.SelectLevel(group, track);
            bike.SetLeague(league);
            InMenu = false;
            state = St.Play;
            pauseFrozen = false;
            Restart(true);
        }

        // Micro.restart(boolean)
        private void Restart(bool showName)
        {
            bike.ResetState(true);
            started = false;
            gameTime = 0;
            moving = false;
            crashUntil = 0;
            crashed = false;
            if (showName)
            {
                message = lev.Name(menu.Group(), menu.Track());
                msgUntil = Raylib.GetTime() + 3.0;
            }
            bike.ClearInput();
        }

        // ===== frame =====

        public void Frame()
        {
            double now = Raylib.GetTime();
            switch (state)
            {
                case St.Splash:
                    if (now >= splashUntil)
                    {
                        if (splashPhase == 0) { splashPhase = 1; splashUntil = now + 1.5; }
                        else { state = St.Menu; InMenu = true; }
                    }
                    Draw();
                    return;
                case St.Menu:
                    MenuInput();
                    if (!pauseFrozen && bike.IsPaused())
                    {
                        try { Renderer.WindTick(); bike.Step(); bike.SaveSnapshot(); }
                        catch (System.Exception) { }
                    }
                    Draw();
                    return;
                case St.Play:
                    PlayUpdate(now);
                    Draw();
                    return;
            }
        }

        private void PlayUpdate(double now)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.M))
            {
                GameToMenu();
                return;
            }
            if (AutoAccel) bike.Input(1, 0);
            else rd.PollGameInput(bike);
            try
            {
                for (int i = NumLoops; i > 0; --i)
                {
                    if (moving) gameTime += 20L;
                    if (!started) started = true;
                    int n2 = bike.Step();
                    if (n2 == Bike.CRASH && !crashed)
                    {
                        crashed = true;
                        crashUntil = now + 3.0;
                        Message("Crashed", 3.0, now);
                    }
                    if (crashed && crashUntil < now)
                    {
                        Restart(true);
                        break;
                    }
                    if (n2 == Bike.STUCK)
                    {
                        Message("Crashed", 1.0, now);
                        Restart(true);
                        break;
                    }
                    if (n2 == Bike.BACK)
                    {
                        started = false;
                        gameTime = 0;
                    }
                    else if (n2 == Bike.GOAL1 || n2 == Bike.GOAL2)
                    {
                        if (n2 == Bike.GOAL2) gameTime -= 10L;
                        menu.OnFinished(gameTime / 10L);
                        return;
                    }
                    moving = n2 != Bike.BACK;
                }
            }
            catch (System.Exception)
            {
                // matches Micro.run's catch(Exception): swallow transient physics errors
            }
            bike.SaveSnapshot();
        }

        private void Message(string text, double seconds, double now)
        {
            message = text;
            msgUntil = now + seconds;
        }

        // ===== input =====

        private void MenuInput()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W)) menu.HandleAction(1);
            else if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S)) menu.HandleAction(6);
            else if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space)) menu.HandleAction(8);
            else if (Raylib.IsKeyPressed(KeyboardKey.Right) || Raylib.IsKeyPressed(KeyboardKey.D)) menu.HandleAction(5);
            else if (Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressed(KeyboardKey.A)) menu.HandleAction(2);
            else if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Backspace)) menu.Back();
        }

        // ===== render =====

        private void Draw()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.White);

            if (state == St.Splash)
            {
                rd.DrawSplash(splashPhase);
                Raylib.EndDrawing();
                return;
            }

            // bike + level background (used by both menu and play)
            bike.PrepareRender();
            rd.SetCamera(-bike.CamX() + rd.Width / 2, bike.CamY() + rd.Height / 2);
            bike.Render(rd);

            if (state == St.Play)
            {
                rd.DrawTimer(gameTime / 10L);
                if (message != null && Raylib.GetTime() < msgUntil) rd.DrawMessage(message);
                else message = null;
                rd.ProgressBar(bike.ProgressIndex(), false);
                Raylib.DrawText("[ESC] menu", 2, 2, 10, Color.DarkGray);
            }
            else // Menu
            {
                rd.DrawRaster();
                menu.Draw();
            }

            Raylib.EndDrawing();
        }

        public void Unload() => rd.Unload();
    }
}
