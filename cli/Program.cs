using Cli;

if (args.Length > 0 && args[0] == "s") {
    Mains.ServerMain(args);
} else {
    Mains.ClientMain(args);
}
