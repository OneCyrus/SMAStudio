using SMAStudio.SMAWebService;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Services
{
    public interface IScheduleService
    {
        IList<Schedule> GetSchedules(bool forceDownload = false);

        ObservableCollection<ScheduleViewModel> GetScheduleViewModels(bool forceDownload = false);

        bool Create();

        bool Update(ScheduleViewModel schedule);

        bool Delete(ScheduleViewModel schedule);
    }
}
