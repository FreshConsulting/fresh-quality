# Comparing automated testing with and without Fresh Quality

## Test Setup
        
### Without Fresh Quality

```csharp
/// <summary>
/// An instance of the TodoController
/// </summary>
private TodoController todoController = null;
/// <summary>
/// An instance of the DB context
/// </summary>
private TodoContext todoContext = null;
[TestInitialize]
public void InitTestEnvironment()
{
    if (this.todoContext != null)
    {
        //Initialization already done.  Note: ClassInitialize isn't used
        //Due to requiring the TodoContext properties.
        return;
    }
    //Setup the TODO Db Context
    var optionsBuilder = new DbContextOptionsBuilder<TodoContext>();
    optionsBuilder.UseInMemoryDatabase("TodoList");
    this.todoContext = new TodoContext(optionsBuilder.Options);
    //Setup the TODO Controller
    var configuration = new ConfigurationBuilder().Build();
    var startup = new Startup(configuration);
    var sc = new ServiceCollection();
    startup.ConfigureServices(sc);
    var serviceProvider = sc.BuildServiceProvider();
    this.todoController = new TodoController(this.todoContext, serviceProvider, configuration);
}
```

### With Fresh Quality

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

## Test Setup and Helpers

### Without Fresh Quality

```csharp
public TodoController GetTodoController()
{
    return this.todoController;
}
```

### With Fresh Quality

## Automated Test

### Without Fresh Quality
```csharp
[TestMethod]
public async Task GetTodoItemsResultsList()
{
  var ctrllr = GetTodoController(); 
  var result = await ctrllr.GetTodoItems();
  var todoEnumerable = result.Value;
  Assert.IsNotNull(todoEnumerable);
  Assert.IsTrue(todoEnumerable.Any());
}
```
### With Fresh Quality
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
