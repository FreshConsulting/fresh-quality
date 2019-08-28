// <summary>
// FreshQuality is a tool that will:
// a) analyze the attached libraries/projects of a test project,
// b) create instances of pertinent classes(via dependency injection), initially only controllers, but this was extended.
// c) expose them for use in unit tests.
//
// The ability to how the dependency injection works allow for testing the classes
// in a disconnect manner.
// </summary>
// <copyright file="FreshQualityHostingEnvironment.cs" company="Fresh Consulting LLC">2019</copyright>

namespace FreshQuality
{
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Provides a stubbed hosting environment for testing purposes.
    /// </summary>
    public class FreshQualityHostingEnvironment : IHostingEnvironment
    {
        /// <summary>
        /// Gets or sets the name of the environment. The host automatically sets this property to the
        /// value of the "ASPNETCORE_ENVIRONMENT" environment variable, or "environment" as specified in any other configuration source.
        /// </summary>
        public string EnvironmentName { get; set; }

        /// <summary>
        /// Gets or sets the name of the application.This property is automatically set
        /// by the host to the assembly containing the application entry point.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the absolute path to the directory that contains the application content files.
        /// </summary>
        public string ContentRootPath { get; set; }

        /// <summary>
        /// Gets or sets an IFileProvider pointing at ContentRootPath.
        /// </summary>
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}