namespace Cli.Modes;

abstract class Mode {
    protected abstract string prompt{get;}
    protected abstract string helpText{get;}
    protected abstract Dictionary<string, Action<ArraySegment<string>>> functions{get;}

    protected string baseHelpText = 
        "\thelp - shows this help\n" +
        "\texit - leaves the mode";

    public void Run() {
        while(true) {
            Console.Write(prompt);
            var cmdLine = Console.ReadLine();
            var cmd = cmdLine.Split(' ');
            var funcName = cmd.FirstOrDefault();
            switch (funcName)
            {
            case "":
                Console.WriteLine("You must input a function, help for help text");
                break;
            case "help":
                Console.WriteLine(helpText);
                break;
            case "exit": 
                Console.WriteLine("Exiting...");
                return;
            default:
                if (functions.ContainsKey(funcName)) {
                    var len = cmd.Length - 1;
                    functions[funcName](new ArraySegment<string>(cmd, 1, len));
                } else {
                    Console.WriteLine($"There is no function with name \"{funcName}\", try help");
                }
                break;
            }
        }
    }
}