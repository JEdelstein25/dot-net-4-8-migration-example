using System;
using System.Threading;

namespace TaxCalculator.StandaloneApi
{
    class Program
    {
        private static TaxApiServer _server;
        private static bool _running = true;

        static void Main(string[] args)
        {
            Console.WriteLine("===============================================");
            Console.WriteLine("Australian Tax Calculator - Standalone API");
            Console.WriteLine("(.NET Framework 4.8 Implementation)");
            Console.WriteLine("===============================================");
            Console.WriteLine();

            var baseUrl = "http://localhost:8080/";
            if (args.Length > 0)
            {
                baseUrl = args[0];
            }

            Console.WriteLine($"Starting server on {baseUrl}");
            Console.WriteLine("Press Ctrl+C to stop the server");
            Console.WriteLine();

            // Handle Ctrl+C gracefully
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _running = false;
                Console.WriteLine("\nShutting down server...");
                _server?.Stop();
            };

            try
            {
                _server = new TaxApiServer(baseUrl);
                
                // Start server in background thread
                var serverThread = new Thread(() =>
                {
                    try
                    {
                        _server.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Server error: {ex.Message}");
                        if (ex.Message.Contains("Access is denied"))
                        {
                            Console.WriteLine("\nTip: Try running as Administrator or use a different port:");
                            Console.WriteLine("  TaxCalculator.StandaloneApi.exe http://localhost:8081/");
                        }
                    }
                })
                {
                    IsBackground = true
                };

                serverThread.Start();

                // Keep main thread alive
                while (_running)
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start server: {ex.Message}");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }

            Console.WriteLine("Server stopped.");
        }
    }
}
