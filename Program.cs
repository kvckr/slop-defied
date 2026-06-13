using Raylib_cs;

namespace GravityDefied
{
    internal static class Program
    {
        private const int Width = 480;
        private const int Height = 320;

        public static int Main(string[] args)
        {
            if (args != null && args.Length > 0 && args[0] == "selftest")
            {
                return SelfTest.Run();
            }

            bool shot = args != null && args.Length > 0 && args[0] == "shot";

            Raylib.SetConfigFlags(ConfigFlags.VSyncHint);
            Raylib.InitWindow(Width, Height, "Gravity Defied - Trial Racing");
            Raylib.SetTargetFPS(33);   // ~30ms frames, like the original
            Raylib.SetExitKey(KeyboardKey.Null);

            Game game = new Game(Width, Height);

            if (shot)
            {
                for (int f = 0; f < 110; ++f) game.Frame();          // splash -> menu
                Raylib.TakeScreenshot("shot_menu.png");
                game.DebugStart(0, 0, 0);                            // Intro
                game.AutoAccel = true;
                for (int f = 0; f < 60; ++f) game.Frame();           // drive a bit
                Raylib.TakeScreenshot("shot_game.png");
                Raylib.CloseWindow();
                return 0;
            }

            while (!Raylib.WindowShouldClose() && game.Running)
            {
                game.Frame();
            }

            game.Unload();
            Raylib.CloseWindow();
            return 0;
        }
    }
}
