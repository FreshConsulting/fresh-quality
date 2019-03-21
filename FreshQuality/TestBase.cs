using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FreshQuality
{
    [TestClass]
    public abstract class TestBase<T, SubclassType>
    {
        QualityFacillitator<T> facillitator = null;
        volatile List<Exception> ErrorsEncountered = null;

        public TestBase()
        {
            facillitator = new QualityFacillitator<T>(typeof(SubclassType));

            try
            {
                facillitator.ServiceInitializerMethod = ServiceInitializerWrapper;
                facillitator.SetupConfigurationMethod = SetupConfiguration;

                if (facillitator.InitializationError == null)
                {
                    facillitator.InitializeServices();
                }
                else
                {
                    LogErrorOccurred(facillitator.InitializationError);
                }
            }
            catch (Exception ex)
            {
                LogErrorOccurred(ex);
            }

        }

        private void LogErrorOccurred(Exception ex)
        {
            if (ErrorsEncountered == null)
            {
                ErrorsEncountered = new List<Exception>();
            }
            ErrorsEncountered.Add(ex);
        }

        private void ServiceInitializerWrapper(ServiceCollection services, HashSet<Type> neededInterfaces)
        {
            try
            {
                ServiceInitializer(services, neededInterfaces);
            }
            catch(Exception ex)
            {
                LogErrorOccurred(ex);
            }

        }

        protected abstract void ServiceInitializer(ServiceCollection services, HashSet<Type> neededInterfaces);

        protected virtual IConfiguration SetupConfiguration()
        {
            return null;
        }

        [TestInitialize]
        public void PrepareTests()
        {
            //This reveals if any missing references exist
            if (ErrorsEncountered != null && ErrorsEncountered.Count > 0)
            {
                throw new AggregateException("Errors occurred in preparing tests", ErrorsEncountered);
            }
        }


        public S Get<S>() where S:T
        {
            return facillitator.Get<S>();
        }
    }
}
