using System;

namespace RapidCMS.Core.Enums
{
    [Flags]
    public enum Features
    {
        None = 0,
        CanGoToEdit = 1,
        CanView = 2,
        CanEdit = 4
    }
}
