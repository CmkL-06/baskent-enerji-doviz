using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity
{
    public class Enums
    {
    }

    public enum Gender
    {
        Male,
        Female
    }

    public enum Rank
    {
        Banned,
        User,
        Customer,
        Staff = 50,   // Personel / şube erişimi
        Admin = 99,
        Owner = 100,
    }
    public enum ContentType
    {
        Article,
    }

    public enum LogType
    {
        Added = 0,
        Deleted = 1,
        Updated = 2,
        Exchange= 3,
    }
    public enum ActionType
    {
        Transaction,
        Deposit,
    }

    public enum OfficeType
    {
        Merkez = 1,  // Ana kasa — transfer gönderme yetkisi var
        Sube   = 2,  // Şube — transfer talep eder, merkez onaylar
        Bayi   = 3,  // Bayi — kendi kasasıyla çalışır, işlem limiti var
    }

    public enum OfficeRole
    {
        Manager = 1,  // Şube/bayi müdürü — kendi birimini yönetir
        Cashier = 2,  // Kasa görevlisi — işlem girebilir
        Viewer  = 3,  // Salt okunur erişim
    }

    public enum TransferStatus
    {
        Pending   = 0,
        Approved  = 1,
        Rejected  = 2,
        Completed = 3,
        Cancelled = 4,
    }

    public enum RateInheritanceMode
    {
        UseParent = 0,  // Merkez kurlarını kullan
        Custom    = 1,  // Kendi kurlarını belirle
    }

    public enum AlertType
    {
        LowBalance          = 1,
        UnusualTransaction  = 2,
        ThresholdBreach     = 3,
        TransferPending     = 4,
        DayClosureMissing   = 5,
    }

    public enum AlertSeverity
    {
        Info     = 0,
        Warning  = 1,
        Critical = 2,
    }
}
