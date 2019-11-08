// <summary>
// FreshQuality is a tool that will:
// a) analyze the attached libraries/projects of a test project,
// b) create instances of pertinent classes(via dependency injection), initially only controllers, but this was extended.
// c) expose them for use in unit tests.
//
// The ability to how the dependency injection works allow for testing the classes
// in a disconnect manner.
// </summary>
// <typeparam name="T">Type to scan for</typeparam>
// <copyright file="QualityFacillitator.cs" company="Fresh Consulting LLC">2019</copyright>

namespace FreshQuality
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Scaffolding mechanism to facillitate testing.
    /// </summary>
    /// <typeparam name="T">Type to scan for</typeparam>
    public class QualityFacillitator<T>
    {
        /// <summary>
        /// Mapping between a needed type and every constructor that needs the type.
        /// </summary>
        private Dictionary<Type, List<ConstructorInfo>> neededTypesToConstructors;

        /// <summary>
        /// State flag for if the processing is already in progress.
        /// </summary>
        private bool processingNeededInterfaces = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:FreshQuality.QualityFacillitator`1"/> class.
        /// </summary>
        /// <param name="subclassType">Subclass type.</param>
        /// <param name="namespaceIgnoreList">prefix list of namespaces to ignore.  Defaults to "System." and "Microsoft.".</param>
        public QualityFacillitator(Type subclassType, List<string> namespaceIgnoreList = null)
        {
            try
            {
                this.SubclassType = subclassType;
                this.PopulateMatchingTypesList(namespaceIgnoreList);
                this.AnalyzeConstructors();
                this.PrepareServices();
            }
            catch (Exception ex)
            {
                this.InitializationError = ex;
            }
        }

        /// <summary>
        /// Gets or sets the Service Initializer Method.  The ServiceInitializer is called with the existing <Type>ServicesCollection</Type> as well as
        /// a <Type>HashSet<typeparamref name="Type"/></Type>.  Any custom services used in DI/IOC
        /// should be set here.
        /// </summary>
        public Action<ServiceCollection, HashSet<Type>> ServiceInitializerMethod { get; set; }

        /// <summary>
        /// Gets the ServiceProvider
        /// </summary>
        public ServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// Gets or sets the Setup Configuration Method.
        /// </summary>
        public Func<IConfiguration> SetupConfigurationMethod { get; set; }

        /// <summary>
        /// Gets any initialization errors.
        /// </summary>
        internal Exception InitializationError { get; private set; }

        /// <summary>
        /// Gets or sets the subclass type.
        /// </summary>
        protected Type SubclassType { get; set; }

        /// <summary>
        /// Gets or sets the types that implement the generic class type (T).
        /// </summary>
        private List<Type> MatchingTypes { get; set; }

        /// <summary>
        /// Gets or sets the interfaces that need to be defined by the user of the library
        /// </summary>
        private HashSet<Type> NeededInterfaces { get; set; }

        /// <summary>
        /// Gets or sets the Services collection
        /// </summary>
        private ServiceCollection Services { get; set; }

        /// <summary>
        /// Gets an instance of type S.
        /// </summary>
        /// <typeparam name="S">The subtype to get</typeparam>
        /// <returns>Instance of type S.</returns>
        public S Get<S>(params object[] serviceOverrides)
            where S : T
        {
            string name = typeof(S).FullName;

            // ToList here to avoid multiple enumerations.
            // Without ToList, every .Any or .First or .Count
            // will go through the enumeration from the beginning.
            var candidates = this.MatchingTypes
                .Where(t => t.FullName.StartsWith(name, StringComparison.Ordinal))
                .ToList();

            if (!candidates.Any())
            {
                return default(S);
            }

            if (candidates.Count == 1)
            {
                // Happy path: no ambiguity
                var type = candidates.First();
                return this.InstantiateType<S>(type, serviceOverrides);
            }
            else
            {
                // System.Linq FTW
                var type = candidates
                    .OrderBy(c => c.FullName.Replace(name, string.Empty))
                    .First();

                return this.InstantiateType<S>(type, serviceOverrides);
            }
        }

        /// <summary>
        /// Initializes the ServiceProvider after calling the method specified by the ServiceInitializer.
        /// </summary>
        public void InitializeServices()
        {
            this.ServiceInitializer(this.Services, this.NeededInterfaces);
            this.ServiceProvider = this.Services.BuildServiceProvider();
        }

        /// <summary>
        /// Either this method must be overridden or the ServiceInitializerMethod must be set.
        /// </summary>
        /// <param name="services">Services to configure.</param>
        /// <param name="neededInterfaces">Needed interfaces.</param>
        /// <param name="isOverridden">If set to <c>true</c> is overridden.</param>
        protected virtual void ServiceInitializer(
            ServiceCollection services, HashSet<Type> neededInterfaces, bool isOverridden = false)
        {
            if (!isOverridden && this.ServiceInitializerMethod == null &&
                (neededInterfaces == null || neededInterfaces.Count == 0))
            {
                throw new NullReferenceException(
                    $"The `ServiceInitializerMethod` method has not been set on the QualityFacilitator.");
            }

            // This can be simplified, but I'd prefer the check to be explicit/separate.
            if (this.ServiceInitializerMethod != null)
            {
                this.ServiceInitializerMethod(services, neededInterfaces);
            }
        }

        /// <summary>
        /// Sets up the default config environment.
        /// </summary>
        /// <param name="services">services collection</param>
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

        /// <summary>
        /// Creates an instance of type S.
        /// </summary>
        /// <typeparam name="S">Type instance</typeparam>
        /// <param name="type">type to instantiate</param>
        /// <param name="serviceOverrides">optional overrides to instances</param>
        /// <returns>An instance of type S</returns>
        private S InstantiateType<S>(Type type, params object[] serviceOverrides)
            where S : T
        {
            var availableConstructors = type.GetConstructors()
                .OrderBy(c => c.GetParameters().Length).ToList();

            var serviceOverridesDict = serviceOverrides.ToDictionary(k => k.GetType());

            // We'll go w/ the first one we can instantiate, if possible.
            foreach (var ctor in availableConstructors)
            {
                // This is more a suggestion, the foreach was good too.
                var parameters = ctor.GetParameters()
                    .Select(parameter => this.GetService(parameter.ParameterType, serviceOverridesDict))
                    .ToArray();

                return (S)ctor.Invoke(parameters);
            }

            // If we got here, couldn't find a match
            throw new TypeInitializationException(
                type.FullName,
                new MissingFieldException(
                    "Unable to initialize the type, because some of the dependencies were not setup."));
        }

        /// <summary>
        /// Gets an instance of a particular type, either using the <paramref name="serviceOverrides"/>
        /// or dependency.
        /// </summary>
        /// <param name="typeToGet">Type to retrieve an instance of</param>
        /// <param name="serviceOverrides">Optional overrides to IOC</param>
        /// <returns>instance of <paramref name="typeToGet"/></returns>
        private object GetService(Type typeToGet, Dictionary<Type, object> serviceOverrides)
        {
            if (!serviceOverrides.TryGetValue(typeToGet, out object instance))
            {
                // if an override doesn't exist, let's get the service via IOC
                instance = ServiceProvider.GetService(typeToGet);
            }

            if (instance == null)
            {
                throw new MissingServiceException(typeToGet.FullName);
            }

            return instance;
        }

        /// <summary>
        /// Examines each constructor of the matching types to determine which types need instances of.
        /// </summary>
        private void AnalyzeConstructors()
        {
            this.neededTypesToConstructors = new Dictionary<Type, List<ConstructorInfo>>();

            // Determine the set of types needed.
            foreach (var type in this.MatchingTypes)
            {
                foreach (var constructor in type.GetConstructors())
                {
                    var parameters = constructor.GetParameters();
                    foreach (var parameter in parameters)
                    {
                        if (!this.neededTypesToConstructors.TryGetValue(parameter.ParameterType, out var constructors))
                        {
                            constructors = new List<ConstructorInfo>();
                            this.neededTypesToConstructors[parameter.ParameterType] = constructors;
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
            services.AddSingleton<IHostingEnvironment, FreshQualityHostingEnvironment>();
            services.AddScoped<IContainer, Container>();
            services.AddScoped<ILoggerFactory, LoggerFactory>();

            this.SetupConfiguration(services);

            this.DetermineNeededInterfaces(services);

            this.Services = services;
        }

        /// <summary>
        /// Sets up the configuration device
        /// </summary>
        /// <param name="services">services collection</param>
        private void SetupConfiguration(ServiceCollection services)
        {
            IConfiguration instance = null;
            if (this.SetupConfigurationMethod != null)
            {
                instance = this.SetupConfigurationMethod();
            }

            // Personal preference: If Not, Else makes it hard to understand the code
            // I prefer to have to true statement in the beginning
            if (instance == null)
            {
                DefaultSetupConfiguration(services);
            }
            else
            {
                services.AddSingleton<IConfiguration>(instance);
            }
        }

        /// <summary>
        /// Examines the types of each implementing constructor,
        /// and see which ones have already been defined in the service collection
        /// </summary>
        /// <param name="services">Services to search through</param>
        private void DetermineNeededInterfaces(ServiceCollection services)
        {
            if (this.NeededInterfaces != null || this.processingNeededInterfaces)
            {
                return; // We've already done this.
            }

            this.processingNeededInterfaces = true;

            try
            {
                var workingNeededInterfaces = new HashSet<Type>();
                foreach (var neededType in this.neededTypesToConstructors.Keys)
                {
                    if (!services.Any(s => s.ServiceType == neededType))
                    {
                        workingNeededInterfaces.Add(neededType);
                    }
                }

                this.NeededInterfaces = workingNeededInterfaces;
            }
            finally
            {
                this.processingNeededInterfaces = false;
            }
        }

        /// <summary>
        /// Helper method that determines if a candidate string starts with any prefixes in the list.
        /// </summary>
        /// <param name="toCheck">search term</param>
        /// <param name="toCheckAgainst">list to search against</param>
        /// <returns>If the search term was found to prefix any of the list</returns>
        private bool StartsWithAny(string toCheck, List<string> toCheckAgainst)
        {
            return toCheckAgainst.Any(candidate => toCheck.StartsWith(candidate, StringComparison.Ordinal));
        }

        /// <summary>
        /// Loads referenced assemblies.
        /// </summary>
        /// <param name="assembly">Current assembly node to load</param>
        /// <param name="toIgnore">List of prefixes to ignore</param>
        private void LoadReferencedAssembly(Assembly assembly, List<string> toIgnore)
        {
            foreach (AssemblyName name in assembly.GetReferencedAssemblies())
            {
                if (this.StartsWithAny(name.FullName, toIgnore))
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
        /// <param name="namespaceIgnoreList">Namespaces to ignore list</param>
        private void PopulateMatchingTypesList(List<string> namespaceIgnoreList)
        {
            var toIgnore = namespaceIgnoreList ?? new List<string>() { "System.", "Microsoft.", "Newtonsoft." };

            this.MatchingTypes = new List<Type>();

            if (this.SubclassType != null)
            {
                this.LoadReferencedAssembly(this.SubclassType.Assembly, toIgnore);
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (this.StartsWithAny(assembly.FullName, toIgnore))
                {
                    continue;
                }

                foreach (var type in assembly.GetTypes())
                {
                    if (type == null)
                    {
                        continue;
                    }

                    if (this.StartsWithAny(type.FullName, toIgnore))
                    {
                        continue;
                    }

                    if (type.IsSubclassOf(typeof(T)))
                    {
                        this.MatchingTypes.Add(type);
                    }
                }
            }
        }
    }
}