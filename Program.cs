using System.Diagnostics;
using System.Text.RegularExpressions;
using XNCPLib.Misc;
using SharpNeedle.Ninja.Csd;
using XNCPLib.XNCP;
using Amicitia.IO.Binary;
using SharpNeedle.Utilities;
using SharpNeedle.IO;
using System.Text;

public class Program
{
    static string? ProgramPath
    {
        get
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDir = System.IO.Path.GetDirectoryName(exePath);
            return exeDir;
        }
    }
    static List<string> Textures = new List<string>();

    public static void Main(string[] args)
    {

        IntroScreenTitle("Choose an option");
        Console.WriteLine("Choose what to do:");
        Console.WriteLine("1. Endian Swapper");
        Console.WriteLine("2. GNCP -> XNCP (experimental)");

        Console.Write("Enter your choice: ");
        int option = 0;
        try { option = Convert.ToInt32(Console.ReadLine()); }
        catch (Exception ex)
        { Console.WriteLine("Not a valid choice. Closing..."); }
        if (option == 1)
        {
            IntroScreenTitle("Converts XNCP to YNCP, and YNCP to XNCP.\nCredits to Skyth and those who worked on XNCPLib\nhttps://github.com/crash5band/Shuriken/tree/master/XNCPLib");
            string path;
            if (Environment.GetCommandLineArgs().Length == 1)
            {
                Console.Write("Enter Path of the *NCP file: ");
                path = @Console.ReadLine();
            }
            else
                path = @Environment.GetCommandLineArgs()[1];
            ///Removes quotation marks since they're going to be added in the code instead, and double quotes would prob break everything
            path = path.Replace("\"", "");


            if (Path.GetExtension(@path).IndexOf(".yncp", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                FAPCFile file = new FAPCFile();
                file.Load(path);
                file.Signature = Utilities.Make4CCLE("FAPC");
                file.Resources[0].Content.Signature = Utilities.Make4CCLE("NXIF");
                file.Resources[1].Content.Signature = Utilities.Make4CCLE("NXIF");
                file.Save($@"{path.Replace(".yncp", ".xncp")}");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Conversion was successful, output will be in the same folder as the original file.\nPress any key to quit.");
                Console.ForegroundColor = ConsoleColor.White;
                Console.ReadKey();
            }
            if (Path.GetExtension(@path).IndexOf(".xncp", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                FAPCFile file = new FAPCFile();
                file.Load(path);
                file.Signature = Utilities.Make4CCLE("CPAF");
                file.Resources[0].Content.Signature = Utilities.Make4CCLE("NYIF");
                file.Resources[1].Content.Signature = Utilities.Make4CCLE("NYIF");
                file.Save($@"{path.Replace(".xncp", ".yncp")}");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Conversion was successful, output will be in the same folder as the original file.\nPress any key to quit.");
                Console.ForegroundColor = ConsoleColor.White;
                Console.ReadKey();
            }

        }
        else if (option == 2)
        {
            IntroScreenTitle("Converts from GNCP-TXD to XNCP.\nCredits to Sajid for creating SharpNeedle, and to all the people who work on PuyoTools\nhttps://github.com/Sajidur78/SharpNeedle/\nhttps://github.com/nickworonekin/puyotools");



            Console.WriteLine("Choose the type:");
            Console.WriteLine("1. Before-Sonic Colors");
            Console.WriteLine("2. Sonic Colors");

            Console.Write("Enter your choice: ");
            int optiongn = 0;
            try { optiongn = Convert.ToInt32(Console.ReadLine()); }
            catch (Exception ex)
            { Console.WriteLine("Not a valid choice. Closing..."); }

            if (optiongn == 1)
            {
                string path;
                if (Environment.GetCommandLineArgs().Length == 1)
                {
                    Console.Write("Enter path of the GNCP file");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("[NOTE: You might need a TXD file of the same name to have textures]:");
                    Console.ForegroundColor = ConsoleColor.White;
                    path = @Console.ReadLine();
                }
                else
                    path = @Environment.GetCommandLineArgs()[1];
                path = path.Replace("\"", "");
                if (Path.GetExtension(@path).IndexOf(".gncp", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    Console.WriteLine("Aborted.");
                    return;
                }



                ConvertTXD(path);
                ConvertGNCP(path);
            }

            else
            {
                string path;
                if (Environment.GetCommandLineArgs().Length == 1)
                {
                    Console.Write("Enter path of the GNCP file: ");
                    path = @Console.ReadLine();
                }
                else
                    path = @Environment.GetCommandLineArgs()[1];
                path = path.Replace("\"", "");
                if (Path.GetExtension(@path).IndexOf(".gncp", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    Console.WriteLine("Path doesn't contain the gncp extension. Aborted.");
                    return;
                }
                ConvertGNCP(path);
            }



        }
    }
    public static void ConvertTXD(string path)
    {
        Console.WriteLine();
        Console.WriteLine("Which compression format would you like for the DDS textures?");
        Console.WriteLine("1. DTX1");
        Console.WriteLine("2. DTX2");
        Console.WriteLine("3. DTX3");
        Console.WriteLine("4. DTX4");
        Console.WriteLine("5. DTX5");
        Console.WriteLine("6. A8R8G8B8 (default)");
        Console.Write("Type in the number of the option you want:");
        string opt = Console.ReadLine();
        string formatDDS;
        try
        {
            switch (int.Parse(opt))
            {
                case 1:
                    {
                        formatDDS = "DTX1";
                        break;
                    }
                case 2:
                    {
                        formatDDS = "DTX2";
                        break;
                    }
                case 3:
                    {
                        formatDDS = "DTX3";
                        break;
                    }
                case 4:
                    {
                        formatDDS = "DTX4";
                        break;
                    }
                case 5:
                    {
                        formatDDS = "DTX5";
                        break;
                    }
                default:
                    {
                        formatDDS = "A8R8G8B8";
                        break;
                    }

            }
        }
        catch { formatDDS = "A8R8G8B8"; }
        

        bool checkForZeros = true;
        int indexForTexture = -1;
        //Check if TXD of same name exists in path and extract it if its found
        if (File.Exists(Regex.Replace(path, "gncp", "txd", RegexOptions.IgnoreCase)))
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @Path.Combine(ProgramPath, "PuyoToolsModified", "PuyoToolsCli.exe");
            startInfo.Arguments = $"archive extract --extract-source-folder --input \"{Regex.Replace(path, "gncp", "txd", RegexOptions.IgnoreCase)}\"";
            Process? processExtractTXD = Process.Start(startInfo);
            processExtractTXD.WaitForExit();
            checkForZeros = true;
        }
        else
        {            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Couldn't find TXD of the same name, skipping extraction...");
            checkForZeros = false;
        }


        
        var directorygvr = Path.GetDirectoryName(path);
        string[] files = System.IO.Directory.GetFiles(directorygvr, "*.gvr");
        if (files.Length <= 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No GVR files found. Skipping conversion...\nChecking for PNG images anyway...");
            Console.ForegroundColor = ConsoleColor.White;
            ConvertPNGtoDDS("A8R8G8B8", Directory.GetFiles(directorygvr, "*.png"));
            return;
        }
        for (int i = 0; i < files.Length; i++)
        {
            Textures.Add("");
        }
        foreach (string file in files)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;

            var fileName = Path.GetFileName(file);
            var filePath = Path.GetFullPath(file);
            if (checkForZeros)
            {
                if (indexForTexture == -1) indexForTexture++;
                if (!Path.GetFileName(file).StartsWith("0"))
                    continue;
                string indexFirstPass = Path.GetFileName(file).Substring(0, 3);
                var indexSecondPass = indexFirstPass.TrimStart('0');

                if (!string.IsNullOrEmpty(indexSecondPass))
                    indexForTexture = Convert.ToInt32(indexSecondPass);
                fileName = Path.GetFileName(file).Remove(0, 3);
                try { System.IO.File.Move(file, Path.Combine(Path.GetDirectoryName(file), fileName)); }
                catch { }
                
                filePath = Path.Combine(Path.GetDirectoryName(file), fileName);
            }
            else
                indexForTexture++;

            var corrected = $"\"{filePath.Replace("\"", "")}\"";
            ProcessStartInfo startInfoGVR = new ProcessStartInfo();
            startInfoGVR.FileName = @Path.Combine(ProgramPath, "PuyoToolsModified", "PuyoToolsCli.exe");
            startInfoGVR.Arguments = $"texture decode --input {@corrected}";
            Process? processConvertGVR = Process.Start(startInfoGVR);
            processConvertGVR.WaitForExit();
            Textures[indexForTexture] = Regex.Replace(fileName, "gvr", "dds", RegexOptions.IgnoreCase);
            if (!File.Exists(Regex.Replace(filePath, "gvr", "png", RegexOptions.IgnoreCase)))
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine($"PuyoTools couldn't convert {fileName}!");
            }
            else
            File.Delete(file);            
        }
        ConvertPNGtoDDS(formatDDS, Directory.GetFiles(directorygvr, "*.png"));
        
    }
    public static void ConvertPNGtoDDS(string format, string[] paths)
    {
        for (int i = 0; i < paths.Length; i++)
        {
            var corrected = $"\"{paths[i].Replace("\"", "")}\"";
            ProcessStartInfo startInfoDDS = new ProcessStartInfo();
            startInfoDDS.FileName = @Path.Combine(ProgramPath, "crunch", "bin", "crunch.exe");
            startInfoDDS.RedirectStandardOutput = true;
            
            startInfoDDS.Arguments = $"-file {corrected} -outdir \"{@Path.GetDirectoryName(paths[i])}\" -fileformat dds -{format}";
            Process? processConvertDDS = Process.Start(startInfoDDS);
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Converting {Path.GetFileName(paths[i])} to DDS [{format}]...");
            processConvertDDS.WaitForExit();

            File.Delete(paths[i]);
        }
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
    }
    public static void ConvertGNCPColors(string path)
    {
        

    }
    public static void ConvertGNCP(string path)
    {
        var csdFile = ResourceUtility.Open<CsdProject>(@path);
        if(csdFile.Textures.Count == 0)
        {
            Console.WriteLine("Type 1 to Invert Texture Index (might fix texture issues)");
            int optioninv = 0;
            try { optioninv = Convert.ToInt32(Console.ReadLine()); }
            catch (Exception ex) { }
            if (optioninv == 0)
                Textures.Reverse();

            csdFile.Project.TextureFormat = TextureFormat.Mirage;
            csdFile.Textures = new TextureListMirage();
            for (int i = 0; i < Textures.Count; i++)
            {
                csdFile.Textures.Add(new TextureMirage(new DirectoryInfo(Textures[i]).Name));
            }
        }
        else
        {
            var textures = new TextureListMirage();
            foreach (var texture in csdFile.Textures)
                textures.Add(new TextureMirage(Path.ChangeExtension(texture.Name, ".dds")));
            csdFile.Textures = textures;
        }
        csdFile.Endianness = Endianness.Little;

        csdFile.Write(FileSystem.Create(@Regex.Replace(path, "gncp", "xncp", RegexOptions.IgnoreCase)));
        var project = csdFile.Project;

        if (csdFile.Textures != null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Conversion was successful, output will be in the same folder as the original file.");
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;           
            Console.WriteLine("NOTE: The result might have artifacts and the textures might look wrong, this is because the process is still W.I.P");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Press any key to quit.");
            Console.ForegroundColor = ConsoleColor.White;
            Console.ReadKey();
        }
    }
    public static void IntroScreenTitle(string panelMessage)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine("|---------------------------|");
        Console.WriteLine();
        Console.WriteLine("NexMod NCP Utility v0.2 - by NextinHKRY");
        Console.WriteLine();
        Console.WriteLine(panelMessage);
        Console.WriteLine();
        Console.WriteLine("|---------------------------|");
        Console.WriteLine();
        if (Environment.GetCommandLineArgs().Length == 2)
            Console.WriteLine($"Arguments found: {Environment.GetCommandLineArgs()[2]}");
        Console.ForegroundColor = ConsoleColor.White;

    }
}

