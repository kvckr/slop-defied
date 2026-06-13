using System.Collections.Generic;
using Raylib_cs;

namespace GravityDefied
{
    // Port of class 'e': a scrollable, selectable list of menu items.
    // Navigation (cfr_renamed_9/cfr_renamed_13/cfr_renamed_7/cfr_renamed_1) is
    // transcribed faithfully, including the skip-non-selectable-item logic.
    internal sealed class MenuList
    {
        private MenuList parent;        // cfr_renamed_9
        public string Title;            // h
        public int Sel = -1;            // cfr_renamed_10
        private List<IMenuItem> items;  // l
        private int e2 = 0;             // window top
        private int d2 = 0;             // window bottom
        private int w = 13;             // visible lines
        private readonly int screenW;
        private readonly int screenH;
        private int left;               // cfr_renamed_8
        private int top = 1;            // cfr_renamed_3

        public MenuList(string title, MenuList parent)
            : this(title, parent, 480, 320) { }

        public MenuList(string title, MenuList parent, int sw, int sh)
        {
            Title = title;
            Sel = -1;
            items = new List<IMenuItem>();
            this.parent = parent;
            screenW = sw;
            screenH = sh;
            left = sw <= 100 ? 6 : 9;
            int avail = (sh - 2 - 10 - MenuTheme.TitleH);
            w = avail / (MenuTheme.LineH + 2);
            if (w > 13) w = 13;
        }

        public MenuList Parent => parent;          // a()
        public void SetParent(MenuList e) => parent = e;
        public int Selected() => Sel;

        // e.a(j) : add an item, recompute window size
        public void Add(IMenuItem item)
        {
            items.Add(item);
            int n2 = MenuTheme.TitleH + 4;
            w = 1;
            for (int i = 0; i < items.Count - 1; ++i)
            {
                n2 += MenuTheme.LineH + 2;
                if (n2 > screenH - 2 - 10) break;
                ++w;
            }
            if (w > 13) w = 13;
            ResetTop();
        }

        public void Clear()   // e.cfr_renamed_4()
        {
            items.Clear();
            e2 = 0;
            d2 = 0;
            Sel = -1;
        }

        public int Count => items.Count;

        // e.cfr_renamed_7() : reset selection to first selectable
        public void ResetTop()
        {
            Sel = 0;
            for (int i = 0; i < items.Count && i < w; ++i)
            {
                if (!items[i].Selectable()) continue;
                Sel = i;
                break;
            }
            e2 = 0;
            d2 = items.Count - 1;
            if (d2 > w - 1) d2 = w - 1;
        }

        // e.cfr_renamed_1() : select last selectable
        public void ResetBottom()
        {
            Sel = items.Count - 1;
            for (int i = items.Count - 1; i > 0; --i)
            {
                if (!items[i].Selectable()) continue;
                Sel = i;
                break;
            }
            e2 = items.Count - w;
            if (e2 < 0) e2 = 0;
            d2 = items.Count - 1;
            if (d2 > Sel + w) d2 = Sel + w;
        }

        // e.cfr_renamed_9() : move selection down
        public void Down()
        {
            if (items.Count == 0) return;
            if (Sel < 0 || Sel >= items.Count || !items[Sel].Selectable())
            {
                ++d2; Sel = d2; ++e2; Clamp(); return;
            }
            ++Sel;
            if (Sel > items.Count - 1) { ResetTop(); return; }
            bool found = false;
            int n3 = Sel;
            for (n3 = Sel; n3 <= d2 + 1 && n3 < items.Count; ++n3)
            {
                if (!items[n3].Selectable()) continue;
                found = true;
                break;
            }
            if (found) Sel = n3;
            else if (d2 < items.Count - 1) { ++d2; ++e2; }
            else --Sel;
            if (Sel > d2)
            {
                ++e2; ++d2;
                if (d2 > items.Count - 1) d2 = items.Count - 1;
                Sel = d2;
            }
            Clamp();
        }

        // e.cfr_renamed_13() : move selection up
        public void Up()
        {
            if (items.Count == 0) return;
            --Sel;
            if (Sel < 0) { ResetBottom(); return; }
            bool found = false;
            int n3 = Sel;
            for (n3 = Sel; n3 >= e2 && n3 >= 0; --n3)
            {
                if (!items[n3].Selectable()) continue;
                found = true;
                break;
            }
            if (!found)
            {
                if (e2 > 0) { --e2; if (items.Count > w - 1) --d2; }
                else ResetBottom();
                Clamp();
                return;
            }
            Sel = n3;
            if (Sel < e2)
            {
                --e2;
                if (e2 < 0) { Sel = 0; e2 = 0; }
                if (items.Count > w - 1) --d2;
            }
            Clamp();
        }

        private void Clamp()
        {
            if (e2 < 0) e2 = 0;
            if (d2 > items.Count - 1) d2 = items.Count - 1;
            if (Sel < 0) Sel = 0;
            if (Sel > items.Count - 1) Sel = items.Count - 1;
        }

        // e.cfr_renamed_10(int) : activate current item with action
        public void Activate(int action)
        {
            if (Sel == -1) return;
            for (int i = Sel; i < items.Count; ++i)
            {
                IMenuItem it = items[i];
                if (it == null || !it.Selectable()) continue;
                it.Activate(action);
                return;
            }
        }

        // e.cfr_renamed_11(int) : pre-scroll to a given index
        public void ScrollTo(int n2)
        {
            ResetTop();
            while (Sel < n2 && Sel < items.Count - 1)
            {
                ++Sel;
                if (Sel <= d2) continue;
                ++e2; ++d2;
            }
        }

        // e.a(Graphics) : render
        public void Draw()
        {
            int y = top;
            if (!string.IsNullOrEmpty(Title))
            {
                Raylib.DrawText(Title, left, y, MenuTheme.TitleH - 4, Color.Black);
                y += MenuTheme.TitleH;
            }
            if (e2 > 0)
                Raylib.DrawText("^", left - 6, y - 2, MenuTheme.FontSize, Color.Black);
            Clamp();
            for (int i = e2; i <= d2 && i < items.Count; ++i)
            {
                if (i == Sel && items[i].Selectable())
                    Raylib.DrawRectangle(left - 4, y - 1, screenW - left - 4, MenuTheme.LineH, new Color(255, 220, 120, 160));
                items[i].Draw(left + 7, y);
                y += MenuTheme.LineH + 2;
            }
            if (items.Count > d2 + 1 && d2 != items.Count - 1)
                Raylib.DrawText("v", left - 6, y - 2, MenuTheme.FontSize, Color.Black);
        }
    }
}
