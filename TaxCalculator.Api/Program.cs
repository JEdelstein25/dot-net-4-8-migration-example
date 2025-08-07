using Autofac;
using Autofac.Extensions.DependencyInjection;
using TaxCalculator.Data.Repositories;
using TaxCalculator.Services.Interfaces;
using TaxCalculator.Services.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Setup Autofac
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Register services
    containerBuilder.RegisterType<TaxCalculationService>().As<ITaxCalculationService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<UserTaxService>().As<IUserTaxService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<CacheService>().As<ICacheService>().SingleInstance();
    containerBuilder.RegisterType<SimpleLogger>().As<ILogger>().SingleInstance();
    
    // Register repositories
    containerBuilder.RegisterType<TaxBracketRepository>().AsSelf().InstancePerLifetimeScope();
    containerBuilder.RegisterType<UserRepository>().AsSelf().InstancePerLifetimeScope();
    containerBuilder.RegisterType<UserIncomeRepository>().AsSelf().InstancePerLifetimeScope();
    containerBuilder.RegisterType<SqlConnectionFactory>().AsSelf().InstancePerLifetimeScope();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
