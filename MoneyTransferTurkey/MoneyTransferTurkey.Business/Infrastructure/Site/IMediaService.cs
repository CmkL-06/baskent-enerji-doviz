using MoneyTransferTurkey.Entity.Entities.Site;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Infrastructure.Site
{
    public interface IMediaService
    {
        Task<List<MediaFile>> GetAll();
        Task<MediaFile> GetById(int id);
        Task<MediaFile> Add(MediaFile mediaFile);
        Task Update(MediaFile mediaFile);
        Task Delete(int id);
        Task<List<MediaFile>> GetByType(string fileType);
        Task<List<MediaFile>> Search(string query);
    }
}