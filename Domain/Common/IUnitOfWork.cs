using System.Threading.Tasks;

namespace AutoMatics.Domain.Common
{
    public interface IUnitOfWork
    {
        Task CompleteAsync();
    }
}