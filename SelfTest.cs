using System;

namespace GravityDefied
{
    // Headless validation of the physics/level port (no raylib/GL needed).
    // Steps the bike across every level and league, accelerating, and reports
    // whether each level can be driven/finished without the simulation throwing.
    internal static class SelfTest
    {
        public static int Run()
        {
            Game.InMenu = false;
            Levels lev = new Levels();
            Bike bike = new Bike(lev);
            bike.SetMode(1);

            int totalLevels = 0, finished = 0, crashed = 0, errors = 0;
            long worstProgress0 = long.MaxValue;

            for (int group = 0; group < 3; ++group)
            {
                int count = lev.Names[group].Length;
                for (int level = 0; level < count; ++level)
                {
                    int league = group;   // pair league to difficulty for variety
                    lev.SelectLevel(group, level);
                    bike.SetLeague(league);
                    bike.ResetState(true);
                    bike.Unpause();
                    totalLevels++;

                    int startX = bike.aa[0].Px;
                    int maxX = startX;
                    int result = Bike.RUN;
                    int crashes = 0;
                    bool fin = false;
                    try
                    {
                        for (int step = 0; step < 6000; ++step)
                        {
                            bike.Input(1, 0);              // hold accelerate
                            result = bike.Step();
                            bike.SaveSnapshot();
                            bike.PrepareRender();
                            if (bike.aa[0].Px > maxX) maxX = bike.aa[0].Px;
                            if (result == Bike.GOAL1 || result == Bike.GOAL2) { fin = true; break; }
                            if (result == Bike.CRASH || result == Bike.STUCK)
                            {
                                crashes++;
                                if (crashes > 30) break;
                                bike.ResetState(true);
                            }
                            // BACK = behind start line: original just holds the timer, bike keeps going
                        }
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        Console.WriteLine($"  [ERR ] g{group} l{level} '{lev.Name(group, level)}': {ex.GetType().Name}: {ex.Message}");
                        continue;
                    }

                    long advanced = (long)(maxX - startX);
                    if (group == 0 && level == 0) worstProgress0 = advanced;
                    if (fin) finished++;
                    else if (crashes > 0) crashed++;
                    Console.WriteLine($"  g{group} l{level,-2} {lev.Name(group, level),-14} -> {(fin ? "FINISHED" : "ran     ")} advanced={advanced,10} crashes={crashes}");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"levels={totalLevels} finished={finished} ranWithCrashes={crashed} errors={errors}");
            Console.WriteLine(errors == 0 ? "SELFTEST OK (no exceptions in physics/level code)" : "SELFTEST had exceptions");
            return errors == 0 ? 0 : 1;
        }
    }
}
