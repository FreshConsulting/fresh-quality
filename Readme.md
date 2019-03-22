# Purpose 

The FreshQuality project provides testing tools for projects in .Net Standard 2.0 or later.  Most notably the TestBase provides a mechanism for testing classes that are instantiated using IOC, a common scenario being Controllers from a MVC project.

## Minimum Requirements

* .Net Standard 2.0 

## Underlying Implementation

The TestBase class hides the implementation that facillitates easy IOC testing, making it simple to get started.  Typically the most difficult part of getting setup is having all necessary Nuget packages and references installed in the test project.

Supporting the TestBase is a mechanism that scans all linked projects for classes that are subclass to the generic type passed into TestBase.  It then determines what constructors are available, and which types are used in those constructors.   It provides this information in a `neededInterfaces` variable into the `ServiceInitializer` method.  The information can be printed out to determine which types must be configured for IOC, or creating a test with a `Get<T>` call can be made, and an error message for missing types needed for `T` to be instantiated will be provided.

## What's Included 

To demonstrate the abilities of the `FreshQuality` project, an example project that implements the code in the tutorial article: https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-2.1&tabs=visual-studio is provided.  This `ExampleProject` gives a simple Todo list web api using an in-memory datastore. 

Also attached to this solution is the `ExampleTests` has a set of tests which provide automated tests against the `TodoController` of `ExampleProject`. The `ControllerTests` class inherits from the `TestBase` class, and demonstrates how easy it is to instantiate a controller that has multiple parameters that use dependency injection.
 
## Getting Started

After an MS Test project is created: 
1. A reference to the project to be tested should be added.
2. Make the test class inherit from TestBase.  TestBase takes 2 generic parameters: 
  *  The base class to scan project references for.  A common scenario would be `ControllerBase` to get all MVC controllers.
  *  The typeof the test class.  In the `ExampleTests`, the class `ControllerTests` would pass in `ControllerTests`.  
  So in the `ExampleTests` scenario, the class declaration for `ControllerTests` looks like:  
        
```Java
[TestClass]
public class ControllerTests : TestBase<ControllerBase, ControllerTests>
```
        
    3.  Add an override method for `ServiceInitializer` that registers any services required for the types that will be tested.   A good starting place would the services that are added in the Startup.cs of a MVC project.  In the `ExampleTests`, the `ServiceInitializer` was implemented as follows:

```Java
protected override void ServiceInitializer(ServiceCollection services, HashSet<Type> neededInterfaces)
{
  //Note this DB context matches the one in Startup, it doesn't have to so long as 
  //it is a valid context compatible with the code to be tested.
  services.AddDbContext<TodoContext>(opt => opt.UseInMemoryDatabase("TodoList"));
}
```
    
    4. Create tests as needed, and use the `Get<S>` method to get instances that `TestBase` has retrieved.  In the `ControllerTests` example, the type `T` is `ControllerBase`, so `S` can be any type that inherits from `ControllerBase`.  In the `ControllerTests.cs` a simple tests that gets the controller is included below:
    
```Java
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
 
    