/*
 * Program.cs
 * 
 * Main class for Hydraulic Erosion app.
 * Ported from https://github.com/henrikglass/erodr/blob/master/src/erodr.c
 * 
 * Created by Claude Sonnet 3.5 under supervision by Nomad1
 */

namespace ErodrSharp
{
    public static class Program
    {
        public static bool ParseArgs(string[] args, out string inputFilePath, out string outputFilePath, out SimParams parameters, out bool asciiEncoding)
        {
            parameters = new SimParams
            {
                N = 70000,
                Ttl = 30,
                PRadius = 2,
                PEnertia = 0.1f,
                PCapacity = 10f,
                PGravity = 4f,
                PEvaporation = 0.1f,
                PErosion = 0.1f,
                PDeposition = 1f,
                PMinSlope = 0.0001f
            };

            inputFilePath = string.Empty;
            outputFilePath = "output.pgm";
            asciiEncoding = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-f":
                        if (i + 1 < args.Length) inputFilePath = args[++i];
                        break;
                    case "-o":
                        if (i + 1 < args.Length) outputFilePath = args[++i];
                        break;
                    case "-a":
                        asciiEncoding = true;
                        break;
                    case "-t":
                        if (i + 1 < args.Length) parameters.Ttl = int.Parse(args[++i]);
                        break;
                    case "-n":
                        if (i + 1 < args.Length) parameters.N = int.Parse(args[++i]);
                        break;
                    case "-r":
                        if (i + 1 < args.Length) parameters.PRadius = int.Parse(args[++i]);
                        break;
                    case "-e":
                        if (i + 1 < args.Length) parameters.PEnertia = float.Parse(args[++i]);
                        break;
                    case "-c":
                        if (i + 1 < args.Length) parameters.PCapacity = float.Parse(args[++i]);
                        break;
                    case "-g":
                        if (i + 1 < args.Length) parameters.PGravity = float.Parse(args[++i]);
                        break;
                    case "-v":
                        if (i + 1 < args.Length) parameters.PEvaporation = float.Parse(args[++i]);
                        break;
                    case "-s":
                        if (i + 1 < args.Length) parameters.PErosion = float.Parse(args[++i]);
                        break;
                    case "-d":
                        if (i + 1 < args.Length) parameters.PDeposition = float.Parse(args[++i]);
                        break;
                    case "-m":
                        if (i + 1 < args.Length) parameters.PMinSlope = float.Parse(args[++i]);
                        break;
                    default:
                        Console.WriteLine($"Unknown argument: {args[i]}");
                        return false;
                }
            }

            return !string.IsNullOrEmpty(inputFilePath);
        }

        // Updated Main method to use ParseArgs
        public static void Main(string[] args)
        {
            string filepath;
            string outputFilepath;
            bool asciiOut;

            SimParams parameters;

            if (!ParseArgs(args, out filepath, out outputFilepath, out parameters, out asciiOut))
            {
                Console.WriteLine("Usage: {0} -f file [-options]", Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location));
                Console.WriteLine("Simulation options:");
                Console.WriteLine(" -n ## \t\t Number of particles to simulate (default: 70'000)");
                Console.WriteLine(" -t ## \t\t Maximum lifetime of a particle (default: 30)");
                Console.WriteLine(" -g ## \t\t Gravitational constant (default: 4)");
                Console.WriteLine(" -r ## \t\t Particle erosion radius (default: 2)");
                Console.WriteLine(" -e ## \t\t Particle enertia coefficient (default: 0.1)");
                Console.WriteLine(" -c ## \t\t Particle capacity coefficient (default: 10)");
                Console.WriteLine(" -v ## \t\t Particle evaporation rate (default: 0.1)");
                Console.WriteLine(" -s ## \t\t Particle erosion coefficient (default: 0.1)");
                Console.WriteLine(" -d ## \t\t Particle deposition coefficient (default: 1.0)");
                Console.WriteLine(" -m ## \t\t Minimum slope (default: 0.0001)");
                Console.WriteLine("Other options:");    
                Console.WriteLine(" -o <file> \t Place the output into <file>");
                Console.WriteLine(" -a \t\t Output is ASCII encoded");
                return;
            }

            Image heightmap;

            try
            {
                heightmap  = new Image(filepath);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error loading file {0}: {1}", filepath, ex);
                return;
            }

            HydraulicErosion.SimulateParticles(heightmap, parameters);

            if (HydraulicErosion.MaybeClamp(heightmap))
                Console.WriteLine("Warning: Output image was clipping. Results have been clamped to [0, 1]");

            Console.WriteLine("Simulation complete.");

            heightmap.Save(outputFilepath, asciiOut);
        }
    }
}