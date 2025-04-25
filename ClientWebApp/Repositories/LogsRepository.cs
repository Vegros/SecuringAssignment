using ClientWebApp.Data;
using ClientWebApp.Models.DatabaseModels;

namespace ClientWebApp.Repositories
{
    public class LogsRepository : BaseRepository
    {
        public LogsRepository(ApplicationDbContext dbContext) : base(dbContext)
        {

        }

        public void addLogs(AccessLog log)
        {
            _dbContext.AccessLogs.Add(log);
            _dbContext.SaveChanges();
        }
    }
}
