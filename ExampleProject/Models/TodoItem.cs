// <summary>
// FreshQuality is a tool that will:
// a) analyze the attached libraries/projects of a test project,
// b) create instances of pertinent classes(via dependency injection), initially only controllers, but this was extended.
// c) expose them for use in unit tests.
//
// The ability to how the dependency injection works allow for testing the classes
// in a disconnect manner.
// </summary>
// <copyright file="TodoItem.cs" company="Fresh Consulting LLC">2019</copyright>
namespace ExampleProject.Models
{
    /// <summary>
    /// A todo item entry
    /// </summary>
    public class TodoItem
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Todo Item is complete.
        /// </summary>
        public bool IsComplete { get; set; }
    }
}
