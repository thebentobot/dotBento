namespace dotBento.Bot;

public static class Program
{
    public static Task Main(string[] args)
        => Startup.RunAsync(args);
}