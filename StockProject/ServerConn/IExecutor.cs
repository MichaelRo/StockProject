
using System.Collections.Generic;

namespace StockProject
{
    interface IExecutor
    {
        bool execute(ControlOptions option, List<string> args);
        void executeCommand(string command);
    }
}