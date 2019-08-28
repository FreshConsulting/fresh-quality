_Todo Make Side by Side between Fresh Quality and vanilla versions_

<script src="https://cdn.mathjax.org/mathjax/latest/MathJax.js?config=TeX-AMS-MML_HTMLorMML" type="text/javascript"></script>
<table border="0">
    <caption>Comparing automated testing with and without Fresh Quality</caption>
    <tr>
        <th>Automated Test Portion</th>
        <th>Without Fresh Quality</th>
        <th>With Fresh Quality</th>
    </tr>
    <tr>
        <td>Test Class Initialize</td>
        <td>

        </td>
        <td>
            ```csharp
            [TestClass]
            public class ControllerTests : TestBase<ControllerBase, ControllerTests>
            {

                protected override void ServiceInitializer(ServiceCollection services, HashSet<Type> neededInterfaces)
                {
                  //Note this DB context matches the one in Startup, it doesn't have to so long as 
                  //it is a valid context compatible with the code to be tested.
                  services.AddDbContext<TodoContext>(opt => opt.UseInMemoryDatabase("TodoList"));
                }
            ```
        </td>
    </tr>
    <tr>
        <td>Test Setup and Helpers</td>
        <td>
        </td>
        <td>
        </td>
    </tr>
    <tr>
        <td>Automated Test</td>
        <td>
        </td>
        <td>
            ```csharp
            [TestMethod]
            public async Task GetTodoItemsResultsList()
            {
              var ctrllr = Get<TodoController>();
              
              var result = await ctrllr.GetTodoItems();
              var todoEnumerable = result.Value;
              
              Assert.IsNotNull(todoEnumerable);
              Assert.IsTrue(todoEnumerable.Any());
            }
            ```
        </td>
    </tr> 
</table>
