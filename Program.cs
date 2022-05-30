using System.Diagnostics;
using System.Text.RegularExpressions;
using XNCPLib.Misc;
using SharpNeedle.Ninja.Csd;
using XNCPLib.XNCP;
using Amicitia.IO.Binary;
using SharpNeedle.Utilities;
using SharpNeedle.IO;

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
                    Console.Write("Enter path of the GNCP file [NOTE: You need a TXD file of the same name in the same folder as the GNCP file]: ");
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
                Console.Clear();
                Console.WriteLine("Not going to implement it for now, too much of a pain.");
            }



        }
    }
    public static void ConvertTXD(string path)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = @Path.Combine(ProgramPath, "PuyoToolsModified", "PuyoToolsCli.exe");
        startInfo.Arguments = $"archive extract --extract-source-folder --input \"{Regex.Replace(path, "gncp", "txd", RegexOptions.IgnoreCase)}\"";
        Process? processExtractTXD = Process.Start(startInfo);

        processExtractTXD.WaitForExit();
        var directorygvr = Path.GetDirectoryName(path);
        string[] files = System.IO.Directory.GetFiles(directorygvr, "*.gvr");
        if (files.Length <= 0)
        {
            Console.WriteLine("PuyoTools wasn't able to extract the TXD. Aborted");
            Console.ReadKey();
            return;
        }
        for (int i = 0; i < files.Length; i++)
        {
            Textures.Add("");
        }
        foreach (string file in files)
        {
            if (!Path.GetFileName(file).StartsWith("0"))
                continue;
            string indexFirstPass = Path.GetFileName(file).Substring(0, 3);
            var indexSecondPass = indexFirstPass.TrimStart('0');

            int index = 0;
            if (!string.IsNullOrEmpty(indexSecondPass))
                index = Convert.ToInt32(indexSecondPass);
            var fileName = Path.GetFileName(file).Remove(0, 3);


            var corrected = $"\"{file.Replace("\"", "")}\"";
            ProcessStartInfo startInfoGVR = new ProcessStartInfo();
            startInfoGVR.FileName = @Path.Combine(ProgramPath, "PuyoToolsModified", "PuyoToolsCli.exe");
            startInfoGVR.Arguments = $"texture decode --input {@corrected}";
            Process? processConvertGVR = Process.Start(startInfoGVR);
            processConvertGVR.WaitForExit();
            Textures[index] = Regex.Replace(fileName, "gvr", "dds", RegexOptions.IgnoreCase);
            File.Delete(file);
            
        }
        
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
            Console.WriteLine("NOTE: You'll need to convert the textures from .PNG to .DDS manually. This is because I haven't found a good way to conver them in C# yet. You can use a program like paint.net for this!");
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
        Console.WriteLine("NexMod NCP Utility v0.1 - by NextinHKRY");
        Console.WriteLine();
        Console.WriteLine(panelMessage);
        Console.WriteLine();
        Console.WriteLine("|---------------------------|");
        Console.WriteLine();
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


    public static void PushIndentation() => ++indent;
    public static void PopIndentation() => --indent;
}