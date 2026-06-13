using System.Collections.Generic;
using Raylib_cs;

namespace GravityDefied
{
    // Menu navigation callback (port of interface 'c').
    internal interface IMenuCallback
    {
        MenuList Current();                       // c.a()
        void Navigate(MenuList target, bool keep); // c.a(e,boolean)
        void ItemActivated(IMenuItem item);        // c.a(j)
    }

    // Menu item (port of interface 'j').
    internal interface IMenuItem
    {
        void SetText(string s);                    // a(String)
        void Draw(int x, int y);                   // a(Graphics,int,int)
        bool Selectable();                         // cfr_renamed_11()
        void Activate(int action);                 // a(int)
    }

    internal static class MenuTheme
    {
        public const int FontSize = 14;
        public const int LineH = 16;
        public const int TitleH = 20;
    }

    // Port of class 'h': a (wrappable) text label.
    internal sealed class Label : IMenuItem
    {
        private string b;
        private int e = 0;
        private bool a;
        private int i;
        private Color color = Color.Black;

        public Label(string text) { b = text; a = false; i = 0; }

        public void SetText(string s) => b = s;
        public bool Selectable() => false;     // h.cfr_renamed_11() = false
        public void Activate(int action) { }
        public void SetIndent(int n) => e = n;
        public void SetIcon(bool on, int idx) { a = on; i = idx; }

        public void Draw(int x, int y)
        {
            Raylib.DrawText(b, x + e, y, MenuTheme.FontSize, color);
        }

        // h.a(String, Micro) : word-wrap a long string into multiple labels
        public static Label[] Wrap(string text, int maxWidth)
        {
            List<Label> list = new List<Label>();
            int start = 0;
            while (start < text.Length)
            {
                int len = text.Length - start;
                // greedily fit words
                int fit = len;
                while (fit > 0 && Raylib.MeasureText(text.Substring(start, fit), MenuTheme.FontSize) > maxWidth - 8)
                {
                    int sp = text.LastIndexOf(' ', start + fit - 1, fit);
                    if (sp <= start) { break; }
                    fit = sp - start;
                }
                if (fit <= 0) fit = len;
                list.Add(new Label(text.Substring(start, fit).Trim()));
                start += fit;
                while (start < text.Length && text[start] == ' ') start++;
            }
            if (list.Count == 0) list.Add(new Label(""));
            return list.ToArray();
        }
    }

    // Port of the menu-link role of class 'n': navigate to a submenu.
    internal sealed class MenuLink : IMenuItem
    {
        private string g;
        private MenuList target;
        private IMenuCallback cb;

        public MenuLink(string text, MenuList target, IMenuCallback cb)
        {
            g = text + ">";
            this.target = target;
            this.cb = cb;
        }

        public void SetText(string s) => g = s + ">";
        public bool Selectable() => true;
        public void Draw(int x, int y) => Raylib.DrawText(g, x, y, MenuTheme.FontSize, Color.Black);

        // n.a(int)
        public void Activate(int action)
        {
            if (action == 1 || action == 2)
            {
                cb.ItemActivated(this);
                target.SetParent(cb.Current());
                cb.Navigate(target, false);
            }
        }
    }

    // Port of class 'g': a value selector (cycles options / opens a submenu)
    // or a leaf link inside a generated submenu.
    internal sealed class Selector : IMenuItem, IMenuCallback
    {
        private string[] t;
        public int q;                 // current value index
        private int r;                // max selectable
        private string v;             // label
        private IMenuCallback w;       // parent callback
        private MenuList k = null;     // generated submenu
        private MenuList l = null;     // parent menu
        private bool s;               // toggle (on/off) mode
        private bool o = false;        // activated flag
        private string p;             // display value
        private Menu m;
        private Selector[] n = null;
        private bool x;               // (leaf) show icon
        private bool u;               // (leaf) enabled
        private bool j;               // leaf flag

        public Selector(string label, int val, IMenuCallback cb, string[] options, bool toggle, Menu m, MenuList parent, bool leaf)
        {
            this.m = m;
            if (leaf)
            {
                j = true;
                v = label;
                w = cb;
                u = true;
                x = false;
                return;
            }
            j = false;
            v = label + ":";
            q = val;
            w = cb;
            t = options ?? new[] { "" };
            r = t.Length - 1;
            s = toggle;
            SetValue(val);
            if (toggle)
            {
                p = val == 1 ? "Off" : "On";
                return;
            }
            l = parent;
            UpdateDisplay();
            BuildSubmenu();
        }

