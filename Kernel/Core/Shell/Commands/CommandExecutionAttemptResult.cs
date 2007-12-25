using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOS.Shell.Commands
{
    public enum CommandExecutionAttemptResult
    {
        UnknownError=0,
        Success = 1,
        NotFound,
        BlankEntry   
    }
}
