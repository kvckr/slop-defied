using Raylib_cs;

namespace GravityDefied
{
    // Port of class 'm': the menu state machine and game-flow controller.
    // The menu tree mirrors the original (Main / Play / Options / Help / About /
    // in-game pause / finished). Progression-unlock gating is relaxed so any
    // track/league is selectable; the rest of the flow is faithful.
    internal sealed class Menu : IMenuCallback
    {
        private readonly Game R;
        private readonly Bike bike;
        private readonly Levels lev;
        private readonly Renderer rd;
        private readonly Highscores z = new Highscores();

        private MenuList F, p, q, s, r2, al, v, w;
        private MenuList au;

        private Selector Y, aM, aZ;             // Level(group) / Track / League
        private Selector bc, a7, K, aD, H, T;    // option toggles
        private Selector startItem, goMain, exitItem, backItem, clearItem;
        private Selector contItem, restartItem, playMenuItem, nextItem;
        private MenuLink playLink, optLink, helpLink, aboutLink, hsLink;

        private readonly string[] az = { "Easy", "Medium", "Pro" };
        private readonly string[] leagues = { "100cc", "175cc", "220cc", "325cc" };
        private readonly string[] onoff = { "On", "Off" };
        private readonly string[] keysets = { "Keyset 1", "Keyset 2", "Keyset 3" };

        private bool S = false;       // want-play latch
        private byte[] name = { 65, 65, 65 };
        private long pendingTime = -1;
        private string be = null;     // formatted time
        public bool bl = false;       // suppress-draw flag (pause command)

        public Menu(Game game, Bike bike, Levels lev, Renderer rd)
        {
            R = game;
            this.bike = bike;
            this.lev = lev;
            this.rd = rd;
            Build();
        }

        public bool ConsumeStart()   // m.k()
        {
            if (S) { S = false; return true; }
            return false;
        }

        public MenuList CurrentMenu => au;
        public int Group() => Y.Value();
        public int Track() => aM.Value();
        public int League() => aZ.Value();

        private MenuList NewMenu(string title, MenuList parent)
            => new MenuList(title, parent, rd.Width, rd.Height);

        private void Build()
        {
            int W = rd.Width;

            F = NewMenu("Main", null);
            p = NewMenu("Play", F);
            q = NewMenu("Options", F);
            s = NewMenu("Help", F);
            r2 = NewMenu("About", F);
            al = NewMenu("Highscore", p);
            v = NewMenu("Finished!", p);
            w = NewMenu("Ingame", p);

            // --- Play menu ---
            startItem = Leaf("Start");
            Y = new Selector("Level", 0, this, az, false, this, p, false);
            aM = new Selector("Track", 0, this, lev.Names[0], false, this, p, false);
            aZ = new Selector("League", 0, this, leagues, false, this, p, false);
            hsLink = new MenuLink("Highscore", al, this);
            goMain = Leaf("Go to Main");
            p.Add(startItem);
            p.Add(Y);
            p.Add(aM);
            p.Add(aZ);
            p.Add(hsLink);
            p.Add(goMain);

            // --- Options menu ---
            bc = new Selector("Perspective", 0, this, onoff, true, this, q, false);
            a7 = new Selector("Shadows", 0, this, onoff, true, this, q, false);
            K = new Selector("Driver sprite", 0, this, onoff, true, this, q, false);
            aD = new Selector("Bike sprite", 0, this, onoff, true, this, q, false);
            H = new Selector("Input", 0, this, keysets, false, this, q, false);
            T = new Selector("Look ahead", 0, this, onoff, true, this, q, false);
            clearItem = Leaf("Clear highscore");
            backItem = Leaf("Back");
            q.Add(bc);
            q.Add(a7);
            q.Add(K);
            q.Add(aD);
            q.Add(H);
            q.Add(T);
            q.Add(clearItem);
            q.Add(backItem);

            // --- Help / About (concise) ---
            AddText(s, "Race to the finish as fast as you can without crashing. " +
                       "Lean with LEFT/RIGHT, accelerate with UP, brake with DOWN. " +
                       "Land on both wheels.");
            s.Add(Leaf("Back"));
            AddText(r2, "Gravity Defied - Trial Racing. " +
                        "Original by Codebrew Software, 2004. .NET 10 / raylib-cs port.");
            r2.Add(Leaf("Back"));

            // --- Main menu ---
            playLink = new MenuLink("Play", p, this);
            optLink = new MenuLink("Options", q, this);
            helpLink = new MenuLink("Help", s, this);
            aboutLink = new MenuLink("About", r2, this);
            exitItem = Leaf("Exit Game");
            F.Add(playLink);
            F.Add(optLink);
            F.Add(helpLink);
            F.Add(aboutLink);
            F.Add(exitItem);

            // --- In-game pause menu ---
            contItem = Leaf("Continue");
            restartItem = Leaf("Restart");
            playMenuItem = Leaf("Play Menu");
            w.Add(contItem);
            w.Add(restartItem);
            w.Add(new MenuLink("Options", q, this));
            w.Add(playMenuItem);
            w.Add(Leaf("Exit Game"));

            // apply initial option state
            ApplyOptions();
            au = F;
            F.ResetTop();
        }

        private Selector Leaf(string label) => new Selector(label, 0, this, null, false, this, F, true);

        private void AddText(MenuList m, string text)
        {
            Label[] ls = Label.Wrap(text, rd.Width - 16);
            foreach (Label l in ls) m.Add(l);
        }

        private void ApplyOptions()
        {
            Levels.Perspective = bc.Value() == 0;
            Levels.Shading = a7.Value() == 0;
            bike.SetSpriteFlags(SpriteFlags());
            bike.SetCamSmooth(T.Value() == 0);
            rd.SetKeyset(H.Value());
        }

