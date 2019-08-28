// <summary>
// FreshQuality is a tool that will:
// a) analyze the attached libraries/projects of a test project,
// b) create instances of pertinent classes(via dependency injection), initially only controllers, but this was extended.
// c) expose them for use in unit tests.
//
// The ability to how the dependency injection works allow for testing the classes
// in a disconnect manner.
// </summary>
// <copyright file="TodoContext.cs" company="Fresh Consulting LLC">2019</copyright>

namespace ExampleProject.Models
{
    using System;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// The DB context for the Todo App
    /// </summary>
    public class TodoContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TodoContext"/> class.
        /// </summary>
        /// <param name="options">Any DB context options.</param>
        public TodoContext(DbContextOptions<TodoContext> options) : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the Todo Items in the DB.
        /// </summary>
        public DbSet<TodoItem> TodoItems { get; set; }
    }
}
