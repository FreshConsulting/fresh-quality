// <summary>
// FreshQuality is a tool that will:
// a) analyze the attached libraries/projects of a test project,
// b) create instances of pertinent classes(via dependency injection), initially only controllers, but this was extended.
// c) expose them for use in unit tests.
//
// The ability to how the dependency injection works allow for testing the classes
// in a disconnect manner.
// </summary>
// <copyright file="ControllerTests_NoFreshQuality.cs" company="Fresh Consulting LLC">2019</copyright>

namespace ExampleTests
{
    using System.Linq;
    using System.Threading.Tasks;
    using ExampleProject;
    using ExampleProject.Controllers;
    using ExampleProject.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Provides a simple example of unit tests for the Todo Controller without
    /// using the FreshQuality tool.
    /// </summary>
    [TestClass]
    public class ControllerTests_NoFreshQuality
    {
        #region Setup Methods
        /// <summary>
        /// An instance of the TodoController
        /// </summary>
        private TodoController todoController = null;

        /// <summary>
        /// An instance of the DB context
        /// </summary>
        private TodoContext todoContext = null;

        /// <summary>
        /// Initializes the test environment
        /// </summary>
        [TestInitialize]
        public void InitTestEnvironment()
        {
            if (this.todoContext != null)
            {
                // Initialization already done.  Note: ClassInitialize isn't used
                // Due to requiring the TodoContext properties and making the comparisons
                // Between w/ and w/o FreshQuality less clear.
                return;
            }

            // Setup the TODO Db Context
            var optionsBuilder = new DbContextOptionsBuilder<TodoContext>();
            optionsBuilder.UseInMemoryDatabase("TodoList");
            
            this.todoContext = new TodoContext(optionsBuilder.Options);

            // Setup the TODO Controller
            var configuration = new ConfigurationBuilder().Build();

            var startup = new Startup(configuration);
            var sc = new ServiceCollection();

            startup.ConfigureServices(sc);
            var serviceProvider = sc.BuildServiceProvider();

            this.todoController = new TodoController(this.todoContext, serviceProvider, configuration);
        }

        #endregion

        #region Helper Methods
        /// <summary>
        /// Gets an instance of TodoController
        /// </summary>
        /// <returns>instance of TodoController</returns>
        public TodoController GetTodoController()
        {
            return this.todoController;
        }

        #endregion

        /// <summary>
        /// Tests the Get method of TestBase is working
        /// </summary>
        [TestMethod]
        public void GetActuallyReturnsAnInstance()
        {
            var ctrllr = this.GetTodoController();

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
            var ctrllr = this.GetTodoController();

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
            var ctrllr = this.GetTodoController();

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
            var ctrllr = this.GetTodoController();

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
            var ctrllr = this.GetTodoController();
            var last = (await ctrllr.GetTodoItems()).Value.Last();
            var item = await ctrllr.GetTodoItem(last.Id);

            Assert.IsNotNull(item);
            Assert.AreEqual(last.Id, item.Value.Id);
        }
    }
}
