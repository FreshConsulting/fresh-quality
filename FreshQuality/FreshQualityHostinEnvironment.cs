/*
FreshQuality is a tool that will:
a) analyze the attached libraries/projects of a test project,
b) create instances of pertinent classes (via dependency injection), [INITIALLY ONLY CONTROLLERS]
c) expose them for use in unit tests.

The ability to how the dependency injection works allow for testing the classes
in a disconnect manner.
*/

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace FreshQuality
{
    public class FreshQualityHostingEnvironment : IHostingEnvironment
    {
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    }
}