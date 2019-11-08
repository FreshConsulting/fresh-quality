// <summary>
// FreshQuality is a tool that will:
// a) analyze the attached libraries/projects of a test project,
// b) create instances of pertinent classes(via dependency injection), initially only controllers, but this was extended.
// c) expose them for use in unit tests.
//
// The ability to how the dependency injection works allow for testing the classes
// in a disconnect manner.
// </summary>
// <copyright file="ControllerTests_WithFreshQuality.cs" company="Fresh Consulting LLC">2019</copyright>

namespace ExampleTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using ExampleProject.Controllers;
    using ExampleProject.Models;
    using FreshQuality;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Provides a simple example of unit tests for the Todo Controller using
    /// FreshQuality's TestBase for IOC.
    /// </summary>
    [TestClass]
    public class ControllerTests_WithFreshQuality : TestBase<ControllerBase, ControllerTests_WithFreshQuality>
    {
        /// <summary>
        /// Tests the Get method of TestBase is working
        /// </summary>
        [TestMethod]
        public void GetActuallyReturnsAnInstance()
        {
            var ctrllr = Get<TodoController>();

            Assert.IsNotNull(ctrllr);
            Assert.IsInstanceOfType(ctrllr, typeof(TodoController));
        }

        /// <summary>
        /// Tests todo item retrieval
        /// </summary>
        /// <returns>nothing to return</returns>
        [TestMethod]
        public async Task GetTodoItemsResultsList()
        {
            var ctrllr = Get<TodoController>();

            var result = await ctrllr.GetTodoItems();
            var todoEnumerable = result.Value;

            Assert.IsNotNull(todoEnumerable);
            Assert.IsTrue(todoEnumerable.Any());
        }

        /// <summary>
        /// Tests adding todo items works
        /// </summary>
        /// <returns>nothing to return</returns>
        [TestMethod]
        public async Task AddTodoItemAddsToList()
        {
            var ctrllr = Get<TodoController>();

            int currentTodoCount = (await ctrllr.GetTodoItems()).Value.Count();
            string todoText = "Test Entry";
            var result = await ctrllr.PostTodoItem(new TodoItem() { Name = todoText });
            int updatedTodoCount = (await ctrllr.GetTodoItems()).Value.Count();

            Assert.IsNotNull(result.Result);
            Assert.AreEqual(currentTodoCount + 1, updatedTodoCount);
        }

        /// <summary>
        /// Tests the deletion of todo items works.
        /// </summary>
        /// <returns>nothing to return</returns>
        [TestMethod]
        public async Task DeleteTodoItemRemovesEntry()
        {
            var ctrllr = Get<TodoController>();

            int currentTodoCount = (await ctrllr.GetTodoItems()).Value.Count();
            var result = await ctrllr.DeleteTodoItem(1);
            int updatedTodoCount = (await ctrllr.GetTodoItems()).Value.Count();

            Assert.IsNotNull(result);
            Assert.AreEqual(currentTodoCount - 1, updatedTodoCount);
        }

        /// <summary>
        /// Tests the item retrieval feature.
        /// </summary>
        /// <returns>nothing to return</returns>
        [TestMethod]
        public async Task GetItemRetrievesEntry()
        {
            var ctrllr = Get<TodoController>();
            var last = (await ctrllr.GetTodoItems()).Value.Last();
            var item = await ctrllr.GetTodoItem(last.Id);

            Assert.IsNotNull(item);
            Assert.AreEqual(last.Id, item.Value.Id);
        }

        /// <summary>
        /// Tests the overriding mechanism for the Get method
        /// </summary>
        /// <returns>nothing to return</returns>
        [TestMethod]
        public async Task OverrideIOCContextResultsInChanges()
        {
            // Setup the TODO Db Context
            var optionsBuilder = new DbContextOptionsBuilder<TodoContext>();
            optionsBuilder.UseInMemoryDatabase("TodoList");

            var filledList = new TodoContext(optionsBuilder.Options);
            
            filledList.Add<TodoItem>(new TodoItem() {  IsComplete = true, Name = "Make an override" });
            filledList.Add<TodoItem>(new TodoItem() {  IsComplete = false, Name = "Test the override" });
            filledList.Add<TodoItem>(new TodoItem() {  IsComplete = false, Name = "Document override" }) ;
            filledList.Add<TodoItem>(new TodoItem() { IsComplete = true, Name = "Make test project" });
            filledList.Add<TodoItem>(new TodoItem() {  IsComplete = true, Name = "Run test project" });
            filledList.SaveChanges();
            var ctrllr = Get<TodoController>(filledList);
            int itemCount = (await ctrllr.GetTodoItems()).Value.ToList().Count;
            Assert.AreEqual(filledList.TodoItems.Count(), itemCount);
        }

        /// <summary>
        /// Initializes the Services.
        /// </summary>
        /// <param name="services">Services collection</param>
        /// <param name="neededInterfaces">Interfaces needed for proper initialization</param>
        protected override void ServiceInitializer(ServiceCollection services, HashSet<Type> neededInterfaces)
        {
            // Note this DB context matches the one in Startup, it doesn't have to so long as 
            // it is a valid context compatible with the code to be tested.
            services.AddDbContext<TodoContext>(opt => opt.UseInMemoryDatabase("TodoList"));
        }

        /// <summary>
        /// Sets up the configuration
        /// </summary>
        /// <returns>the configuration</returns>
        protected override IConfiguration SetupConfiguration()
        {
            // If the base result is used, then the default config is setup.
            return base.SetupConfiguration();   
        }
    }
}
