using System.Reflection;
using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.Modules.Admin;
using RezSaaS.Modules.Availability;
using RezSaaS.Modules.Booking;
using RezSaaS.Modules.Catalog;
using RezSaaS.Modules.Identity;
using RezSaaS.Modules.Integrations;
using RezSaaS.Modules.Messaging;
using RezSaaS.Modules.Organization;
using RezSaaS.Modules.Payments;
using RezSaaS.Modules.Resources;
using RezSaaS.Modules.Reviews;
using RezSaaS.Modules.TenantManagement;

namespace RezSaaS.ArchitectureTests;

public sealed class ModuleDependencyTests
{
    public static TheoryData<Type> ModuleTypes
    {
        get
        {
            TheoryData<Type> moduleTypes = new();
            moduleTypes.Add(typeof(IdentityModule));
            moduleTypes.Add(typeof(TenantManagementModule));
            moduleTypes.Add(typeof(OrganizationModule));
            moduleTypes.Add(typeof(CatalogModule));
            moduleTypes.Add(typeof(ResourcesModule));
            moduleTypes.Add(typeof(AvailabilityModule));
            moduleTypes.Add(typeof(BookingModule));
            moduleTypes.Add(typeof(MessagingModule));
            moduleTypes.Add(typeof(ReviewsModule));
            moduleTypes.Add(typeof(IntegrationsModule));
            moduleTypes.Add(typeof(PaymentsModule));
            moduleTypes.Add(typeof(AdminModule));

            return moduleTypes;
        }
    }

    [Theory]
    [MemberData(nameof(ModuleTypes))]
    public void ModulesMustImplementModuleContract(Type moduleType)
    {
        Assert.True(typeof(IModule).IsAssignableFrom(moduleType));
    }

    [Theory]
    [MemberData(nameof(ModuleTypes))]
    public void ModulesMustNotReferenceOtherModules(Type moduleType)
    {
        Assembly moduleAssembly = moduleType.Assembly;
        string moduleAssemblyName = moduleAssembly.GetName().Name!;

        string[] invalidReferences = moduleAssembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name =>
                name is not null
                && name.StartsWith("RezSaaS.Modules.", StringComparison.Ordinal)
                && name != moduleAssemblyName)
            .Select(name => name!)
            .ToArray();

        Assert.Empty(invalidReferences);
    }
}