        public void SetLeafState(bool icon, bool enabled) { x = icon; u = enabled; }

        public void SetOptions(string[] options)
        {
            t = options;
            if (q > t.Length - 1) q = t.Length - 1;
            if (r > t.Length - 1) r = t.Length - 1;
            UpdateDisplay();
            BuildSubmenu();
        }

        // g.cfr_renamed_12() : build the option-picker submenu
        public void BuildSubmenu()
        {
            k = new MenuList(v, l);
            n = new Selector[t.Length];
            for (int i = 0; i < n.Length; ++i)
            {
                n[i] = new Selector(t[i], 0, this, null, false, m, l, true);
                if (i > r) n[i].SetLeafState(true, true);
                k.Add(n[i]);
            }
        }

        public void SetParentMenu(MenuList e) => l = e;
        public void SetText(string s2) { if (j) v = s2; else v = s2 + ":"; }
        public bool Selectable() => true;

        // g.a(int)
        public void Activate(int action)
        {
            if (j)
            {
                if (action == 1) w.ItemActivated(this);
                return;
            }
            switch (action)
            {
                case 1:
                    if (s)
                    {
                        ++q; if (q > 1) q = 0;
                        p = q == 1 ? "Off" : "On";
                        w.ItemActivated(this);
                        return;
                    }
                    o = true;
                    w.ItemActivated(this);
                    return;
                case 2:
                    if (s)
                    {
                        if (q == 1) { q = 0; p = "On"; w.ItemActivated(this); }
                        return;
                    }
                    ++q;
                    if (q > t.Length - 1) q = t.Length - 1;
                    else w.ItemActivated(this);
                    UpdateDisplay();
                    return;
                case 3:
                    if (s)
                    {
                        if (q == 0) { q = 1; p = "Off"; w.ItemActivated(this); }
                        return;
                    }
                    --q;
                    if (q < 0) q = 0;
                    else { UpdateDisplay(); w.ItemActivated(this); }
                    UpdateDisplay();
                    return;
            }
        }

        private void UpdateDisplay() => p = t[q];

        public void SetUnlockLimit(int n2)   // g.cfr_renamed_11(int)
        {
            r = n2;
            if (r > t.Length - 1) r = t.Length - 1;
            if (k != null)
                for (int i = 0; i < n.Length; ++i)
                    n[i].SetLeafState(i > n2, !(i > n2));
        }

        public int UnlockLimit() => r;           // cfr_renamed_0()
        public int MaxIndex() => t.Length - 1;   // cfr_renamed_5()
        public string[] Options() => t;          // cfr_renamed_1()

        public void SetValue(int n2)             // cfr_renamed_4(int)
        {
            q = n2;
            if (q > t.Length - 1) q = 0;
            if (q < 0) q = t.Length - 1;
            UpdateDisplay();
        }

        public int Value() => q;                 // cfr_renamed_3()
        public MenuList Submenu() => k;          // a()

        public bool ConsumeActivated()           // cfr_renamed_8()
        {
            if (o) { o = false; return true; }
            return false;
        }

        public void Draw(int x2, int y)
        {
            if (j)
            {
                Raylib.DrawText(v, x2, y, MenuTheme.FontSize, Color.Black);
                return;
            }
            Raylib.DrawText(v + " " + p, x2, y, MenuTheme.FontSize, Color.Black);
        }

        // IMenuCallback (for the generated submenu's leaf links)
        public MenuList Current() => k;
        public void Navigate(MenuList target, bool keep) { }
        public void ItemActivated(IMenuItem item)  // g.a(j)
        {
            for (int i = 0; i < n.Length; ++i)
            {
                if (item != n[i]) continue;
                q = i;
                UpdateDisplay();
                break;
            }
            w.Navigate(l, true);
            w.ItemActivated(this);
        }

        public void SetCallback(IMenuCallback cb) => w = cb;  // cfr_renamed_10(e) variant
    }
}
