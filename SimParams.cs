/*
 * SimParams.cs
 * 
 * Main class for Hydraulic Erosion.
 * Ported from https://github.com/henrikglass/erodr/blob/master/src/params.h
 * 
 * Created by Claude Sonnet 3.5 under supervision by Nomad1
 */

namespace ErodrSharp
{
    public struct SimParams
    {
        public int N;
        public int Ttl;
        public int PRadius;
        public float PEnertia;
        public float PCapacity;
        public float PGravity;
        public float PEvaporation;
        public float PErosion;
        public float PDeposition;
        public float PMinSlope;
    }
}