// <summary>
// FreshQuality is a tool that will:
// a) analyze the attached libraries/projects of a test project,
// b) create instances of pertinent classes(via dependency injection), initially only controllers, but this was extended.
// c) expose them for use in unit tests.
//
// The ability to how the dependency injection works allow for testing the classes
// in a disconnect manner.
// </summary>
// <copyright file="TodoController.cs" company="Fresh Consulting LLC">2019</copyright>

namespace ExampleProject.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using ExampleProject.Models;
    using Microsoft.AspNetCore.Mvc;

    // For simplicity sake, the example project will be based on the 
    // web api tutorial by MS: https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-2.1&tabs=visual-studio
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Provides endpoints to manage ToDo items.  
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TodoController : ControllerBase
    {
        /// <summary>
        /// A reference to the DB context
        /// </summary>
        private readonly TodoContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="TodoController"/> class.
        /// </summary>
        /// <param name="context">The DB context</param>
        /// <param name="serviceProvider">The service provider</param>
        /// <param name="configuration">The configuration</param>
        public TodoController(TodoContext context, IServiceProvider serviceProvider,  IConfiguration configuration)
        {
            this.context = context;

            if (this.context.TodoItems.Count() == 0)
            {
                // Create a new TodoItem if collection is empty,
                // which means you can't delete all TodoItems.
                this.context.TodoItems.Add(new TodoItem { Name = "Item1" });
                this.context.SaveChanges();
            }

            // These two checks would not be in production code, but are provided
            // to reveal if the IOC for unit tests is working.
            if (serviceProvider == null)
            {
                var msg = $"Service Provider {nameof(serviceProvider)} was null indicating IOC failed.";
                throw new NullReferenceException(msg);
            }

            if (configuration == null)
            {
                var msg = $"Configuration {nameof(configuration)} was null indicating IOC failed.";
                throw new NullReferenceException(msg);
            }
        }

        /// <summary>
        /// GET api/Todo
        /// </summary>
        /// <returns>List of todo items</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
        {
            return await this.context.TodoItems.ToListAsync();
        }

        /// <summary>
        /// GET api/Todo/<paramref name="id"/>
        /// </summary>
        /// <param name="id">The id of the Todo Item</param>
        /// <returns>Todo Item matching the id.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem(long id)
        {
            var todoItem = await this.context.TodoItems.FindAsync(id);
            if (todoItem == null)
            {
                return this.NotFound();
            }

            return todoItem;
        }

        /// <summary>
        /// POST api/Todo
        /// </summary>
        /// <param name="item">Item to create</param>
        /// <returns>the created item (with an Id)</returns>
        [HttpPost]
        public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem item)
        {
            this.context.TodoItems.Add(item);
            await this.context.SaveChangesAsync();

            return this.CreatedAtAction(nameof(this.GetTodoItem), new { id = item.Id }, item);
        }

        /// <summary>
        /// PUT api/Todo/5
        /// </summary>
        /// <param name="id">id to modify</param>
        /// <param name="item">item to modify</param>
        /// <returns>success status</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTodoItem(long id, TodoItem item)
        {
            if (id != item.Id)
            {
                return this.BadRequest();
            }

            this.context.Entry(item).State = EntityState.Modified;
            await this.context.SaveChangesAsync();

            return this.NoContent();
        }

        /// <summary>
        /// DELETE api/Todo/<paramref name="id"/>
        /// </summary>
        /// <param name="id">Id of Todo Item to delete</param>
        /// <returns>Success of deletion</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoItem(long id)
        {
            var todoItem = await this.context.TodoItems.FindAsync(id);

            if (todoItem == null)
            {
                return this.NotFound();
            }

            this.context.TodoItems.Remove(todoItem);
            await this.context.SaveChangesAsync();

            return this.NoContent();
        }
    }
}
