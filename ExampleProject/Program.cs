// <summary>
// FreshQuality is a tool that will:
// a) analyze the attached libraries/projects of a test project,
// b) create instances of pertinent classes(via dependency injection), initially only controllers, but this was extended.
// c) expose them for use in unit tests.
//
// The ability to how the dependency injection works allow for testing the classes
// in a disconnect manner.
// </summary>
// <copyright file="Program.cs" company="Fresh Consulting LLC">2019</copyright>

namespace ExampleProject
{
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;

    /// <summary>
    /// Main TODO application 
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Program launch point
        /// </summary>
        /// <param name="args">command line args</param>
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Creates a default web host
        /// </summary>
        /// <param name="args">any command line arguments</param>
        /// <returns>A web host builder</returns>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
