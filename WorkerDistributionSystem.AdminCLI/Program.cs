using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkerDistributionSystem.AdminCLI.Commands;

namespace WorkerDistributionSystem.AdminCLI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder().Build();
            var commandProcessor = host.Services.GetRequiredService<ICommandProcessor>();

            await StartCLI(commandProcessor);
        }

        private static async Task StartCLI(ICommandProcessor commandProcessor)
        {
            ShowInstructions();

            while (true)
            {
                try
                {
                Start:
                    Console.Write("> ");
                    var input = Console.ReadLine();

                    if (string.IsNullOrEmpty(input))
                    {
                        Console.WriteLine("Please enter command!");
                        goto Start;
                    }

                    if (IsExitCommand(input))
                    {
                        Console.WriteLine("Goodbye!");
                        break;
                    }

                    if (IsHelpCommand(input))
                    {
                        ShowInstructions();
                        continue;
                    }

                    if (IsClearCommand(input))
                    {
                        Console.Clear();
                        ShowInstructions();
                        continue;
                    }

                    var inputArgs = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    await commandProcessor.ProcessAsync(inputArgs);
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine("Type 'help' for available commands.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error: {ex.Message}");
                }
            }
        }

        private static bool IsClearCommand(string input)
        {
            return input.Equals("clear", StringComparison.OrdinalIgnoreCase) ||
                         input.Equals("cls", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHelpCommand(string input)
        {
            return input.Equals("help", StringComparison.OrdinalIgnoreCase) ||
                         input.Equals("--help", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsExitCommand(string input)
        {
            return string.IsNullOrEmpty(input) ||
                         input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                         input.Equals("quit", StringComparison.OrdinalIgnoreCase);
        }

        private static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices(ConfigureApplicationDependencies)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });
        }

        private static void ConfigureApplicationDependencies(IServiceCollection services)
        {
            services.AddScoped<ICommandProcessor, CommandProcessor>();
            services.AddLogging(configure => configure.AddConsole());
        }

        private static void ShowInstructions()
        {
            Console.WriteLine("Worker Distribution System - Admin CLI");
            Console.WriteLine("Usage: AdminCLI <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  worker execute \"<command>\"  - Execute command on available worker");
            Console.WriteLine("  worker add <worker_name>     - Add new worker instance");
            Console.WriteLine("  worker remove <worker_name>  - Remove worker instance");
            Console.WriteLine("  worker status                - Show workers status");
            Console.WriteLine("  service start                - Start the Windows service");
            Console.WriteLine("  service stop                 - Stop the Windows service");
            Console.WriteLine("  service status               - Show service status");
            Console.WriteLine("<-------------------------------------------------------------------->");
        }
    }
}