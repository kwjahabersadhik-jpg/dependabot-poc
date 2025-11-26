using CreditCardsSystem.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Web.Server
{
    public static class DbContextExtensions
    {
        public static void AddDataBaseConfiguration(this IServiceCollection services, ConfigurationManager configuration)
        {
            string sqlConnectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            string fdrConnectionString = configuration.GetConnectionString("FdrOracleConnection") ?? "";

            services.AddDbContextFactory<ApplicationDbContext>(option => { option.UseSqlServer(sqlConnectionString); }).AddDataProtection()
        .SetApplicationName(nameof(CreditCardsSystem.Web.Server))
        .PersistKeysToDbContext<ApplicationDbContext>();

            services.AddDbContextFactory<FdrDBContext>(option =>
            {
                option.UseOracle(fdrConnectionString, options =>
                {
                    options.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19);
                });
            });

            //services.AddDbContext<ReportingDbContext>(option =>
            //{
            //    option.UseSqlServer(configuration.GetConnectionString("ReportingSqlDbConnection"));
            //});
        }
    }
}