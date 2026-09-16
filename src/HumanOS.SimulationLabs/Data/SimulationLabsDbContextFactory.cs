using Microsoft.EntityFrameworkCore.Design;

namespace HumanOS.SimulationLabs.Data;

public class SimulationLabsDbContextFactory : IDesignTimeDbContextFactory<SimulationLabsDbContext>
{
    public SimulationLabsDbContext CreateDbContext(string[] args)
    {
        return DbContextHelper.CreateDbContext();
    }
}
