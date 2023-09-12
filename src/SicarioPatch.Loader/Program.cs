using System.Threading.Tasks;

namespace SicarioPatch.Loader;

file static class Program
{
    private static Task<int> Main(string[] args)
    {
        return Startup.GetApp().RunAsync(args);
    }
}