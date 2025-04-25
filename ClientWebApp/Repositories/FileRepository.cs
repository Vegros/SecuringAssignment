using ClientWebApp.Data;
using ClientWebApp.Models.DatabaseModels;

namespace ClientWebApp.Repositories
{
    public class FileRepository : BaseRepository
    {


        public FileRepository(ApplicationDbContext dbContext) : base(dbContext)
        {

        }

        public void UploadFile(UploadedFile file)
        {
            _dbContext.Files.Add(file);
            _dbContext.SaveChanges();
        }
    }
}
