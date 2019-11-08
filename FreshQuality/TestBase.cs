// <copyright file="TestBase.cs" company="Fresh Consulting LLC">2019</copyright>
namespace FreshQuality
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Base Class that initializes the IOC necessary for the test.
    /// </summary>
    /// <typeparam name="T">The class to provide IOC for</typeparam>
    /// <typeparam name="TSubclassType">The testing class that inherits from TestBase</typeparam>
    [TestClass]
    public abstract class TestBase<T, TSubclassType>
    {
        /// <summary>
        /// Private instance of the underlying mechanism that parses the DLLs for the desired type.
        /// </summary>
        private QualityFacillitator<T> facillitator = null;

        /// <summary>
        /// List of issues encountered by the facillitator.
        /// </summary>
        private volatile List<Exception> errorsEncountered = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestBase{T,TSubclassType}"/> class.
        /// </summary>
        public TestBase()
        {
            this.facillitator = new QualityFacillitator<T>(typeof(TSubclassType));

            try
            {
                this.facillitator.ServiceInitializerMethod = this.ServiceInitializerWrapper;
                this.facillitator.SetupConfigurationMethod = this.SetupConfiguration;

                if (this.facillitator.InitializationError == null)
                {
                    this.facillitator.InitializeServices();
                }
                else
                {
                    this.LogErrorOccurred(this.facillitator.InitializationError);
                }
            }
            catch (Exception ex)
            {
                this.LogErrorOccurred(ex);
            }
        }

        /// <summary>
        /// This is a sentinel that prevents the unit tests from running if there were errors
        /// initializing the IOC environment.
        /// </summary>
        [TestInitialize]
        public void PrepareTests()
        {
            // This reveals if any missing references exist
            if (this.errorsEncountered != null && this.errorsEncountered.Count > 0)
            {
                throw new AggregateException("Errors occurred in preparing tests", this.errorsEncountered);
            }
        }

        /// <summary>
        /// This method allows for easy retrieval of "T".
        /// </summary>
        /// <typeparam name="S">Desired type</typeparam>
        /// <param name="serviceOverrides">overrides to IOC</param>
        /// <returns>An instance of the desired type</returns>
        public S Get<S>(params object[] serviceOverrides)
            where S : T
        {
            return this.facillitator.Get<S>(serviceOverrides);
        }

        /// <summary>
        /// Overridable mechanism for setting up services.
        /// </summary>
        /// <param name="services">Services to initialize</param>
        /// <param name="neededInterfaces">Interfaces there were found to be missing.</param>
        protected abstract void ServiceInitializer(ServiceCollection services, HashSet<Type> neededInterfaces);

        /// <summary>
        /// Sets up the IConfiguration object.
        /// </summary>
        /// <returns>An instance of IConfiguration, override if null is not desired.</returns>
        protected virtual IConfiguration SetupConfiguration()
        {
            return null;
        }

        /// <summary>
        /// Adds the exception to the list of exceptions encountered.
        /// </summary>
        /// <param name="ex">The exception to log</param>
        private void LogErrorOccurred(Exception ex)
        {
            if (this.errorsEncountered == null)
            {
                this.errorsEncountered = new List<Exception>();
            }

            this.errorsEncountered.Add(ex);
        }

        /// <summary>
        /// Wraps the ServiceInitializer method with error handling.
        /// </summary>
        /// <param name="services">Services to initialize</param>
        /// <param name="neededInterfaces">Any missing interfaces found</param>
        private void ServiceInitializerWrapper(ServiceCollection services, HashSet<Type> neededInterfaces)
        {
            try
            {
                this.ServiceInitializer(services, neededInterfaces);
            }
            catch (Exception ex)
            {
                this.LogErrorOccurred(ex);
            }
        }
    }
}