        // m.j() : sprite bitmask
        public int SpriteFlags()
        {
            int n = 0;
            if (K.Value() == 0) n |= 2;   // driver sprite
            if (aD.Value() == 0) n |= 1;  // bike sprite
            return n;
        }

        // ===== IMenuCallback =====
        public MenuList Current() => au;

        public void Navigate(MenuList target, bool keep)
        {
            if (target == p)
            {
                aM.SetOptions(lev.Names[Y.Value()]);
                aM.SetValue(Track());
            }
            else if (target == al)
            {
                BuildHighscore(aZ.Value());
            }
            if (target == F || target == p)
            {
                bike.Pause();   // reset + auto-balance in the menu background
            }
            au = target;
            if (au != null && !keep) au.ResetTop();
            bl = false;
        }

        public void ItemActivated(IMenuItem item)
        {
            if (item == startItem)
            {
                lev.SelectLevel(Y.Value(), aM.Value());
                bike.SetLeague(aZ.Value());
                S = true;
                R.MenuToGame();
                return;
            }
            if (item == Y)
            {
                aM.SetOptions(lev.Names[Y.Value()]);
                aM.SetValue(0);
                return;
            }
            if (item == aM || item == aZ) return;
            if (item == bc)
            {
                bool on = bc.Value() == 0;
                bike.ShiftY(on);
                Levels.Perspective = on;
                return;
            }
            if (item == a7) { Levels.Shading = a7.Value() == 0; return; }
            if (item == K || item == aD) { bike.SetSpriteFlags(SpriteFlags()); return; }
            if (item == H) { rd.SetKeyset(H.Value()); return; }
            if (item == T) { bike.SetCamSmooth(T.Value() == 0); return; }
            if (item == clearItem)
            {
                z.ClearAll();
                return;
            }
            if (item == backItem || item == goMain)
            {
                Navigate(item == goMain ? F : au.Parent, false);
                return;
            }
            if (item == exitItem) { R.Exit(); return; }
            if (item == contItem) { R.MenuToGame(); return; }
            if (item == restartItem) { S = true; R.MenuToGame(); return; }
            if (item == playMenuItem) { Navigate(p, false); return; }
            if (item == nextItem)
            {
                if (aM.Value() < aM.MaxIndex()) aM.SetValue(aM.Value() + 1);
                lev.SelectLevel(Y.Value(), aM.Value());
                bike.SetLeague(aZ.Value());
                S = true;
                R.MenuToGame();
                return;
            }
        }

        // ===== input =====
        public void HandleAction(int action)
        {
            if (au == null) return;
            switch (action)
            {
                case 1: au.Up(); break;       // UP
                case 6: au.Down(); break;     // DOWN
                case 8: au.Activate(1); break;// FIRE
                case 5:
                    au.Activate(2);
                    if (au == al) { ShiftHs(1); }
                    break;
                case 2:
                    au.Activate(3);
                    if (au == al) { ShiftHs(-1); }
                    break;
            }
        }

        private int hsLeague = 0;
        private void ShiftHs(int dir)
        {
            hsLeague += dir;
            if (hsLeague < 0) hsLeague = 0;
            if (hsLeague > aZ.MaxIndex()) hsLeague = aZ.MaxIndex();
            BuildHighscore(hsLeague);
        }

        public void Back()
        {
            if (au == w) { R.MenuToGame(); return; }
            if (au != null && au.Parent != null) Navigate(au.Parent, true);
        }

        // ===== highscore / finish screens =====
        private void BuildHighscore(int league)
        {
            hsLeague = league;
            al.Clear();
            z.Open(Y.Value(), aM.Value());
            al.Add(new Label(lev.Name(Y.Value(), aM.Value())));
            al.Add(new Label("LEAGUE: " + leagues[league]));
            string[] arr = z.Strings(league);
            bool any = false;
            for (int i = 0; i < arr.Length; ++i)
            {
                if (arr[i] == null) continue;
                any = true;
                al.Add(new Label("" + (i + 1) + "." + arr[i]));
            }
            z.Close();
            if (!any) al.Add(new Label("No Highscores"));
            al.Add(Leaf("Back"));
            al.ResetTop();
        }

        // called by Game when the player finishes the level
        public void OnFinished(long timeTenths)
        {
            pendingTime = timeTenths;
            be = FormatTime(timeTenths);
            z.Open(Y.Value(), aM.Value());
            z.Insert(aZ.Value(), name, timeTenths);
            z.Save();

            v.Clear();
            v.Add(new Label("Time: " + be));
            string[] arr = z.Strings(aZ.Value());
            for (int i = 0; i < arr.Length; ++i)
                if (arr[i] != null) v.Add(new Label("" + (i + 1) + "." + arr[i]));
            z.Close();
            restartItem = Leaf("Restart");
            nextItem = Leaf(aM.Value() < aM.MaxIndex() ? "Next Track" : "Replay");
            v.Add(nextItem);
            v.Add(restartItem);
            v.Add(playMenuItem);
            v.ResetTop();
            au = v;
            R.GameToMenuFinished();
        }

        private static string FormatTime(long l2)
        {
            int sec = (int)(l2 / 100L);
            int cs = (int)(l2 % 100L);
            string s = sec / 60 < 10 ? " 0" + sec / 60 : " " + sec / 60;
            s += sec % 60 < 10 ? ":0" + sec % 60 : ":" + sec % 60;
            s += cs < 10 ? ".0" + cs : "." + cs;
            return s;
        }

        // called when entering the in-game pause menu
        public void ShowIngame() { au = w; w.ResetTop(); bl = false; }

        public void Draw()
        {
            au?.Draw();
        }
    }
}