public static class GNCPConvertSharpNeedle
{
    static int indent = 0;
    public static void PrintSceneNode(SceneNode node)
    {
        foreach (var scenePair in node.Scenes)
        {
            WriteLine($"{scenePair.Key}:");
            PushIndentation();
            int i = 0;
            foreach (var group in scenePair.Value.Families)
            {
                WriteLine($"Layer_{i++}:");
                PushIndentation();
                foreach (var cast in group)
                    PrintCast(cast);

                PopIndentation();
                Console.WriteLine();
            }
            PopIndentation();

            void PrintCast(SharpNeedle.Ninja.Csd.Cast cast)
            {
                WriteLine($"{cast.Name}{(cast.Count == 0 ? "" : ":")}");
                PushIndentation();

                foreach (var child in cast)
                    PrintCast(child);

                PopIndentation();
            }
        }

        foreach (var child in node.Children)
        {
            WriteLine($"{child.Key}:");
            PushIndentation();
            PrintSceneNode(child.Value);
            PopIndentation();
        }
    }

    public static void ApplyIndentation()
    {
        for (int i = 0; i < indent; i++)
            Console.Write("  ");
    }

    public static void Write(string text)
    {
        ApplyIndentation();
        Console.Write(text);
    }

    public static void WriteLine(string text)
    {
        ApplyIndentation();
        Console.WriteLine(text);
    }
    public static void HideOutput(string input)
    {        
    }


    public static void PushIndentation() => ++indent;
    public static void PopIndentation() => --indent;
}