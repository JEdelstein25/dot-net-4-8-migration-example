using System.Configuration;
using System.Reflection;
using Autofac;
using Autofac.Integration.WebApi;
using TaxCalculator.Data.Interfaces;
using TaxCalculator.Data.Repositories;
using TaxCalculator.Services.Interfaces;
using TaxCalculator.Services.Services;

namespace TaxCalculator.Api
{
    public static class AutofacConfig
    {
        public static void Configure()
        {
            var builder = new ContainerBuilder();

            // Register API controllers
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            // Register connection factory
            var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            builder.Register(c => new SqlConnectionFactory(connectionString))
                   .As<IConnectionFactory>()
                   .InstancePerLifetimeScope();

            // Register repositories
            builder.RegisterType<TaxBracketRepository>().As<ITaxBracketRepository>();
            builder.RegisterType<UserRepository>().As<IUserRepository>();
            builder.RegisterType<UserIncomeRepository>().As<IUserIncomeRepository>();

            // Register services
            builder.RegisterType<TaxCalculationService>().As<ITaxCalculationService>();
            builder.RegisterType<UserTaxService>().As<IUserTaxService>();
            builder.RegisterType<CacheService>().As<ICacheService>().SingleInstance();
            builder.RegisterType<SimpleLogger>().As<ILogger>().SingleInstance();

            // Build container
            var container = builder.Build();

            // Set dependency resolver
            GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }
    }
}
