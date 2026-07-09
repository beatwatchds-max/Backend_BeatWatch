using BeatWatch_BackEnd.Data;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class DatabaseTestController : ControllerBase
    {
        private readonly MongoDbContext _dbContext;
        private readonly ILogger<DatabaseTestController> _logger;

        public DatabaseTestController(MongoDbContext dbContext, ILogger<DatabaseTestController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet("db-status")]
        public async Task<IActionResult> GetDbStatus()
        {
            _logger.LogInformation("Testing MongoDB connection and collection indexes...");
            try
            {
                var statusReport = new List<object>();

                // Check index verification helper function
                async Task<object> GetCollectionIndexInfo<T>(IMongoCollection<T> collection, string name)
                {
                    var indexesCursor = await collection.Indexes.ListAsync();
                    var indexesList = await indexesCursor.ToListAsync();
                    var indexNames = indexesList.Select(idx => idx.GetValue("name").AsString).ToList();
                    return new
                    {
                        CollectionName = name,
                        Status = "Connected & Verified",
                        IndexCount = indexesList.Count,
                        Indexes = indexNames
                    };
                }

                statusReport.Add(await GetCollectionIndexInfo(_dbContext.Usuarios, "Usuarios"));
                statusReport.Add(await GetCollectionIndexInfo(_dbContext.Pacientes, "Pacientes"));
                statusReport.Add(await GetCollectionIndexInfo(_dbContext.Licencias, "Licencias"));
                statusReport.Add(await GetCollectionIndexInfo(_dbContext.Arritmias, "Arritmias"));
                statusReport.Add(await GetCollectionIndexInfo(_dbContext.Dispositivos, "Dispositivos"));

                return Ok(new
                {
                    Message = "MongoDB connection is healthy and indexes are initialized.",
                    Timestamp = DateTime.UtcNow,
                    CollectionsReport = statusReport
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to MongoDB or verify indexes.");
                return StatusCode(500, new
                {
                    Message = "Failed to connect to MongoDB database.",
                    Error = ex.Message
                });
            }
        }
    }
}
