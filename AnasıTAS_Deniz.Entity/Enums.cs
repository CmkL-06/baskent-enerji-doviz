using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity
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
        Author = 79,
        Moderator = 89,
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
}
