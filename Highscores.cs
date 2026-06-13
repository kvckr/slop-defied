using System;
using System.Collections.Generic;
using System.IO;

namespace GravityDefied
{
    // Port of class 'a': per-track best times. The original used MIDP RecordStore;
    // here the same data model (4 leagues x 3 places of time+name) is persisted to
    // a local file, keyed by "<group><track>".
    internal sealed class Highscores
    {
        private long[][] c = new long[4][];          // times
        private byte[][][] d = new byte[4][][];       // names (3 chars)
        private string key = null;

        private static readonly Dictionary<string, byte[]> store = new Dictionary<string, byte[]>();
        private static bool loaded = false;
        private static readonly string FilePath =
            Path.Combine(AppContext.BaseDirectory, "highscores.dat");

        public Highscores()
        {
            for (int i = 0; i < 4; ++i)
            {
                c[i] = new long[3];
                d[i] = new byte[3][];
                for (int k = 0; k < 3; ++k) d[i][k] = new byte[3];
            }
            LoadFile();
        }

        // a.a(int,int) : open the store for (group, track)
        public void Open(int group, int track)
        {
            Zero();
            key = "" + group + track;
            if (store.TryGetValue(key, out byte[] buf)) Decode(buf);
        }

        // a.a() : close (no-op for file store)
        public void Close() { }

        private void Zero()
        {
            for (int i = 0; i < 4; ++i)
                for (int k = 0; k < 3; ++k)
                {
                    c[i][k] = 0L;
                    d[i][k] = new byte[3];
                }
        }

        // a.a(int,long) : place (0..2) for a time, or 3 if it doesn't qualify
        public int Place(int league, long time)
        {
            for (int i = 0; i < 3; ++i)
            {
                if (c[league][i] <= time && c[league][i] != 0L) continue;
                return i;
            }
            return 3;
        }

        // a.a(int,byte[],long) : insert a new time
        public void Insert(int league, byte[] name, long time)
        {
            int pos = Place(league, time);
            if (pos != 3)
            {
                if (time > 0xFFFF28L) time = 0xFFFF28L;
                Shift(league, pos);
                c[league][pos] = time;
                for (int i = 0; i < 3; ++i) d[league][pos][i] = name[i];
            }
        }

        private void Shift(int league, int pos)
        {
            for (int i = 2; i > pos; --i)
            {
                c[league][i] = c[league][i - 1];
                Array.Copy(d[league][i - 1], 0, d[league][i], 0, 3);
            }
        }

        // a.a(int) : formatted strings for a league
        public string[] Strings(int league)
        {
            string[] outArr = new string[3];
            for (int i = 0; i < 3; ++i)
            {
                if (c[league][i] != 0L)
                {
                    int n3 = (int)(c[league][i] / 100);
                    int n4 = (int)(c[league][i] % 100);
                    string s = "" + System.Text.Encoding.Latin1.GetString(d[league][i]) + " ";
                    s += n3 / 60 < 10 ? " 0" + n3 / 60 : " " + n3 / 60;
                    s += n3 % 60 < 10 ? ":0" + n3 % 60 : ":" + n3 % 60;
                    s += n4 < 10 ? ".0" + n4 : "." + n4;
                    outArr[i] = s;
                }
                else outArr[i] = null;
            }
            return outArr;
        }

        // a.cfr_renamed_4() : save current track
        public void Save()
        {
            if (key == null) return;
            store[key] = Encode();
            SaveFile();
        }

        // a.cfr_renamed_10() : clear all
        public void ClearAll()
        {
            store.Clear();
            SaveFile();
        }

        private byte[] Encode()
        {
            byte[] b = new byte[4 * 3 * (8 + 3)];
            int p = 0;
            for (int i = 0; i < 4; ++i)
                for (int k = 0; k < 3; ++k)
                {
                    long v = c[i][k];
                    for (int s = 0; s < 8; ++s) { b[p++] = (byte)(v & 0xFF); v >>= 8; }
                    for (int s = 0; s < 3; ++s) b[p++] = d[i][k][s];
                }
            return b;
        }

        private void Decode(byte[] b)
        {
            int p = 0;
            for (int i = 0; i < 4; ++i)
                for (int k = 0; k < 3; ++k)
                {
                    long v = 0;
                    for (int s = 0; s < 8; ++s) v |= ((long)b[p++] & 0xFF) << (8 * s);
                    c[i][k] = v;
                    for (int s = 0; s < 3; ++s) d[i][k][s] = b[p++];
                }
        }

        private static void LoadFile()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                if (!File.Exists(FilePath)) return;
                using var br = new BinaryReader(File.OpenRead(FilePath));
                int n = br.ReadInt32();
                for (int i = 0; i < n; ++i)
                {
                    string k = br.ReadString();
                    int len = br.ReadInt32();
                    store[k] = br.ReadBytes(len);
                }
            }
            catch { }
        }

        private static void SaveFile()
        {
            try
            {
                using var bw = new BinaryWriter(File.Create(FilePath));
                bw.Write(store.Count);
                foreach (var kv in store)
                {
                    bw.Write(kv.Key);
                    bw.Write(kv.Value.Length);
                    bw.Write(kv.Value);
                }
            }
            catch { }
        }
    }
}
