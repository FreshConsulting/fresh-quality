/*
FreshQuality is a tool that will:
a) analyze the attached libraries/projects of a test project, 
b) create instances of pertinent classes (via dependency injection), [INITIALLY ONLY CONTROLLERS]
c) expose them for use in unit tests.

The ability to how the dependency injection works allow for testing the classes
in a disconnect manner.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;

namespace FreshQuality
{
    /// <summary>
    /// Scaffolding mechanism to facillitate testing.
    /// </summary>
    public class QualityFacillitator<T>
    {

        protected Type SubclassType { get; set; }

        /// <summary>
        /// The types that implement the generic class type (T).
        /// </summary>
        private List<Type> matchingTypes { get; set; }

        /// <summary>
        /// Interfaces that need to be defined by the user of the library
        /// </summary>
        private HashSet<Type> neededInterfaces { get; set; }

        /// <summary>
        /// Mapping between a needed type and every constructor that needs the type.
        /// </summary>
        private Dictionary<Type, List<ConstructorInfo>> neededTypesToConstructors;

        /// <summary>
        /// The ServiceInitializer is called with the existing <Type>ServicesCollection</Type> as well as
        /// a <Type>HashSet<typeparamref name="Type"/></Type>.  Any custom services used in DI/IOC
        /// should be set here.
        /// </summary>
        public Action<ServiceCollection, HashSet<Type>> ServiceInitializerMethod { get; set; }

        public ServiceProvider ServiceProvider { get; private set; }
        private ServiceCollection Services { get; set; }

        internal Exception InitializationError { get; private set; }

        public Func<IConfiguration> SetupConfigurationMethod { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:FreshQuality.QualityFacillitator`1"/> class.
        /// </summary>
        /// <param name="namespaceIgnoreList">prefix list of namespaces to ignore.  Defaults to "System." and "Microsoft.".</param>
        public QualityFacillitator(Type subclassType, List<string> namespaceIgnoreList = null)
        {
            try
            {
                SubclassType = subclassType;
                PopulateMatchingTypesList(namespaceIgnoreList);
                AnalyzeConstructors();
                PrepareServices();
            }
            catch(Exception ex)
            {
                InitializationError = ex;
            }
        }

        /// <summary>
        /// Either this method must be overridden or the ServiceInitializerMethod must be set.
        /// </summary>
        /// <param name="services">Services.</param>
        /// <param name="neededInterfaces">Needed interfaces.</param>
        /// <param name="isOverridden">If set to <c>true</c> is overridden.</param>
        protected virtual void ServiceInitializer(ServiceCollection services, HashSet<Type> neededInterfaces, bool isOverridden = false)
        {
            if (!isOverridden && ServiceInitializerMethod == null && (neededInterfaces == null || neededInterfaces.Count == 0))
            {
                throw new NullReferenceException($"The `ServiceInitializerMethod` method has not been set on the QualityFacilitator.");
            }
            else if (ServiceInitializerMethod != null) //This can be simplified, but I'd prefer the check to be explicit/separate.
            {
                ServiceInitializerMethod(services, neededInterfaces);
            }

        }

        public S Get<S>() where S:T
        {

            string name = typeof(S).FullName;
            var candidates = matchingTypes.Where(t => t.FullName.StartsWith(name, StringComparison.Ordinal));
            if (candidates == null || !candidates.Any())
            {
                return default(S);
            }

            if (candidates.Count() == 1)
            {
                //Happy path: no ambiguity
                var type = candidates.First();
                return InstantiateType<S>(type);
            }
            else
            {
                List<Type> sortedCandidates = new List<Type>(candidates);
                sortedCandidates.Sort((x, y) => x.FullName.Replace(name, "").Length.CompareTo(y.FullName.Replace(name, "").Length));
                var type = sortedCandidates.First();
                return InstantiateType<S>(type);

            }
        }

        private S InstantiateType<S>(Type type) where S:T
        {
            var availableConstructors = new List<ConstructorInfo>(type.GetConstructors());
            availableConstructors.Sort((x, y) => x.GetParameters().Length.CompareTo(y.GetParameters().Length));
            //We'll go w/ the first one we can instantiate, if possible.  
            foreach (var ctor in availableConstructors)
            {
                var parameters = new List<object>();
                foreach(var parameter in ctor.GetParameters())
                {
                    var obj = ServiceProvider.GetService(parameter.ParameterType);
                    if (obj == null)
                    {
                        throw new MissingServiceException(parameter.ParameterType.FullName);
                    }
                    parameters.Add(obj);
                }
                return (S)ctor.Invoke(parameters.ToArray());
            }

            //If we got here, couldn't find a match
            throw new TypeInitializationException(type.FullName,
            new MissingFieldException("Unable to initialize the type, because some of the dependencies were not setup."));
        }



        /// <summary>
        /// Initializes the ServiceProvider after calling the method specified by the ServiceInitializer.
        /// </summary>
        public void InitializeServices()
        {

            ServiceInitializer(Services, neededInterfaces);
            ServiceProvider = Services.BuildServiceProvider();
        }

        /// <summary>
        /// Examines each constructor of the matching types to determine which types need instances of.
        /// </summary>
        private void AnalyzeConstructors()
        {
            neededTypesToConstructors = new Dictionary<Type, List<ConstructorInfo>>();

            //Determine the set of types needed.
            foreach (var type in matchingTypes)
            {
                foreach (var constructor in type.GetConstructors())
                {
                    var parameters = constructor.GetParameters();
                    foreach (var parameter in parameters)
                    {
                        List<ConstructorInfo> constructors = null;
                        if (!neededTypesToConstructors.TryGetValue(parameter.ParameterType, out constructors))
                        {
                            constructors = new List<ConstructorInfo>();
                            neededTypesToConstructors[parameter.ParameterType] = constructors;
                        }
                        constructors.Add(constructor);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the ServiceCollection with common services. Also some basic configuration setup. 
        /// </summary>
        private void PrepareServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IServiceCollection, ServiceCollection>((isp) => services);
            services.AddSingleton<IHostingEnvironment, FreshQualityHostinEnvironment>();
            services.AddScoped<IContainer, Container>();
            //services.AddScoped<IServiceProvider,>
            services.AddScoped<ILoggerFactory, LoggerFactory>();

            SetupConfiguration(services);

            DetermineNeededInterfaces(services);

            Services = services;
        }

        private void SetupConfiguration(ServiceCollection services)
        {
            IConfiguration instance = null;
            if (SetupConfigurationMethod != null)
            {
                instance = SetupConfigurationMethod();
            }
            if (instance != null)
            {
                services.AddSingleton<IConfiguration>(instance);
            }
            else
            {
                DefaultSetupConfiguration(services);
            }
        }

        private static void DefaultSetupConfiguration(ServiceCollection services)
        {
            string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string launch = Environment.GetEnvironmentVariable("LAUNCH_PROFILE");

            if (string.IsNullOrWhiteSpace(env))
            {
                env = "Development";
            }

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            services.AddSingleton<IConfiguration>(builder.Build());
        }

        private bool processingNeededInterfaces = false;

        /// <summary>
        /// Examines the types of each implementing constructor, 
        /// and see which ones have already been defined in the service collection
        /// </summary>
        private void DetermineNeededInterfaces(ServiceCollection services)
        {
            if (neededInterfaces != null || processingNeededInterfaces)
            {
                return; //We've already done this.
            }

            processingNeededInterfaces = true;

            try
            {
                var workingNeededInterfaces = new HashSet<Type>();
                foreach (var neededType in neededTypesToConstructors.Keys)
                {
                    bool found = false;
                    foreach (var service in services)
                    {
                        if (service.ServiceType == neededType)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        workingNeededInterfaces.Add(neededType);
                    }
                }

                neededInterfaces = workingNeededInterfaces;
            }
            finally
            {
                processingNeededInterfaces = false;
            }
        }



        /// <summary>
        /// Helper method that determines if a candidate string starts with any prefixes in the list.
        /// </summary>
        private bool StartsWithAny(string toCheck, List<string> toCheckAgainst)
        {
            return toCheckAgainst.Any(candidate => toCheck.StartsWith(candidate, StringComparison.Ordinal));
        }

        private void LoadReferencedAssembly(Assembly assembly, List<string> toIgnore)
        {
            foreach (AssemblyName name in assembly.GetReferencedAssemblies())
            {
                if (StartsWithAny(name.FullName, toIgnore))
                {
                    continue;
                }

                if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName == name.FullName))
                {
                    this.LoadReferencedAssembly(Assembly.Load(name), new List<string>());
                }
            }
        }

        /// <summary>
        /// Determines which types from all the assemblies match 
        /// </summary>
        private void PopulateMatchingTypesList(List<string> namespaceIgnoreList)
        {
            var toIgnore = namespaceIgnoreList ?? new List<string>() { "System.", "Microsoft.", "Newtonsoft." };

            matchingTypes = new List<Type>();

            if (SubclassType !=null)
            {
                    LoadReferencedAssembly(SubclassType.Assembly, toIgnore);
            }


            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (StartsWithAny(assembly.FullName, toIgnore))
                {
                    continue;
                }

                foreach (var type in assembly.GetTypes())
                {
                    if (type == null)
                    {
                        continue;
                    }

                    if (StartsWithAny(type.FullName, toIgnore))
                    {
                        continue;
                    }

                    if ( type.IsSubclassOf(typeof(T)))
                    {
                        matchingTypes.Add(type);
                    }
                }
            }
        }
    }
}
