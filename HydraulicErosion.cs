/*
 * HydraulicErosion.cs
 * 
 * Main class for Hydraulic Erosion algorithm.
 * Ported from https://github.com/henrikglass/erodr/blob/master/src/erodr.c
 * 
 * Created by Claude Sonnet 3.5 under supervision by Nomad1
 */

using System.Numerics;

namespace ErodrSharp
{
    public static class HydraulicErosion
    {
        private struct Particle
        {
            public Vector2 Pos;
            public Vector2 Dir;
            public float Vel;
            public float Sediment;
            public float Water;
        }

        public static void SimulateParticles(Image hmap, SimParams parameters)
        {
            Random rand = new Random();

            for (int i = 0; i < parameters.N; i++)
            {
                Particle p = new Particle
                {
                    Pos = new Vector2((float)(rand.NextDouble() * (hmap.Width - 1)), (float)(rand.NextDouble() * (hmap.Height - 1))),
                    Dir = Vector2.Zero,
                    Vel = 0f,
                    Sediment = 0f,
                    Water = 1f
                };

                for (int j = 0; j < parameters.Ttl; j++)
                {
                    Vector2 posOld = p.Pos;
                    Vector3 hg = HeightGradientAt(hmap, posOld);
                    Vector2 g = new Vector2(hg.X, hg.Y);
                    float hOld = hg.Z;

                    p.Dir = Vector2.Subtract(
                        Vector2.Multiply(parameters.PEnertia, p.Dir),
                        Vector2.Multiply(1f - parameters.PEnertia, g)
                    );

                    p.Dir = Vector2.Normalize(p.Dir);

                    p.Pos = posOld + p.Dir;

                    if (p.Pos.X > (hmap.Width - 1) || p.Pos.X < 0 ||
                            p.Pos.Y > (hmap.Height - 1) || p.Pos.Y < 0)
                        break;

                    float hNew = BilInterpolateMapFloat(hmap, p.Pos);
                    float hDiff = hNew - hOld;

                    float c = MathF.Max(-hDiff, parameters.PMinSlope) * p.Vel * p.Water * parameters.PCapacity;

                    if (hDiff > 0 || p.Sediment > c)
                    {
                        float toDeposit = (hDiff > 0) ?
                            Math.Min(p.Sediment, hDiff) :
                            (p.Sediment - c) * parameters.PDeposition;
                        p.Sediment -= toDeposit;
                        Deposit(hmap, posOld, toDeposit);
                    }
                    else
                    {
                        float toErode = Math.Min((c - p.Sediment) * parameters.PErosion, -hDiff);
                        p.Sediment += toErode;
                        Erode(hmap, posOld, toErode, parameters.PRadius);
                    }

                    float sqVel = p.Vel * p.Vel + hDiff * parameters.PGravity;
                    p.Vel = sqVel < 0 ? float.MaxValue : MathF.Sqrt(sqVel);
                    p.Water *= (1f - parameters.PEvaporation);
                }

                if ((i + 1) % 10000 == 0)
                    Console.WriteLine("Particles simulated: {0}", i + 1);
            }
        }

