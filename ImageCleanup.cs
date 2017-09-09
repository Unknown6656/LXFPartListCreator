using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Globalization;
using System.Drawing;
using System;

namespace LXF
{
    using static Math;

    public static unsafe class ImageCleanup
    {
        public static Bitmap RemoveEdges(this Bitmap bmp)
        {
            const int BORDER = 2;
            (int w, int h) = (bmp.Width, bmp.Height);
            (int wx, int hx) = (w + 2 * BORDER, h + 2 * BORDER);
            Bitmap src = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            Bitmap dst = new Bitmap(wx, hx, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(src))
                g.DrawImage(bmp, 0, 0, w, h);

            BitmapData dsrc = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData ddst = dst.LockBits(new Rectangle(0, 0, wx, hx), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            ARGB* pdst = (ARGB*)ddst.Scan0;
            ARGB* psrc = (ARGB*)dsrc.Scan0;
            ARGB* ps, pd;

            float[] m1 = new float[9]
            {
                1, 0, -1,
                2, 0, -2,
                1, 0, -1,
            };
            float[] m2 = new float[9]
            {
                1, 2, 1,
                0, 0, 0,
                -1, -2, -1,
            };
            float v1, v2;

            for (int y = 0, x, _x, _y; y < hx; ++y)
                for (x = 0; x < wx; ++x)
                    if (insidebox(x, y, BORDER, BORDER, wx - BORDER, hx - BORDER))
                    {
                        _x = x - BORDER;
                        _y = y - BORDER;
                        v1 = v2 = 0;
                        pd = pdst + y * wx + x;

                        for (int i = 0, j, ndx; i < 3; ++i)
                            for (j = 0; j < 3; ++j)
                            {
                                ps = psrc + Min(Max(_y + i - 1, 0), h - 1) * w + Min(Max(_x + j - 1, 0), w - 1);
                                ndx = i * 3 + j;

                                v1 += m1[ndx] * ps->Gray;
                                v2 += m2[ndx] * ps->Gray;
                            }

                        pd->A = 0xff;
                        pd->R =
                        pd->G =
                        pd->B = (byte)Min(255, Max(0, Sqrt(v1 * v1 + v2 * v2)));
                    }
                    else
                        pdst[y * wx + x] = 0xff000000u;

            byte[,] iα = new byte[wx, hx];
            double diag = Sqrt(wx * wx + hx * hx) / 2;
            const byte α_THRESHOLD = 20;

            for (double θ = 0, rs = 1 / diag, θs = Atan(rs) / 3; θ < PI * 2; θ += θs)
                for (double r = 1; r >= 0; r -= rs)
                {
                    int x = (int)(w / 2d + Sin(θ) * r * diag);
                    int y = (int)(h / 2d + Cos(θ) * r * diag);

                    if (insidebox(x, y, 1, 1, wx - 1, hx - 1))
                    {
                        ARGB* px = pdst + y * wx + x;

                        if (px->Gray >= α_THRESHOLD)
                        {
                            iα[x, y] = 0;

                            break;
                        }
                        else
                            iα[x, y] = (byte)(255f * Max(0, α_THRESHOLD - px->Gray) / α_THRESHOLD);
                    }
                    else if (insidebox(x, y, 0, 0, wx, hx))  // ignore 1px border
                        iα[x, y] = 0xff;
                }

            for (int y = 0; y < hx; ++y)
                for (int x = 0; x < wx; ++x)
                {
                    ARGB px = insidebox(x, y, BORDER, BORDER, wx - BORDER, hx - BORDER) ? psrc[(y - BORDER) * w + (x - BORDER)] : (ARGB)0x00ffffffu;

                    px.A = (byte)(0xff - iα[x, y]);

                    pdst[y * wx + x] = px;
                }

            dst.UnlockBits(ddst);
            src.UnlockBits(dsrc);

            return dst;

            bool insidebox(int px, int py, int x0, int y0, int xm, int ym) => (px >= x0) && (py >= y0) && (px < xm) && (py < ym);
        }
    }

    [Serializable, StructLayout(LayoutKind.Sequential, Size = 4, Pack = 1), NativeCppClass]
    internal struct ARGB
    {
        public byte B;
        public byte G;
        public byte R;
        public byte A;

        public float Gray => (R + G + B) / 3f;

        public override string ToString() => $"#{A:x2}{R:x2}{G:x2}{B:x2}";


        public static implicit operator string(ARGB col) => col.ToString();

        public static implicit operator ARGB(string str) => uint.Parse(str.Replace("#", ""), NumberStyles.HexNumber);

        public static implicit operator uint(ARGB col) =>
            (uint)(col.A << 24) |
            (uint)(col.R << 16) |
            (uint)(col.G << 8) |
            col.B;

        public static implicit operator ARGB(uint hex) =>
            new ARGB
            {
                A = (byte)((hex >> 24) & 0xff),
                R = (byte)((hex >> 16) & 0xff),
                G = (byte)((hex >> 8) & 0xff),
                B = (byte)(hex & 0xff),
            };
    }
}
