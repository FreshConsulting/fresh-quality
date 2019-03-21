using Microsoft.VisualStudio.TestTools.UnitTesting;
using FreshQuality;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using ExampleProject.Controllers;
using System.Runtime.CompilerServices;
using ExampleProject.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace ExampleTests
{
    [TestClass]
    public class ControllerTests : TestBase<ControllerBase, ControllerTests>
    {
        public ControllerTests():base()
        {
        }


        protected override void ServiceInitializer(ServiceCollection services, HashSet<Type> neededInterfaces)
        {
            //Note this DB context matches the one in Startup, it doesn't have to so long as 
            //it is a valid context compatible with the code to be tested.
            services.AddDbContext<TodoContext>(opt => opt.UseInMemoryDatabase("TodoList"));

        }

        protected override IConfiguration SetupConfiguration()
        {
            //If the base result is used, then the default config is setup.
            return base.SetupConfiguration();   
        }


        [TestMethod]
        public void GetActuallyReturnsAnInstance()
        {
            var ctrllr = Get<TodoController>();

            Assert.IsNotNull(ctrllr);
            Assert.IsInstanceOfType(ctrllr, typeof(TodoController));
        }

        [TestMethod]
        public async Task GetTodoItemsResultsList()
        {
            var ctrllr = Get<TodoController>();

            var result = await ctrllr.GetTodoItems();
            var todoEnumerable = result.Value;

            Assert.IsNotNull(todoEnumerable);
            Assert.IsTrue(todoEnumerable.Any());
        }

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

        [TestMethod]
        public async Task GetItemRetrievesEntry()
        {
            var ctrllr = Get<TodoController>();
            var last = (await ctrllr.GetTodoItems()).Value.Last();
            var item = await ctrllr.GetTodoItem(last.Id);

            Assert.IsNotNull(item);
            Assert.AreEqual(last.Id, item.Value.Id);
        }

        [TestMethod]
        public async Task PutUpdatesItem()
        {
            var ctrllr = Get<TodoController>();
            var last = (await ctrllr.GetTodoItems()).Value.Last();

            last.Name = "Altered Name";
            var result = await ctrllr.PutTodoItem(last.Id, last);

            var updated = await ctrllr.GetTodoItem(last.Id);

            Assert.AreEqual(last.Name, updated.Value.Name);
        }
    }
}
