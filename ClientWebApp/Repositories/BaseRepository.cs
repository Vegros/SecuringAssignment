using ClientWebApp.Data;

namespace ClientWebApp.Repositories
{
    public class BaseRepository
    {
        public ApplicationDbContext _dbContext { get; set; }
        public BaseRepository(ApplicationDbContext context)
        {
            _dbContext = context;
        }
    }
}
