using SharedHideoutClient.Enums;
using System.Threading.Tasks;

namespace SharedHideoutClient.Models.Interfaces
{
    public interface IHideoutActionHandler
    {
        EHideoutActionType ActionType { get; }
        Task HandleRequest(string json);
    }
}
