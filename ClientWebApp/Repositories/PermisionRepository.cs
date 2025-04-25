using ClientWebApp.Data;
using ClientWebApp.Models.DatabaseModels;

namespace ClientWebApp.Repositories
{
    public class PermisionRepository : BaseRepository
    {
        public PermisionRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        public void addPermission(AccessPermission permission)
        {
            _dbContext.AccessPermissions.Add(permission);
            _dbContext.SaveChanges();
        }
    }
}