        private static void Erode(Image hmap, Vector2 pos, float amount, int radius)
        {
            if (radius < 1)
            {
                Deposit(hmap, pos, -amount);
                return;
            }

            int x0 = (int)(pos.X - radius);
            int y0 = (int)(pos.Y - radius);
            int xStart = Math.Max(0, x0);
            int yStart = Math.Max(0, y0);
            int xEnd = Math.Min(hmap.Width, x0 + 2 * radius + 1);
            int yEnd = Math.Min(hmap.Height, y0 + 2 * radius + 1);

            float[,] kernel = new float[2 * radius + 1, 2 * radius + 1];
            float kernelSum = 0f;

            for (int y = yStart; y < yEnd; y++)
            {
                for (int x = xStart; x < xEnd; x++)
                {
                    float dX = x - pos.X;
                    float dY = y - pos.Y;
                    float distance = MathF.Sqrt(dX * dX + dY * dY);
                    float w = MathF.Max(0f, radius - distance);
                    kernelSum += w;
                    kernel[y - y0, x - x0] = w;
                }
            }

            if (kernelSum > 0f)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    for (int x = xStart; x < xEnd; x++)
                    {
                        kernel[y - y0, x - x0] /= kernelSum;
                        int idx = y * hmap.Width + x;
                        hmap.Buffer[idx] = MathF.Max(0f, hmap.Buffer[idx] - amount * kernel[y - y0, x - x0]);
                    }
                }
            }
        }

        private static void Deposit(Image hmap, Vector2 pos, float amount)
        {
            int xI = (int)pos.X;
            int yI = (int)pos.Y;
            float u = pos.X - xI;
            float v = pos.Y - yI;

            int idx = yI * hmap.Width + xI;
            hmap.Buffer[idx] += amount * (1f - u) * (1f - v);

            if (xI + 1 < hmap.Width)
            {
                hmap.Buffer[idx + 1] += amount * u * (1f - v);
            }

            if (yI + 1 < hmap.Height)
            {
                hmap.Buffer[idx + hmap.Width] += amount * (1f - u) * v;

                if (xI + 1 < hmap.Width)
                {
                    hmap.Buffer[idx + hmap.Width + 1] += amount * u * v;
                }
            }
        }

        private static Vector3 HeightGradientAt(Image hmap, Vector2 pos)
        {
            int xI = (int)pos.X;
            int yI = (int)pos.Y;
            float u = pos.X - xI;
            float v = pos.Y - yI;

            Vector2 ul = GradientAt(hmap, xI, yI);
            Vector2 ur = GradientAt(hmap, Math.Min(xI + 1, hmap.Width - 1), yI);
            Vector2 ll = GradientAt(hmap, xI, Math.Min(yI + 1, hmap.Height - 1));
            Vector2 lr = GradientAt(hmap, Math.Min(xI + 1, hmap.Width - 1), Math.Min(yI + 1, hmap.Height - 1));

            Vector2 iplL = Vector2.Lerp(ul, ll, v);
            Vector2 iplR = Vector2.Lerp(ur, lr, v);

            Vector2 gradient = Vector2.Lerp(iplL, iplR, u);
            float height = BilInterpolateMapFloat(hmap, pos);

            return new Vector3(gradient, height);
        }

        private static float BilInterpolateMapFloat(Image map, Vector2 pos)
        {
            int xI = (int)pos.X;
            int yI = (int)pos.Y;
            float u = pos.X - xI;
            float v = pos.Y - yI;

            float ul = map.Buffer[yI * map.Width + xI];
            float ur = map.Buffer[yI * map.Width + Math.Min(xI + 1, map.Width - 1)];
            float ll = map.Buffer[Math.Min(yI + 1, map.Height - 1) * map.Width + xI];
            float lr = map.Buffer[Math.Min(yI + 1, map.Height - 1) * map.Width + Math.Min(xI + 1, map.Width - 1)];

            float iplL = (1f - v) * ul + v * ll;
            float iplR = (1f - v) * ur + v * lr;

            return (1f - u) * iplL + u * iplR;
        }

        private static Vector2 GradientAt(Image hmap, int x, int y)
        {
            int idx = y * hmap.Width + x;
            int right = idx + ((x > hmap.Width - 2) ? 0 : 1);
            int below = idx + ((y > hmap.Height - 2) ? 0 : hmap.Width);

            return new Vector2(
                hmap.Buffer[right] - hmap.Buffer[idx],
                hmap.Buffer[below] - hmap.Buffer[idx]
            );
        }

        public static bool MaybeClamp(Image img)
        {
            bool clamped = false;
            for (int i = 0; i < img.Width * img.Height; i++)
            {
                if (img.Buffer[i] < 0)
                {
                    img.Buffer[i] = 0;
                    clamped = true;
                } else if (img.Buffer[i] > 1)
                {
                    img.Buffer[i] = 1;
                    clamped = true;
                }
            }
            return clamped;
        }
    }
}